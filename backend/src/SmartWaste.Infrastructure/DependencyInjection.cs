using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartWaste.Application.Common.Options;
using SmartWaste.Application.Interfaces;
using SmartWaste.Application.Reporting.Interfaces;
using SmartWaste.Domain.Entities;
using SmartWaste.Infrastructure.Persistence;
using SmartWaste.Infrastructure.Reporting.Services;
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
        services.AddScoped<IUserService, UserService>();

        // Component 1 — Waste Reporting & Citizen Management
        services.AddScoped<IWasteReportService, WasteReportService>();
        services.AddScoped<IWasteReportAttachmentService, WasteReportAttachmentService>();

        // Cloud Storage — Supabase Storage
        var storageSection = configuration.GetSection(SmartWaste.Infrastructure.Reporting.Storage.SupabaseStorageOptions.SectionName);
        services.Configure<SmartWaste.Infrastructure.Reporting.Storage.SupabaseStorageOptions>(storageSection);

        services.AddHttpClient<IFileStorageService, SmartWaste.Infrastructure.Reporting.Storage.SupabaseFileStorageService>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmartWaste.Infrastructure.Reporting.Storage.SupabaseStorageOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl) && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });

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
