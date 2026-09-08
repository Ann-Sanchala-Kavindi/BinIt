using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartWaste.Application.Common.Options;
using SmartWaste.Application.Interfaces;
using SmartWaste.Domain.Entities;
using SmartWaste.Infrastructure.Persistence;
using SmartWaste.Infrastructure.Services;

namespace SmartWaste.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services with the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure services including the database context, Identity, and AuthService.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context with PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(AppDbContext).Assembly.FullName)
            )
        );

        // ASP.NET Core Identity configuration
        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
        {
            // Password requirements for university project
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // User & Email requirements
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Register application services
        services.AddScoped<IAuthService, AuthService>();

        // Internal FastAPI AI Service typed client registration
        var aiSection = configuration.GetSection(AiServiceOptions.SectionName);
        services.Configure<AiServiceOptions>(aiSection);

        var aiBaseUrl = aiSection["BaseUrl"];
        if (string.IsNullOrWhiteSpace(aiBaseUrl))
        {
            aiBaseUrl = "http://127.0.0.1:8000";
        }

        var timeoutSeconds = 5;
        if (int.TryParse(aiSection["TimeoutSeconds"], out var parsedTimeout) && parsedTimeout > 0)
        {
            timeoutSeconds = parsedTimeout;
        }

        services.AddHttpClient<IAiServiceClient, AiServiceClient>(client =>
        {
            client.BaseAddress = new Uri(aiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}
