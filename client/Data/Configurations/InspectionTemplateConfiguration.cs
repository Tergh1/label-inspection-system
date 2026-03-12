using client.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace client.Data.Configurations;

public sealed class InspectionTemplateConfiguration : IEntityTypeConfiguration<InspectionTemplate>
{
    public void Configure(EntityTypeBuilder<InspectionTemplate> builder)
    {
        builder.ToTable("InspectionTemplates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.FriendlyName).IsRequired();
        builder.Property(x => x.OriginalFileName).IsRequired();
        builder.Property(x => x.ContentType).IsRequired();
        builder.Property(x => x.StoredRelativePath).IsRequired();
        builder.Property(x => x.PublicAccessToken).IsRequired();
        builder.Property(x => x.TolerancePercent).HasPrecision(5, 2);

        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.PublicAccessToken).IsUnique();

        builder.HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
