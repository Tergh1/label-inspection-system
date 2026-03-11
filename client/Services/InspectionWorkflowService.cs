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

        var now = DateTimeOffset.UtcNow;
        var storedFile = await fileStorage.SaveAsync(originalFileName, contentType, fileSizeBytes, fileStream, cancellationToken);

        var inspectionImage = new InspectionImage
        {
            Id = Guid.NewGuid(),
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

        await DispatchAsync(inspectionImage, cancellationToken);

        return inspectionImage;
    }

    public async Task<IReadOnlyList<InspectionImage>> GetUserImagesAsync(string ownerUserId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionImages
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InspectionImage?> GetUserImageAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        return await dbContext.InspectionImages
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == ownerUserId && x.Id == imageId, cancellationToken);
    }

    public async Task DeleteAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        var inspectionImage = await dbContext.InspectionImages
            .SingleOrDefaultAsync(x => x.Id == imageId && x.OwnerUserId == ownerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Image not found.");

        dbContext.InspectionImages.Remove(inspectionImage);
        await dbContext.SaveChangesAsync(cancellationToken);

        fileStorage.Delete(inspectionImage.StoredRelativePath);
    }

    public async Task<InspectionImage> RedispatchFailedAsync(string ownerUserId, Guid imageId, CancellationToken cancellationToken)
    {
        var inspectionImage = await dbContext.InspectionImages
            .SingleOrDefaultAsync(x => x.Id == imageId && x.OwnerUserId == ownerUserId, cancellationToken)
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

    private async Task DispatchAsync(InspectionImage inspectionImage, CancellationToken cancellationToken)
    {
        try
        {
            await mlInspectionClient.DispatchAsync(
                inspectionImage.Id,
                urlBuilder.BuildImageUrl(inspectionImage),
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
