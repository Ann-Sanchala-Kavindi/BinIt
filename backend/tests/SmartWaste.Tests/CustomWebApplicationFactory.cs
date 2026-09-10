using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SmartWaste.Tests;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var builtConfig = config.Build();
            var existingConn = builtConfig.GetConnectionString("DefaultConnection");
            var connToUse = (!string.IsNullOrWhiteSpace(existingConn) && !existingConn.Contains("CHANGE_ME"))
                ? existingConn
                : "Host=localhost;Port=5432;Database=smartwaste_db;Username=postgres;Password=postgres";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connToUse,
                ["Jwt:Key"] = builtConfig["Jwt:Key"] ?? "ThisIsASecretKeyForSmartWasteDevelopmentOnly12345!",
                ["Jwt:Issuer"] = "SmartWaste.Api",
                ["Jwt:Audience"] = "SmartWaste.Clients",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
    }
}
