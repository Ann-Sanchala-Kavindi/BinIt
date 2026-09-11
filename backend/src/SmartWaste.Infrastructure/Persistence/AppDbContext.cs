using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartWaste.Domain.Entities;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
