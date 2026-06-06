using client.Options;
using client.Services;
using Microsoft.Extensions.Options;

namespace client.Tests.TestSupport;

internal static class ServiceFactory
{
    public static InspectionFileStorage CreateFileStorage(TempDirectory tempDirectory)
    {
        return new InspectionFileStorage(
            Microsoft.Extensions.Options.Options.Create(new InspectionStorageOptions
            {
                UploadRoot = tempDirectory.Path,
                MaxFileSizeBytes = 1024 * 1024,
                AllowedExtensions = [".png", ".jpg"]
            }),
            new TestWebHostEnvironment(tempDirectory.Path));
    }

    public static InspectionUrlBuilder CreateUrlBuilder()
    {
        return new InspectionUrlBuilder(
            Microsoft.Extensions.Options.Options.Create(new InspectionMlOptions
            {
                PublicAppBaseUrl = "https://app.example.test/",
                WebhookPath = "api/ml/webhook"
            }),
            Microsoft.Extensions.Options.Options.Create(new InspectionStorageOptions
            {
                PublicFilePathPrefix = "inspection-files",
                TemplatePublicFilePathPrefix = "/inspection-template-files"
            }));
    }

    public static MlInspectionClient CreateMlClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ml.example.test")
        };

        return new MlInspectionClient(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new InspectionMlOptions
            {
                InspectPath = "/inspect-async",
                ApiKey = "test-key",
                ApiKeyHeaderName = "X-Test-Key"
            }));
    }

    public static MemoryStream ImageStream(string contents = "image")
        => new(System.Text.Encoding.UTF8.GetBytes(contents));
}
