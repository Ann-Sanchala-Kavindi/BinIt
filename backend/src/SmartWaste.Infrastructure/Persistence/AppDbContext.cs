using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartWaste.Domain.Collection.Entities;
using SmartWaste.Domain.Entities;
using SmartWaste.Domain.Reporting.Entities;

namespace SmartWaste.Infrastructure.Persistence;

/// <summary>
/// Main EF Core database context configured for ASP.NET Core Identity with AppUser.
/// Business DbSets will be added in subsequent component implementations.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Component 1 — Waste Reporting & Citizen Management
    public DbSet<WasteReport> WasteReports => Set<WasteReport>();
    public DbSet<ReportAttachment> ReportAttachments => Set<ReportAttachment>();
    public DbSet<WasteReportStatusHistory> WasteReportStatusHistories => Set<WasteReportStatusHistory>();

    // Component 2 — Waste Collection & Bin Management
    public DbSet<WasteBin> WasteBins => Set<WasteBin>();
    public DbSet<WasteBinAcceptedWasteType> WasteBinAcceptedWasteTypes => Set<WasteBinAcceptedWasteType>();
    public DbSet<BinObservation> BinObservations => Set<BinObservation>();
    public DbSet<CollectionTask> CollectionTasks => Set<CollectionTask>();
    public DbSet<CollectionTaskStatusHistory> CollectionTaskStatusHistories => Set<CollectionTaskStatusHistory>();
    public DbSet<CollectionTaskScheduleHistory> CollectionTaskScheduleHistories => Set<CollectionTaskScheduleHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // AppUser configuration
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            entity.Property(u => u.MustChangePassword)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(u => u.CreatedAt)
                .IsRequired();
        });
    }
}
