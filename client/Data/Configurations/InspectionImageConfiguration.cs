using client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace client.Data.Configurations;

public sealed class InspectionImageConfiguration : IEntityTypeConfiguration<InspectionImage>
{
    public void Configure(EntityTypeBuilder<InspectionImage> builder)
    {
        builder.ToTable("InspectionImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalFileName).IsRequired();
        builder.Property(x => x.ContentType).IsRequired();
        builder.Property(x => x.StoredRelativePath).IsRequired();
        builder.Property(x => x.PublicAccessToken).IsRequired();
        builder.Property(x => x.OwnerUserId).IsRequired();

        builder.Property(x => x.TolerancePercent).HasPrecision(5, 2);
        builder.Property(x => x.MinimumSimilarityPercent).HasPrecision(5, 2);
        builder.Property(x => x.SimilarityPercent).HasPrecision(5, 2);

        builder.Property(x => x.DefectsJson).HasColumnType("jsonb");

        builder.Property(x => x.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.OutcomeStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ProcessingStatus);
        builder.HasIndex(x => x.PublicAccessToken).IsUnique();

        builder.HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
