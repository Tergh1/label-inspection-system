using System.ComponentModel.DataAnnotations;

namespace client.Data.Entities;

public sealed class InspectionTemplate
{
    public Guid Id { get; set; }

    [MaxLength(450)]
    public required string OwnerUserId { get; set; }

    [MaxLength(200)]
    public required string FriendlyName { get; set; }

    [MaxLength(260)]
    public required string OriginalFileName { get; set; }

    [MaxLength(50)]
    public required string ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    [MaxLength(512)]
    public required string StoredRelativePath { get; set; }

    [MaxLength(64)]
    public required string PublicAccessToken { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public decimal TolerancePercent { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ApplicationUser? OwnerUser { get; set; }
}
