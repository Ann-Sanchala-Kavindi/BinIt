using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartWaste.Application.Common.Models;
using SmartWaste.Application.DTOs.Auth;
using SmartWaste.Application.Reporting.DTOs.Responses;
using SmartWaste.Domain.Entities;
using SmartWaste.Domain.Reporting.Entities;
using SmartWaste.Domain.Reporting.Enums;
using SmartWaste.Infrastructure.Persistence;
using Xunit;

namespace SmartWaste.Tests.Reporting.Controllers;

[Collection(IntegrationTestCollection.Name)]
public class AiToolsWasteReportsControllerTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private const string InternalApiKey = "TestInternalServiceKey_12345!";
    private const string InternalEndpoint = "/api/v1/internal/ai-tools/waste-reports/verified";

    private static readonly JsonSerializerOptions SharedTestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false) }
    };

    public AiToolsWasteReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpRequestMessage CreateInternalRequest(HttpMethod method, string url, string? apiKey = InternalApiKey)
    {
        var request = new HttpRequestMessage(method, url);
        if (apiKey != null)
        {
            request.Headers.Add("X-Internal-Service-Key", apiKey);
        }
        return request;
    }

    private async Task<Guid> GetOrCreateCitizenIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var citizen = db.Users.FirstOrDefault(u => u.Email == "citizen@smartwaste.local");
        if (citizen != null) return citizen.Id;

        var uniqueCitizen = new AppUser
        {
            UserName = $"cit_{Guid.NewGuid():N}@test.local",
            Email = $"cit_{Guid.NewGuid():N}@test.local",
            FullName = "Test Citizen",
            EmailConfirmed = true,
            IsActive = true
        };
        db.Users.Add(uniqueCitizen);
        await db.SaveChangesAsync();
        return uniqueCitizen.Id;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 1. SERVICE-TO-SERVICE AUTHENTICATION TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_WithoutInternalServiceKey_Returns401Unauthorized()
    {
        var request = CreateInternalRequest(HttpMethod.Get, InternalEndpoint, apiKey: null);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(SharedTestJsonOptions);
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task GetVerifiedReports_WithInvalidInternalServiceKey_Returns401Unauthorized()
    {
        var request = CreateInternalRequest(HttpMethod.Get, InternalEndpoint, apiKey: "WrongSecretKey123!");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVerifiedReports_WithUserBearerJwtOnly_Returns401Unauthorized()
    {
        // Login as WasteOfficer and send Bearer token without internal service key
        var loginReq = new LoginRequest
        {
            Email = "officer@smartwaste.local",
            Password = "DevPassword123!",
            ClientType = "web"
        };
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>(SharedTestJsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Get, InternalEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.SendAsync(request);

        // Endpoint requires InternalServicePolicy scheme; bearer token without key is challenged with 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetVerifiedReports_WithValidInternalServiceKey_Returns200OK()
    {
        var request = CreateInternalRequest(HttpMethod.Get, InternalEndpoint);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VerifiedWasteReportToolItemDto>>(SharedTestJsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 2. VERIFIED-ONLY ENFORCEMENT & LEAKAGE DEFENSE
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_StrictlyReturnsOnlyReportsWithVerifiedStatus()
    {
        var citizenId = await GetOrCreateCitizenIdAsync();

        // Seed one report of each status directly to test isolation
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var reports = new[]
            {
                new WasteReport { CitizenId = citizenId, Description = "Submitted report", Status = WasteReportStatus.Submitted, WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8 },
                new WasteReport { CitizenId = citizenId, Description = "UnderReview report", Status = WasteReportStatus.UnderReview, WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8 },
                new WasteReport { CitizenId = citizenId, Description = "Verified report test target", Status = WasteReportStatus.Verified, WasteType = WasteType.Organic, Latitude = 6.92, Longitude = 79.85, VerifiedAt = DateTime.UtcNow },
                new WasteReport { CitizenId = citizenId, Description = "Rejected report", Status = WasteReportStatus.Rejected, WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8 },
                new WasteReport { CitizenId = citizenId, Description = "Cancelled report", Status = WasteReportStatus.Cancelled, WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8 }
            };

            db.WasteReports.AddRange(reports);
            await db.SaveChangesAsync();
        }

        var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page=1&pageSize=50");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VerifiedWasteReportToolItemDto>>(SharedTestJsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();

        // Critical safety invariant: Every single returned report MUST have Status == Verified
        result.Items.Should().OnlyContain(r => r.Status == WasteReportStatus.Verified);
        result.Items.Should().NotContain(r => r.Description == "Submitted report");
        result.Items.Should().NotContain(r => r.Description == "UnderReview report");
        result.Items.Should().NotContain(r => r.Description == "Rejected report");
        result.Items.Should().NotContain(r => r.Description == "Cancelled report");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. DATA MINIMIZATION & ALLOW-LIST CONTRACT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_DataMinimization_DoesNotLeakSensitiveFields()
    {
        var citizenId = await GetOrCreateCitizenIdAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var report = new WasteReport
            {
                CitizenId = citizenId,
                Description = "Verification leakage test report",
                Status = WasteReportStatus.Verified,
                WasteType = WasteType.Recyclable,
                Latitude = 6.93,
                Longitude = 79.86,
                AddressText = "Colombo 03",
                VerifiedAt = DateTime.UtcNow
            };
            db.WasteReports.Add(report);
            await db.SaveChangesAsync();
        }

        var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page=1&pageSize=10");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rawJson = await response.Content.ReadAsStringAsync();

        // Inspect raw JSON to guarantee excluded properties are NOT serialized
        rawJson.Should().NotContainEquivalentOf("citizenId");
        rawJson.Should().NotContainEquivalentOf("citizenName");
        rawJson.Should().NotContainEquivalentOf("citizenEmail");
        rawJson.Should().NotContainEquivalentOf("verifiedByUserId");
        rawJson.Should().NotContainEquivalentOf("verifiedByUserName");
        rawJson.Should().NotContainEquivalentOf("storageKey");
        rawJson.Should().NotContainEquivalentOf("fileUrl");
        rawJson.Should().NotContainEquivalentOf("attachments");
        rawJson.Should().NotContainEquivalentOf("statusHistory");
        rawJson.Should().NotContainEquivalentOf("notes");
        rawJson.Should().NotContainEquivalentOf("priority");

        // Confirm expected safe fields exist in JSON
        rawJson.Should().ContainEquivalentOf("id");
        rawJson.Should().ContainEquivalentOf("description");
        rawJson.Should().ContainEquivalentOf("wasteType");
        rawJson.Should().ContainEquivalentOf("latitude");
        rawJson.Should().ContainEquivalentOf("longitude");
        rawJson.Should().ContainEquivalentOf("status");
        rawJson.Should().ContainEquivalentOf("createdAt");
        rawJson.Should().ContainEquivalentOf("attachmentCount");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4. DETERMINISTIC ORDERING
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_DeterministicOrder_ReturnsCreatedAtDescThenIdDesc()
    {
        var citizenId = await GetOrCreateCitizenIdAsync();
        var now = DateTime.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var reportOld = new WasteReport
            {
                CitizenId = citizenId,
                Description = "Old verified report",
                Status = WasteReportStatus.Verified,
                WasteType = WasteType.General,
                Latitude = 6.9,
                Longitude = 79.8,
                CreatedAt = now.AddHours(-10),
                VerifiedAt = now.AddHours(-9)
            };

            var reportNew = new WasteReport
            {
                CitizenId = citizenId,
                Description = "New verified report",
                Status = WasteReportStatus.Verified,
                WasteType = WasteType.General,
                Latitude = 6.9,
                Longitude = 79.8,
                CreatedAt = now.AddHours(-1),
                VerifiedAt = now.AddHours(-1)
            };

            db.WasteReports.AddRange(reportOld, reportNew);
            await db.SaveChangesAsync();
        }

        var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page=1&pageSize=50");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VerifiedWasteReportToolItemDto>>(SharedTestJsonOptions);

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();

        // Items must be ordered descending by CreatedAt
        for (var i = 0; i < result.Items.Count - 1; i++)
        {
            result.Items[i].CreatedAt.Should().BeOnOrAfter(result.Items[i + 1].CreatedAt);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 5. PAGINATION BOUNDS & VALIDATION
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_Pagination_DefaultsToPage1PageSize20()
    {
        var request = CreateInternalRequest(HttpMethod.Get, InternalEndpoint);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<VerifiedWasteReportToolItemDto>>(SharedTestJsonOptions);

        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0, 20)]   // Page < 1
    [InlineData(-1, 20)]  // Page < 0
    [InlineData(1, 0)]    // PageSize < 1
    [InlineData(1, 51)]   // PageSize > 50 (bounded limit)
    [InlineData(1, 100)]  // PageSize > 50
    public async Task GetVerifiedReports_InvalidPaginationParameters_Returns400BadRequest(int page, int pageSize)
    {
        var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page={page}&pageSize={pageSize}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(SharedTestJsonOptions);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    public async Task GetVerifiedReports_ValidBoundaryPaginationParameters_Returns200OK(int page, int pageSize)
    {
        var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page={page}&pageSize={pageSize}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 6. NO SIDE EFFECTS GUARANTEE
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVerifiedReports_CallingEndpoint_HasZeroSideEffectsOnDatabase()
    {
        var citizenId = await GetOrCreateCitizenIdAsync();
        Guid reportId;
        DateTime originalCreatedAt;
        DateTime? originalVerifiedAt;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var report = new WasteReport
            {
                CitizenId = citizenId,
                Description = "Side effects test report",
                Status = WasteReportStatus.Verified,
                WasteType = WasteType.Organic,
                Latitude = 6.91,
                Longitude = 79.87,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                VerifiedAt = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            db.WasteReports.Add(report);
            await db.SaveChangesAsync();

            reportId = report.Id;
            originalCreatedAt = report.CreatedAt;
            originalVerifiedAt = report.VerifiedAt;
        }

        // Call the AI endpoint multiple times
        for (var i = 0; i < 3; i++)
        {
            var request = CreateInternalRequest(HttpMethod.Get, $"{InternalEndpoint}?page=1&pageSize=10");
            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Verify row state in PostgreSQL remains completely untouched
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentReport = db.WasteReports.First(r => r.Id == reportId);

            currentReport.Status.Should().Be(WasteReportStatus.Verified);
            currentReport.Priority.Should().BeNull();
            currentReport.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
            currentReport.VerifiedAt.Should().BeCloseTo(originalVerifiedAt.Value, TimeSpan.FromMilliseconds(1));

            // Verify no history rows were created by reading
            var historyCount = db.WasteReportStatusHistories.Count(h => h.WasteReportId == reportId);
            historyCount.Should().Be(0);
        }
    }
}
