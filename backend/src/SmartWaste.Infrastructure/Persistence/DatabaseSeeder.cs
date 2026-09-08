using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartWaste.Domain.Common;
using SmartWaste.Domain.Entities;

namespace SmartWaste.Infrastructure.Persistence;

/// <summary>
/// Idempotent database seeder for initial Identity roles and development test accounts.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedDatabaseAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();

        // 1. Seed Roles idempotently
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Seeded Identity role: {Role}", roleName);
                }
                else
                {
                    logger.LogWarning("Failed to seed role {Role}: {Errors}",
                        roleName,
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        // 2. Seed development accounts only in Development environment
        if (isDevelopment)
        {
            await SeedDevelopmentUsersAsync(userManager, configuration, logger);
        }
    }

    private static async Task SeedDevelopmentUsersAsync(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Password for development accounts - default for local university testing only
        var devPassword = configuration["Seed:DevUserPassword"] ?? "DevPassword123!";

        var devUsers = new List<(string Email, string FullName, string Phone, string Role)>
        {
            ("manager@smartwaste.local", "Municipal Manager", "+94770000001", AppRoles.MunicipalManager),
            ("officer@smartwaste.local", "Waste Officer", "+94770000002", AppRoles.WasteOfficer),
            ("driver@smartwaste.local", "Waste Driver", "+94770000003", AppRoles.Driver),
            ("citizen@smartwaste.local", "Test Citizen", "+94770000004", AppRoles.Citizen)
        };

        foreach (var devUser in devUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(devUser.Email);
            if (existingUser == null)
            {
                var user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = devUser.Email,
                    Email = devUser.Email,
                    FullName = devUser.FullName,
                    PhoneNumber = devUser.Phone,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, devPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, devUser.Role);
                    logger.LogInformation("Seeded development account: {Email} ({Role})", devUser.Email, devUser.Role);
                }
                else
                {
                    logger.LogWarning("Failed to create development user {Email}: {Errors}",
                        devUser.Email,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
