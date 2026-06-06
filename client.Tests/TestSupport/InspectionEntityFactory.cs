using client.Data.Entities;

namespace client.Tests.TestSupport;

internal static class InspectionEntityFactory
{
    public static InspectionTemplate Template(
        Guid? id = null,
        string ownerUserId = "owner-1",
        string friendlyName = "Template",
        DateTimeOffset? createdAtUtc = null)
    {
        var now = createdAtUtc ?? DateTimeOffset.UtcNow;

        return new InspectionTemplate
        {
            Id = id ?? Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            FriendlyName = friendlyName,
            OriginalFileName = "template.png",
            ContentType = "image/png",
            FileSizeBytes = 10,
            StoredRelativePath = "templates/template.png",
            PublicAccessToken = Guid.NewGuid().ToString("N"),
            TolerancePercent = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public static InspectionImage Image(
        Guid? id = null,
        Guid? templateId = null,
        InspectionTemplate? template = null,
        string ownerUserId = "owner-1",
        string originalFileName = "image.png",
        ProcessingStatus processingStatus = ProcessingStatus.Completed,
        OutcomeStatus outcomeStatus = OutcomeStatus.Valid,
        DateTimeOffset? createdAtUtc = null)
    {
        var now = createdAtUtc ?? DateTimeOffset.UtcNow;

        return new InspectionImage
        {
            Id = id ?? Guid.NewGuid(),
            TemplateId = templateId ?? template?.Id,
            Template = template,
            OwnerUserId = ownerUserId,
            OriginalFileName = originalFileName,
            ContentType = "image/png",
            FileSizeBytes = 10,
            StoredRelativePath = "images/image.png",
            PublicAccessToken = Guid.NewGuid().ToString("N"),
            TolerancePercent = 5,
            MinimumSimilarityPercent = 95,
            ProcessingStatus = processingStatus,
            OutcomeStatus = outcomeStatus,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }
}
