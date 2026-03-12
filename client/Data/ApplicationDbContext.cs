using client.Data.Configurations;
using client.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace client.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<InspectionImage> InspectionImages => Set<InspectionImage>();
    public DbSet<InspectionTemplate> InspectionTemplates => Set<InspectionTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new InspectionImageConfiguration());
        builder.ApplyConfiguration(new InspectionTemplateConfiguration());
    }
}
