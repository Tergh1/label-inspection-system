using System.Net;
using System.Net.Http.Json;
using client.Contracts;
using client.Data;
using client.Data.Entities;
using client.Endpoints;
using client.Options;
using client.Services;
using client.Tests.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace client.Tests.Endpoints;

public sealed class MlWebhookEndpointsTests
{
    [Fact]
    public async Task Webhook_WhenSecretIsMissing_ReturnsUnauthorized()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/ml/webhook", new MlWebhookResultHolder
        {
            ImageId = Guid.NewGuid().ToString(),
            Results = [new MlWebhookResult { Model = "SSIM", SimilarityPercent = 99 }]
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenImageIdIsInvalid_ReturnsBadRequest()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Webhook-Secret", "secret");

        var response = await client.PostAsJsonAsync("/api/ml/webhook", new MlWebhookResultHolder
        {
            ImageId = "not-a-guid",
            Results = [new MlWebhookResult { Model = "SSIM", SimilarityPercent = 99 }]
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenImageDoesNotExist_ReturnsNotFound()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Webhook-Secret", "secret");

        var response = await client.PostAsJsonAsync("/api/ml/webhook", new MlWebhookResultHolder
        {
            ImageId = Guid.NewGuid().ToString(),
            Results = [new MlWebhookResult { Model = "SSIM", SimilarityPercent = 99 }]
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenResultsAreEmpty_ReturnsBadRequest()
    {
        var image = InspectionEntityFactory.Image(processingStatus: ProcessingStatus.Queued, outcomeStatus: OutcomeStatus.Pending);
        await using var app = await CreateAppAsync(image);
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Webhook-Secret", "secret");

        var response = await client.PostAsJsonAsync("/api/ml/webhook", new MlWebhookResultHolder
        {
            ImageId = image.Id.ToString(),
            Results = []
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WhenPayloadIsValid_UpdatesImageAndPublishesNotification()
    {
        var image = InspectionEntityFactory.Image(processingStatus: ProcessingStatus.Queued, outcomeStatus: OutcomeStatus.Pending);
        image.MinimumSimilarityPercent = 95;
        InspectionUpdateNotification? notification = null;
        await using var app = await CreateAppAsync(image, notifier =>
        {
            notifier.Subscribe(image.OwnerUserId, published =>
            {
                notification = published;
                return Task.CompletedTask;
            });
        });
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Webhook-Secret", "secret");

        var response = await client.PostAsJsonAsync("/api/ml/webhook", new MlWebhookResultHolder
        {
            ImageId = image.Id.ToString(),
            Results =
            [
                new MlWebhookResult
                {
                    Model = " SSIM ",
                    SimilarityPercent = 97
                }
            ]
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await dbContext.InspectionImages.SingleAsync(x => x.Id == image.Id);
        Assert.Equal(ProcessingStatus.Completed, updated.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Valid, updated.OutcomeStatus);
        Assert.Equal(97, updated.SimilarityPercent);
        Assert.NotNull(notification);
        Assert.Equal(image.OwnerUserId, notification!.OwnerUserId);
        Assert.Equal(image.Id, notification.ImageId);
    }

    private static async Task<WebApplication> CreateAppAsync(
        InspectionImage? image = null,
        Action<InspectionUpdateNotifier>? configureNotifier = null)
    {
        var builder = WebApplication.CreateBuilder();
        var databaseName = $"webhook-tests-{Guid.NewGuid():N}";
        builder.WebHost.UseTestServer();
        builder.Services.Configure<InspectionMlOptions>(options =>
        {
            options.WebhookPath = "/api/ml/webhook";
            options.WebhookSecret = "secret";
            options.WebhookSecretHeaderName = "X-Test-Webhook-Secret";
        });
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        builder.Services.AddSingleton<InspectionUpdateNotifier>();

        var app = builder.Build();
        app.MapMlWebhookEndpoints();

        if (image is not null)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.InspectionImages.Add(image);
            await dbContext.SaveChangesAsync();
        }

        if (configureNotifier is not null)
        {
            configureNotifier(app.Services.GetRequiredService<InspectionUpdateNotifier>());
        }

        await app.StartAsync();
        return app;
    }
}
