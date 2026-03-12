using System.Security.Cryptography;
using client.Data;
using client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace client.Services;

public sealed class InspectionTemplateWorkflowService(
    ApplicationDbContext dbContext,
    InspectionFileStorage fileStorage)
{
    public async Task<InspectionTemplate> CreateAsync(
        string ownerUserId,
        string friendlyName,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream fileStream,
        decimal tolerancePercent,
        string? description,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            throw new InvalidOperationException("Friendly name is required.");
        }

        if (tolerancePercent < 0 || tolerancePercent > 100)
        {
            throw new InvalidOperationException("Tolerance must be between 0 and 100.");
        }

        var now = DateTimeOffset.UtcNow;
        var storedFile = await fileStorage.SaveAsync(originalFileName, contentType, fileSizeBytes, fileStream, cancellationToken);

        var template = new InspectionTemplate
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            FriendlyName = friendlyName.Trim(),
            OriginalFileName = Path.GetFileName(originalFileName),
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoredRelativePath = storedFile.RelativePath,
            PublicAccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TolerancePercent = decimal.Round(tolerancePercent, 2),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.InspectionTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<IReadOnlyList<InspectionTemplate>> GetUserTemplatesAsync(string ownerUserId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionTemplates
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InspectionTemplate?> GetUserTemplateAsync(string ownerUserId, Guid templateId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerUserId && x.Id == templateId, cancellationToken);
    }

    public async Task DeleteAsync(string ownerUserId, Guid templateId, CancellationToken cancellationToken)
    {
        var template = await dbContext.InspectionTemplates
            .SingleOrDefaultAsync(x => x.Id == templateId && x.OwnerUserId == ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Template not found.");

        var isReferenced = await dbContext.InspectionImages
            .AsNoTracking()
            .AnyAsync(x => x.TemplateId == templateId, cancellationToken);

        if (isReferenced)
        {
            throw new InvalidOperationException("Template cannot be deleted because it is used by one or more uploaded images.");
        }

        dbContext.InspectionTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        fileStorage.Delete(template.StoredRelativePath);
    }
}
