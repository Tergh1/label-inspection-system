using System.Net;
using System.Text.Json;
using client.Data.Entities;
using client.Services;
using client.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace client.Tests.Services;

public sealed class InspectionWorkflowServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenDispatchSucceeds_PersistsImageAndMarksQueued()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var service = CreateService(dbContext, fileStorage, handler);

        await using var imageStream = ServiceFactory.ImageStream();
        var image = await service.CreateAsync(
            "owner-1",
            template.Id,
            "uploads/label.png",
            "image/png",
            imageStream.Length,
            imageStream,
            7.126m,
            "  First run  ",
            CancellationToken.None);

        Assert.Equal(template.Id, image.TemplateId);
        Assert.Equal("label.png", image.OriginalFileName);
        Assert.Equal("First run", image.Description);
        Assert.Equal(7.13m, image.TolerancePercent);
        Assert.Equal(92.87m, image.MinimumSimilarityPercent);
        Assert.Equal(ProcessingStatus.Queued, image.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Pending, image.OutcomeStatus);
        Assert.Null(image.FailureReason);
        Assert.True(File.Exists(fileStorage.GetAbsolutePath(image.StoredRelativePath)));
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/inspect-async", request.Uri);
        Assert.True(request.Headers.ContainsKey("X-Test-Key"));
        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(image.Id, body.RootElement.GetProperty("image_id").GetGuid());
        Assert.Contains(image.PublicAccessToken, body.RootElement.GetProperty("image_url").GetString());
        Assert.Contains(template.PublicAccessToken, body.RootElement.GetProperty("template_url").GetString());
        Assert.Equal("https://app.example.test/api/ml/webhook", body.RootElement.GetProperty("callback_url").GetString());
    }

    [Fact]
    public async Task CreateAsync_WhenTemplateIsNotOwnedByUser_ThrowsBeforeSavingFile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-2");
        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(dbContext, fileStorage, handler);

        await using var imageStream = ServiceFactory.ImageStream();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "owner-1",
            template.Id,
            "label.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None));

        Assert.Equal("Template not found.", exception.Message);
        Assert.Empty(dbContext.InspectionImages);
        Assert.Empty(Directory.EnumerateFiles(tempDirectory.Path, "*", SearchOption.AllDirectories));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task CreateAsync_WhenToleranceIsOutsideAllowedRange_Throws(decimal tolerancePercent)
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, fileStorage, new FakeHttpMessageHandler());

        await using var imageStream = ServiceFactory.ImageStream();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "owner-1",
            template.Id,
            "label.png",
            "image/png",
            imageStream.Length,
            imageStream,
            tolerancePercent,
            null,
            CancellationToken.None));

        Assert.Equal("Tolerance must be between 0 and 100.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDispatchFails_MarksImageFailedAndStoresReason()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var handler = new FakeHttpMessageHandler
        {
            ExceptionToThrow = new HttpRequestException("ML unavailable")
        };
        var service = CreateService(dbContext, fileStorage, handler);

        await using var imageStream = ServiceFactory.ImageStream();
        var image = await service.CreateAsync(
            "owner-1",
            template.Id,
            "label.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None);

        Assert.Equal(ProcessingStatus.Failed, image.ProcessingStatus);
        Assert.Equal("ML unavailable", image.FailureReason);
    }

    [Fact]
    public async Task DeleteAsync_RemovesImageAndStoredFile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, fileStorage, new FakeHttpMessageHandler());
        await using var imageStream = ServiceFactory.ImageStream();
        var image = await service.CreateAsync(
            "owner-1",
            template.Id,
            "label.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None);
        var absolutePath = fileStorage.GetAbsolutePath(image.StoredRelativePath);

        await service.DeleteAsync("owner-1", image.Id, CancellationToken.None);

        Assert.False(File.Exists(absolutePath));
        Assert.Null(await dbContext.InspectionImages.FindAsync(image.Id));
    }

    [Fact]
    public async Task RedispatchFailedAsync_ClearsPreviousResultsAndQueuesImage()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        var image = InspectionEntityFactory.Image(
            templateId: template.Id,
            ownerUserId: "owner-1",
            processingStatus: ProcessingStatus.Failed,
            outcomeStatus: OutcomeStatus.Invalid);
        image.SimilarityPercent = 70;
        image.DefectsJson = "[]";
        image.MlResultsJson = "[]";
        image.FailureReason = "previous failure";
        dbContext.InspectionTemplates.Add(template);
        dbContext.InspectionImages.Add(image);
        await dbContext.SaveChangesAsync();
        var handler = new FakeHttpMessageHandler();
        var service = CreateService(dbContext, fileStorage, handler);

        var redispatched = await service.RedispatchFailedAsync("owner-1", image.Id, CancellationToken.None);

        Assert.Equal(ProcessingStatus.Queued, redispatched.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Pending, redispatched.OutcomeStatus);
        Assert.Null(redispatched.SimilarityPercent);
        Assert.Null(redispatched.DefectsJson);
        Assert.Null(redispatched.MlResultsJson);
        Assert.Null(redispatched.FailureReason);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task RedispatchFailedAsync_WhenImageIsNotFailed_Throws()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var template = InspectionEntityFactory.Template(ownerUserId: "owner-1");
        var image = InspectionEntityFactory.Image(
            templateId: template.Id,
            ownerUserId: "owner-1",
            processingStatus: ProcessingStatus.Completed);
        dbContext.InspectionTemplates.Add(template);
        dbContext.InspectionImages.Add(image);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, fileStorage, new FakeHttpMessageHandler());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RedispatchFailedAsync("owner-1", image.Id, CancellationToken.None));

        Assert.Equal("Only failed images can be inspected again.", exception.Message);
    }

    private static InspectionWorkflowService CreateService(
        client.Data.ApplicationDbContext dbContext,
        InspectionFileStorage fileStorage,
        FakeHttpMessageHandler handler)
    {
        return new InspectionWorkflowService(
            dbContext,
            fileStorage,
            ServiceFactory.CreateUrlBuilder(),
            ServiceFactory.CreateMlClient(handler));
    }
}
