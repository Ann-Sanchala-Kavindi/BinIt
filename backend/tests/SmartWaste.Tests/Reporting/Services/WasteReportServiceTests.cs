using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartWaste.Application.Common.Exceptions;
using SmartWaste.Application.Reporting.DTOs.Requests;
using SmartWaste.Application.Reporting.Interfaces;
using SmartWaste.Application.Reporting.Queries;
using SmartWaste.Domain.Common;
using SmartWaste.Domain.Entities;
using SmartWaste.Domain.Reporting.Entities;
using SmartWaste.Domain.Reporting.Enums;
using SmartWaste.Infrastructure.Persistence;
using SmartWaste.Infrastructure.Reporting.Services;
using SmartWaste.Tests.Reporting.Fakes;
using Xunit;

namespace SmartWaste.Tests.Reporting.Services;

/// <summary>
/// Unit tests for WasteReportService using EF Core InMemory provider.
/// Each test gets an isolated DbContext to prevent state leakage.
/// UserManager is seeded with real AppUser rows via InMemory Identity stores.
/// </summary>
public class WasteReportServiceTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // TEST INFRASTRUCTURE
    // ──────────────────────────────────────────────────────────────────────────

    private static (AppDbContext Db, UserManager<AppUser> UserManager) CreateContext()
    {
        var dbName = $"SmartWaste_Reporting_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        var userStore = new UserStore<AppUser, IdentityRole<Guid>, AppDbContext, Guid>(db);
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var keyNorm = new UpperInvariantLookupNormalizer();
        var errDescriber = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var loggerMock = new Mock<ILogger<UserManager<AppUser>>>();

        var userManager = new UserManager<AppUser>(
            userStore,
            new Mock<IOptions<IdentityOptions>>().Object,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNorm,
            errDescriber,
            services,
            loggerMock.Object
        );

        return (db, userManager);
    }

    private static WasteReportService CreateService(
        AppDbContext db,
        UserManager<AppUser> userManager,
        IFileStorageService? fileStorageService = null)
    {
        fileStorageService ??= new FakeFileStorageService();
        return new WasteReportService(db, userManager, fileStorageService);
    }

    private static async Task<AppUser> SeedUserAsync(
        AppDbContext db,
        UserManager<AppUser> userManager,
        string fullName = "Test User",
        string? email = null)
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            UserName = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.NormalizedEmail = email.ToUpperInvariant();
        user.NormalizedUserName = email.ToUpperInvariant();
        await userManager.CreateAsync(user, "Password1!");
        return user;
    }

    private static async Task<WasteReport> SeedReportAsync(
        AppDbContext db,
        Guid citizenId,
        WasteReportStatus status = WasteReportStatus.Submitted)
    {
        var report = new WasteReport
        {
            Id = Guid.NewGuid(),
            CitizenId = citizenId,
            Description = "Test garbage heap near the bus stop on Main Street",
            WasteType = WasteType.General,
            Latitude = 6.9271,
            Longitude = 79.8612,
            AddressText = "Main Street, Pettah",
            Status = status,
            Priority = null,
            CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    private static CreateWasteReportRequest ValidCreateRequest(string desc = "Large pile of garbage dumped near road junction") =>
        new()
        {
            Description = desc,
            WasteType = WasteType.General,
            Latitude = 6.9271,
            Longitude = 79.8612,
            AddressText = "Main Street, Pettah"
        };

    // ──────────────────────────────────────────────────────────────────────────
    // CREATE TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Citizen_ShouldSucceed()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um, "Kamal Silva");
        var svc = CreateService(db, um);

        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);

        dto.Should().NotBeNull();
        dto.Status.Should().Be(WasteReportStatus.Submitted);
    }

    [Fact]
    public async Task CreateAsync_CitizenId_SourcedFromActor_NotRequest()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um, "Kamal Silva");
        var svc = CreateService(db, um);

        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);

        dto.CitizenId.Should().Be(citizen.Id);
    }

    [Fact]
    public async Task CreateAsync_SetsSubmittedStatus_AndNullPriority()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);

        dto.Status.Should().Be(WasteReportStatus.Submitted);
        dto.Priority.Should().BeNull();
        dto.VerifiedByUserId.Should().BeNull();
        dto.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesInitialStatusHistory()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);

        var history = await db.WasteReportStatusHistories.Where(h => h.WasteReportId == dto.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].FromStatus.Should().BeNull();
        history[0].ToStatus.Should().Be(WasteReportStatus.Submitted);
        history[0].ChangedByUserId.Should().Be(citizen.Id);
    }

    [Fact]
    public async Task CreateAsync_TimestampsAreUtc()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var before = DateTime.UtcNow;
        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);
        var after = DateTime.UtcNow;

        dto.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        dto.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        dto.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(AppRoles.WasteOfficer)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task CreateAsync_NonCitizen_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var actor = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var act = () => svc.CreateAsync(ValidCreateRequest(), actor.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET BY ID TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Citizen_OwnReport_Succeeds()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var dto = await svc.GetByIdAsync(report.Id, citizen.Id, AppRoles.Citizen);

        dto.Id.Should().Be(report.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Citizen_OtherCitizenReport_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen1 = await SeedUserAsync(db, um);
        var citizen2 = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen1.Id);
        var svc = CreateService(db, um);

        var act = () => svc.GetByIdAsync(report.Id, citizen2.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Theory]
    [InlineData(AppRoles.WasteOfficer)]
    [InlineData(AppRoles.MunicipalManager)]
    public async Task GetByIdAsync_StaffRoles_CanViewAnyReport(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var dto = await svc.GetByIdAsync(report.Id, officer.Id, role);

        dto.Id.Should().Be(report.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Driver_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var driver = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var act = () => svc.GetByIdAsync(report.Id, driver.Id, AppRoles.Driver);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_MissingReport_ThrowsNotFound()
    {
        var (db, um) = CreateContext();
        var officer = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var act = () => svc.GetByIdAsync(Guid.NewGuid(), officer.Id, AppRoles.WasteOfficer);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LIST TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetListAsync_Citizen_OnlySeesOwnReports()
    {
        var (db, um) = CreateContext();
        var citizen1 = await SeedUserAsync(db, um);
        var citizen2 = await SeedUserAsync(db, um);
        await SeedReportAsync(db, citizen1.Id);
        await SeedReportAsync(db, citizen1.Id);
        await SeedReportAsync(db, citizen2.Id);
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(new WasteReportListQuery(), citizen1.Id, AppRoles.Citizen);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(r => r.CitizenId.Should().BeNull()); // Citizen summary hides CitizenId
    }

    [Fact]
    public async Task GetListAsync_WasteOfficer_SeesAllReports()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        await SeedReportAsync(db, citizen.Id);
        await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(new WasteReportListQuery(), officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetListAsync_MunicipalManager_SeesAllReports()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var manager = await SeedUserAsync(db, um);
        await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(new WasteReportListQuery(), manager.Id, AppRoles.MunicipalManager);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListAsync_Driver_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var driver = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var act = () => svc.GetListAsync(new WasteReportListQuery(), driver.Id, AppRoles.Driver);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetListAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        await SeedReportAsync(db, citizen.Id, WasteReportStatus.Submitted);
        await SeedReportAsync(db, citizen.Id, WasteReportStatus.Verified);
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { Status = WasteReportStatus.Submitted },
            officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(WasteReportStatus.Submitted);
    }

    [Fact]
    public async Task GetListAsync_WasteTypeFilter_ReturnsOnlyMatchingType()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        await SeedReportAsync(db, citizen.Id); // General
        var hazardous = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "Chemical drums dumped near river bank",
            WasteType = WasteType.Hazardous, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted, CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.Add(hazardous);
        await db.SaveChangesAsync();
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { WasteType = WasteType.Hazardous },
            officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(1);
        result.Items[0].WasteType.Should().Be(WasteType.Hazardous);
    }

    [Fact]
    public async Task GetListAsync_Search_CaseInsensitiveDescription()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var r = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "HOSPITAL waste overflow on Galle Road",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted, CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.Add(r);
        await db.SaveChangesAsync();
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { Search = "hospital" },
            officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListAsync_Search_AddressText()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var r = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "Garbage pile near corner",
            AddressText = "PETTAH Market Street",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted, CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.Add(r);
        await db.SaveChangesAsync();
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { Search = "pettah" },
            officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListAsync_FromDate_FiltersCorrectly()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var old = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "Old report well before the cutoff date here",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var recent = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "Recent report created just after the cutoff date",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.AddRange(old, recent);
        await db.SaveChangesAsync();
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { FromDate = DateTime.UtcNow.AddDays(-1) },
            officer.Id, AppRoles.WasteOfficer);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(recent.Id);
    }

    [Fact]
    public async Task GetListAsync_Pagination_TotalCountAndPagesCorrect()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        for (var i = 0; i < 5; i++) await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(
            new WasteReportListQuery { Page = 1, PageSize = 3 },
            officer.Id, AppRoles.WasteOfficer);

        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(2);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetListAsync_SortByCreatedAtDesc_IsDefault()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var r1 = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "First report created earliest by timestamp",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var r2 = new WasteReport
        {
            Id = Guid.NewGuid(), CitizenId = citizen.Id,
            Description = "Second report created later than first report",
            WasteType = WasteType.General, Latitude = 6.9, Longitude = 79.8,
            Status = WasteReportStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };
        db.WasteReports.AddRange(r1, r2);
        await db.SaveChangesAsync();
        var svc = CreateService(db, um);

        var result = await svc.GetListAsync(new WasteReportListQuery(), officer.Id, AppRoles.WasteOfficer);

        result.Items[0].Id.Should().Be(r2.Id); // newest first
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_CitizenOwner_Submitted_Succeeds()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var req = new UpdateWasteReportRequest { Description = "Updated description with more than ten chars" };
        var dto = await svc.UpdateAsync(report.Id, req, citizen.Id, AppRoles.Citizen);

        dto.Description.Should().Be(req.Description);
        dto.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(AppRoles.WasteOfficer)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task UpdateAsync_NonCitizen_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var other = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var act = () => svc.UpdateAsync(report.Id, new UpdateWasteReportRequest { Description = "Updated valid description here" }, other.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateAsync_DifferentCitizen_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen1 = await SeedUserAsync(db, um);
        var citizen2 = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen1.Id);
        var svc = CreateService(db, um);

        var act = () => svc.UpdateAsync(report.Id, new UpdateWasteReportRequest { Description = "Trying to update other person report" }, citizen2.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateAsync_NonSubmittedReport_ThrowsConflict()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var act = () => svc.UpdateAsync(report.Id, new UpdateWasteReportRequest { Description = "Trying to update under review report" }, citizen.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<BusinessRuleConflictException>();
    }

    [Fact]
    public async Task UpdateAsync_AddressTextNull_KeepsExisting()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var originalAddress = report.AddressText;
        var svc = CreateService(db, um);

        // AddressText omitted (null) → no change
        var req = new UpdateWasteReportRequest { Description = "Description updated keeping same address" };
        var dto = await svc.UpdateAsync(report.Id, req, citizen.Id, AppRoles.Citizen);

        dto.AddressText.Should().Be(originalAddress);
    }

    [Fact]
    public async Task UpdateAsync_AddressTextEmptyString_ClearsToNull()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        // AddressText = "" → clear (persisted as null)
        var req = new UpdateWasteReportRequest { AddressText = "" };
        var dto = await svc.UpdateAsync(report.Id, req, citizen.Id, AppRoles.Citizen);

        dto.AddressText.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_DoesNotCreateStatusHistory()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        await svc.UpdateAsync(report.Id, new UpdateWasteReportRequest { Description = "Description changed without status change" }, citizen.Id, AppRoles.Citizen);

        var historyCount = await db.WasteReportStatusHistories
            .CountAsync(h => h.WasteReportId == report.Id);
        historyCount.Should().Be(0); // no status transition, so no history
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var before = DateTime.UtcNow;
        var dto = await svc.UpdateAsync(report.Id, new UpdateWasteReportRequest { Description = "Updated description for updatedAt check" }, citizen.Id, AppRoles.Citizen);

        dto.UpdatedAt.Should().NotBeNull();
        dto.UpdatedAt!.Value.Should().BeOnOrAfter(before);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CANCEL TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_CitizenOwner_Submitted_TransitionsToCancelled()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var dto = await svc.CancelAsync(report.Id, citizen.Id, AppRoles.Citizen);

        dto.Status.Should().Be(WasteReportStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_CreatesHistoryWithCancelledByNote()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        await svc.CancelAsync(report.Id, citizen.Id, AppRoles.Citizen);

        var history = await db.WasteReportStatusHistories
            .Where(h => h.WasteReportId == report.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].FromStatus.Should().Be(WasteReportStatus.Submitted);
        history[0].ToStatus.Should().Be(WasteReportStatus.Cancelled);
        history[0].Notes.Should().Be("Cancelled by citizen");
    }

    [Fact]
    public async Task CancelAsync_DoesNotDeleteDatabaseRow()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        await svc.CancelAsync(report.Id, citizen.Id, AppRoles.Citizen);

        var exists = await db.WasteReports.AnyAsync(r => r.Id == report.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_WrongCitizen_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen1 = await SeedUserAsync(db, um);
        var citizen2 = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen1.Id);
        var svc = CreateService(db, um);

        var act = () => svc.CancelAsync(report.Id, citizen2.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task CancelAsync_NonSubmitted_ThrowsConflict()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var act = () => svc.CancelAsync(report.Id, citizen.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<BusinessRuleConflictException>();
    }

    [Theory]
    [InlineData(AppRoles.WasteOfficer)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task CancelAsync_NonCitizenRole_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var other = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var act = () => svc.CancelAsync(report.Id, other.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // START REVIEW TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StartReviewAsync_WasteOfficer_Submitted_TransitionsToUnderReview()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var dto = await svc.StartReviewAsync(report.Id, officer.Id, AppRoles.WasteOfficer);

        dto.Status.Should().Be(WasteReportStatus.UnderReview);
    }

    [Fact]
    public async Task StartReviewAsync_CreatesHistory()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        await svc.StartReviewAsync(report.Id, officer.Id, AppRoles.WasteOfficer);

        var history = await db.WasteReportStatusHistories
            .Where(h => h.WasteReportId == report.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].FromStatus.Should().Be(WasteReportStatus.Submitted);
        history[0].ToStatus.Should().Be(WasteReportStatus.UnderReview);
    }

    [Theory]
    [InlineData(AppRoles.Citizen)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task StartReviewAsync_NonOfficer_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var other = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var act = () => svc.StartReviewAsync(report.Id, other.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Theory]
    [InlineData(WasteReportStatus.UnderReview)]
    [InlineData(WasteReportStatus.Verified)]
    [InlineData(WasteReportStatus.Rejected)]
    [InlineData(WasteReportStatus.Cancelled)]
    public async Task StartReviewAsync_NonSubmitted_ThrowsConflict(WasteReportStatus status)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, status);
        var svc = CreateService(db, um);

        var act = () => svc.StartReviewAsync(report.Id, officer.Id, AppRoles.WasteOfficer);

        await act.Should().ThrowAsync<BusinessRuleConflictException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VERIFY TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_WasteOfficer_UnderReview_TransitionsToVerified()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var dto = await svc.VerifyAsync(report.Id, new VerifyWasteReportRequest { Priority = WasteReportPriority.Low }, officer.Id, AppRoles.WasteOfficer);

        dto.Status.Should().Be(WasteReportStatus.Verified);
    }

    [Fact]
    public async Task VerifyAsync_SetsVerificationFields()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um, "Officer Silva");
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var before = DateTime.UtcNow;
        var dto = await svc.VerifyAsync(report.Id, new VerifyWasteReportRequest { Priority = WasteReportPriority.Medium }, officer.Id, AppRoles.WasteOfficer);

        dto.VerifiedByUserId.Should().Be(officer.Id);
        dto.VerifiedAt.Should().NotBeNull();
        dto.VerifiedAt!.Value.Should().BeOnOrAfter(before);
        dto.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyAsync_PersistsSelectedPriority()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var dto = await svc.VerifyAsync(report.Id, new VerifyWasteReportRequest { Priority = WasteReportPriority.Urgent }, officer.Id, AppRoles.WasteOfficer);

        dto.Priority.Should().Be(WasteReportPriority.Urgent);
    }

    [Fact]
    public async Task VerifyAsync_CreatesHistory()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        await svc.VerifyAsync(report.Id, new VerifyWasteReportRequest { Priority = WasteReportPriority.High }, officer.Id, AppRoles.WasteOfficer);

        var history = await db.WasteReportStatusHistories
            .Where(h => h.WasteReportId == report.Id).ToListAsync();
        history.Should().HaveCount(1);
        history[0].FromStatus.Should().Be(WasteReportStatus.UnderReview);
        history[0].ToStatus.Should().Be(WasteReportStatus.Verified);
    }

    [Theory]
    [InlineData(WasteReportStatus.Submitted)]
    [InlineData(WasteReportStatus.Verified)]
    [InlineData(WasteReportStatus.Rejected)]
    [InlineData(WasteReportStatus.Cancelled)]
    public async Task VerifyAsync_NonUnderReview_ThrowsConflict(WasteReportStatus status)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, status);
        var svc = CreateService(db, um);

        var act = () => svc.VerifyAsync(report.Id, officer.Id, AppRoles.WasteOfficer);

        await act.Should().ThrowAsync<BusinessRuleConflictException>();
    }

    [Theory]
    [InlineData(AppRoles.Citizen)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task VerifyAsync_NonOfficer_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var other = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var act = () => svc.VerifyAsync(report.Id, other.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REJECT TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_WasteOfficer_UnderReview_TransitionsToRejected()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var dto = await svc.RejectAsync(report.Id, new RejectWasteReportRequest { Reason = "Duplicate report already handled." }, officer.Id, AppRoles.WasteOfficer);

        dto.Status.Should().Be(WasteReportStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_HistoryNotesContainReason()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var reason = "Duplicate report already covered by scheduled pickup route.";
        await svc.RejectAsync(report.Id, new RejectWasteReportRequest { Reason = reason }, officer.Id, AppRoles.WasteOfficer);

        var history = await db.WasteReportStatusHistories
            .Where(h => h.WasteReportId == report.Id).FirstAsync();
        history.Notes.Should().Be(reason);
    }

    [Fact]
    public async Task RejectAsync_DoesNotSetVerificationFields()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var dto = await svc.RejectAsync(report.Id, new RejectWasteReportRequest { Reason = "Invalid location provided." }, officer.Id, AppRoles.WasteOfficer);

        dto.VerifiedByUserId.Should().BeNull();
        dto.VerifiedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(WasteReportStatus.Submitted)]
    [InlineData(WasteReportStatus.Verified)]
    [InlineData(WasteReportStatus.Rejected)]
    [InlineData(WasteReportStatus.Cancelled)]
    public async Task RejectAsync_NonUnderReview_ThrowsConflict(WasteReportStatus status)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, status);
        var svc = CreateService(db, um);

        var act = () => svc.RejectAsync(report.Id, new RejectWasteReportRequest { Reason = "Test rejection reason." }, officer.Id, AppRoles.WasteOfficer);

        await act.Should().ThrowAsync<BusinessRuleConflictException>();
    }

    [Theory]
    [InlineData(AppRoles.Citizen)]
    [InlineData(AppRoles.MunicipalManager)]
    [InlineData(AppRoles.Driver)]
    public async Task RejectAsync_NonOfficer_ThrowsForbidden(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var other = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id, WasteReportStatus.UnderReview);
        var svc = CreateService(db, um);

        var act = () => svc.RejectAsync(report.Id, new RejectWasteReportRequest { Reason = "Test reason." }, other.Id, role);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET HISTORY TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_Citizen_OwnReport_ReturnsChronologicalHistory()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var officer = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        // Simulate: Submitted → UnderReview → Verified
        var h1 = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(), WasteReportId = report.Id,
            FromStatus = null, ToStatus = WasteReportStatus.Submitted,
            ChangedByUserId = citizen.Id, ChangedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var h2 = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(), WasteReportId = report.Id,
            FromStatus = WasteReportStatus.Submitted, ToStatus = WasteReportStatus.UnderReview,
            ChangedByUserId = officer.Id, ChangedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var h3 = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(), WasteReportId = report.Id,
            FromStatus = WasteReportStatus.UnderReview, ToStatus = WasteReportStatus.Verified,
            ChangedByUserId = officer.Id, ChangedAt = DateTime.UtcNow
        };
        db.WasteReportStatusHistories.AddRange(h1, h2, h3);
        await db.SaveChangesAsync();

        var history = await svc.GetHistoryAsync(report.Id, citizen.Id, AppRoles.Citizen);

        history.Should().HaveCount(3);
        history[0].ToStatus.Should().Be(WasteReportStatus.Submitted);
        history[1].ToStatus.Should().Be(WasteReportStatus.UnderReview);
        history[2].ToStatus.Should().Be(WasteReportStatus.Verified);
    }

    [Fact]
    public async Task GetHistoryAsync_Citizen_OtherReport_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen1 = await SeedUserAsync(db, um);
        var citizen2 = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen1.Id);
        var svc = CreateService(db, um);

        var act = () => svc.GetHistoryAsync(report.Id, citizen2.Id, AppRoles.Citizen);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Theory]
    [InlineData(AppRoles.WasteOfficer)]
    [InlineData(AppRoles.MunicipalManager)]
    public async Task GetHistoryAsync_StaffRoles_CanViewAnyHistory(string role)
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var staff = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var history = await svc.GetHistoryAsync(report.Id, staff.Id, role);

        history.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistoryAsync_Driver_ThrowsForbidden()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var driver = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        var act = () => svc.GetHistoryAsync(report.Id, driver.Id, AppRoles.Driver);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetHistoryAsync_MissingReport_ThrowsNotFound()
    {
        var (db, um) = CreateContext();
        var officer = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var act = () => svc.GetHistoryAsync(Guid.NewGuid(), officer.Id, AppRoles.WasteOfficer);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ATOMICITY TESTS
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReportAndHistoryBothPersisted()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var svc = CreateService(db, um);

        var dto = await svc.CreateAsync(ValidCreateRequest(), citizen.Id, AppRoles.Citizen);

        var reportExists = await db.WasteReports.AnyAsync(r => r.Id == dto.Id);
        var historyExists = await db.WasteReportStatusHistories.AnyAsync(h => h.WasteReportId == dto.Id);

        reportExists.Should().BeTrue();
        historyExists.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAsync_ReportAndHistoryBothPersisted()
    {
        var (db, um) = CreateContext();
        var citizen = await SeedUserAsync(db, um);
        var report = await SeedReportAsync(db, citizen.Id);
        var svc = CreateService(db, um);

        await svc.CancelAsync(report.Id, citizen.Id, AppRoles.Citizen);

        var dbReport = await db.WasteReports.FindAsync(report.Id);
        var historyCount = await db.WasteReportStatusHistories
            .CountAsync(h => h.WasteReportId == report.Id);

        dbReport!.Status.Should().Be(WasteReportStatus.Cancelled);
        historyCount.Should().Be(1);
    }
}
