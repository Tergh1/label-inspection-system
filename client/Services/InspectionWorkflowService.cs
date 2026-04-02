using System.Security.Cryptography;
using client.Data;
using client.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace client.Services;

public sealed class InspectionWorkflowService(
    ApplicationDbContext dbContext,
    InspectionFileStorage fileStorage,
    InspectionUrlBuilder urlBuilder,
    MlInspectionClient mlInspectionClient)
{
    public async Task<InspectionImage> CreateAsync(
        string ownerUserId,
        Guid templateId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream fileStream,
        decimal tolerancePercent,
        string? description,
        CancellationToken cancellationToken)
    {
        if (tolerancePercent < 0 || tolerancePercent > 100)
        {
            throw new InvalidOperationException("Tolerance must be between 0 and 100.");
        }

        var template = await dbContext.InspectionTemplates
            .SingleOrDefaultAsync(x => x.Id == templateId && x.OwnerUserId == ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Template not found.");

        var now = DateTimeOffset.UtcNow;
        var storedFile = await fileStorage.SaveAsync(originalFileName, contentType, fileSizeBytes, fileStream, cancellationToken);

        var inspectionImage = new InspectionImage
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            OwnerUserId = ownerUserId,
            OriginalFileName = Path.GetFileName(originalFileName),
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoredRelativePath = storedFile.RelativePath,
            PublicAccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            TolerancePercent = decimal.Round(tolerancePercent, 2),
            MinimumSimilarityPercent = decimal.Round(100 - tolerancePercent, 2),
            ProcessingStatus = ProcessingStatus.PendingDispatch,
            OutcomeStatus = OutcomeStatus.Pending,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.InspectionImages.Add(inspectionImage);
        await dbContext.SaveChangesAsync(cancellationToken);

        inspectionImage.Template = template;
        await DispatchAsync(inspectionImage, cancellationToken);

        return inspectionImage;
    }

    public async Task<IReadOnlyList<InspectionImage>> GetUserImagesAsync(string ownerUserId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionImages
            .AsNoTracking()
            .Include(x => x.Template)
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InspectionImage?> GetUserImageAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionImages
            .AsNoTracking()
            .Include(x => x.Template)
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerUserId && x.Id == imageId, cancellationToken);
    }

    public async Task DeleteAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        var inspectionImage = await dbContext.InspectionImages
            .Include(x => x.Template)
            .SingleOrDefaultAsync(x => x.Id == imageId && x.OwnerUserId == ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Image not found.");

        dbContext.InspectionImages.Remove(inspectionImage);
        await dbContext.SaveChangesAsync(cancellationToken);

        fileStorage.Delete(inspectionImage.StoredRelativePath);
    }

    public async Task<InspectionImage> RedispatchFailedAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        var inspectionImage = await LoadTrackedInspectionImageAsync(ownerUserId, imageId, cancellationToken)
            ?? throw new InvalidOperationException("Image not found.");

        if (inspectionImage.ProcessingStatus != ProcessingStatus.Failed)
        {
            throw new InvalidOperationException("Only failed images can be inspected again.");
        }

        inspectionImage.ProcessingStatus = ProcessingStatus.PendingDispatch;
        inspectionImage.OutcomeStatus = OutcomeStatus.Pending;
        inspectionImage.SimilarityPercent = null;
        inspectionImage.DefectsJson = null;
        inspectionImage.FailureReason = null;
        inspectionImage.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await DispatchAsync(inspectionImage, cancellationToken);
        return inspectionImage;
    }

    private async Task<InspectionImage?> LoadTrackedInspectionImageAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        DetachTrackedInspectionImage(imageId);

        return await dbContext.InspectionImages
            .Include(x => x.Template)
            .SingleOrDefaultAsync(x => x.Id == imageId && x.OwnerUserId == ownerUserId, cancellationToken);
    }

    private void DetachTrackedInspectionImage(Guid imageId)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<InspectionImage>().Where(x => x.Entity.Id == imageId).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task DispatchAsync(InspectionImage inspectionImage, CancellationToken cancellationToken)
    {
        try
        {
            var templateId = inspectionImage.TemplateId
                ?? throw new InvalidOperationException("This inspection image does not have a template assigned.");

            var template = inspectionImage.Template ?? await dbContext.InspectionTemplates
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken)
                ?? throw new InvalidOperationException("Template not found.");

            await mlInspectionClient.DispatchAsync(
                inspectionImage.Id,
                urlBuilder.BuildImageUrl(inspectionImage),
                urlBuilder.BuildTemplateUrl(template),
                urlBuilder.BuildWebhookUrl(),
                cancellationToken);

            inspectionImage.ProcessingStatus = ProcessingStatus.Queued;
            inspectionImage.FailureReason = null;
        }
        catch (Exception ex)
        {
            inspectionImage.ProcessingStatus = ProcessingStatus.Failed;
            inspectionImage.FailureReason = ex.Message;
        }

        inspectionImage.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
