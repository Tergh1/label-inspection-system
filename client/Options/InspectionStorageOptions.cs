namespace client.Options;

public sealed class InspectionStorageOptions
{
    public const string SectionName = "InspectionStorage";

    public string UploadRoot { get; set; } = "App_Data/uploads";

    public string PublicFilePathPrefix { get; set; } = "/inspection-files";

    public string TemplatePublicFilePathPrefix { get; set; } = "/inspection-template-files";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".png", ".jpg", ".jpeg", ".bmp", ".webp"];
}
