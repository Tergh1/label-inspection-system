using client.Data.Entities;
using client.Services;
using client.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace client.Tests.Services;

public sealed class InspectionTemplateWorkflowServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidatesAndPersistsTemplateWithStoredFile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var service = new InspectionTemplateWorkflowService(dbContext, fileStorage);

        await using var imageStream = ServiceFactory.ImageStream();
        var template = await service.CreateAsync(
            "owner-1",
            "  Pharmacy label  ",
            "uploads/template.png",
            "image/png",
            imageStream.Length,
            imageStream,
            7.126m,
            "  Baseline label  ",
            CancellationToken.None);

        Assert.Equal("owner-1", template.OwnerUserId);
        Assert.Equal("Pharmacy label", template.FriendlyName);
        Assert.Equal("template.png", template.OriginalFileName);
        Assert.Equal("Baseline label", template.Description);
        Assert.Equal(7.13m, template.TolerancePercent);
        Assert.Equal(48, template.PublicAccessToken.Length);
        Assert.True(File.Exists(fileStorage.GetAbsolutePath(template.StoredRelativePath)));
        Assert.Equal(template.Id, (await dbContext.InspectionTemplates.SingleAsync()).Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WhenFriendlyNameIsMissing_Throws(string friendlyName)
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var service = new InspectionTemplateWorkflowService(dbContext, ServiceFactory.CreateFileStorage(tempDirectory));

        await using var imageStream = ServiceFactory.ImageStream();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "owner-1",
            friendlyName,
            "template.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None));

        Assert.Equal("Friendly name is required.", exception.Message);
        Assert.Empty(dbContext.InspectionTemplates);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task CreateAsync_WhenToleranceIsOutsideAllowedRange_Throws(decimal tolerancePercent)
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var service = new InspectionTemplateWorkflowService(dbContext, ServiceFactory.CreateFileStorage(tempDirectory));

        await using var imageStream = ServiceFactory.ImageStream();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "owner-1",
            "Template",
            "template.png",
            "image/png",
            imageStream.Length,
            imageStream,
            tolerancePercent,
            null,
            CancellationToken.None));

        Assert.Equal("Tolerance must be between 0 and 100.", exception.Message);
        Assert.Empty(dbContext.InspectionTemplates);
    }

    [Fact]
    public async Task GetUserTemplatesAsync_ReturnsOnlyOwnerTemplatesNewestFirst()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var older = InspectionEntityFactory.Template(ownerUserId: "owner-1", friendlyName: "Older", createdAtUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var newer = InspectionEntityFactory.Template(ownerUserId: "owner-1", friendlyName: "Newer", createdAtUtc: DateTimeOffset.UtcNow);
        var otherOwner = InspectionEntityFactory.Template(ownerUserId: "owner-2", friendlyName: "Other");
        dbContext.InspectionTemplates.AddRange(older, newer, otherOwner);
        await dbContext.SaveChangesAsync();
        using var tempDirectory = new TempDirectory();
        var service = new InspectionTemplateWorkflowService(dbContext, ServiceFactory.CreateFileStorage(tempDirectory));

        var templates = await service.GetUserTemplatesAsync("owner-1", CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], templates.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task DeleteAsync_WhenTemplateIsReferenced_ThrowsAndLeavesTemplateAndFile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var service = new InspectionTemplateWorkflowService(dbContext, fileStorage);
        await using var imageStream = ServiceFactory.ImageStream();
        var template = await service.CreateAsync(
            "owner-1",
            "Template",
            "template.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None);
        dbContext.InspectionImages.Add(InspectionEntityFactory.Image(templateId: template.Id, ownerUserId: "owner-1"));
        await dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync("owner-1", template.Id, CancellationToken.None));

        Assert.Equal("Template cannot be deleted because it is used by one or more uploaded images.", exception.Message);
        Assert.True(File.Exists(fileStorage.GetAbsolutePath(template.StoredRelativePath)));
        Assert.NotNull(await dbContext.InspectionTemplates.FindAsync(template.Id));
    }

    [Fact]
    public async Task DeleteAsync_WhenTemplateIsUnreferenced_RemovesTemplateAndFile()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var tempDirectory = new TempDirectory();
        var fileStorage = ServiceFactory.CreateFileStorage(tempDirectory);
        var service = new InspectionTemplateWorkflowService(dbContext, fileStorage);
        await using var imageStream = ServiceFactory.ImageStream();
        var template = await service.CreateAsync(
            "owner-1",
            "Template",
            "template.png",
            "image/png",
            imageStream.Length,
            imageStream,
            5,
            null,
            CancellationToken.None);
        var absolutePath = fileStorage.GetAbsolutePath(template.StoredRelativePath);

        await service.DeleteAsync("owner-1", template.Id, CancellationToken.None);

        Assert.False(File.Exists(absolutePath));
        Assert.Null(await dbContext.InspectionTemplates.FindAsync(template.Id));
    }
}
