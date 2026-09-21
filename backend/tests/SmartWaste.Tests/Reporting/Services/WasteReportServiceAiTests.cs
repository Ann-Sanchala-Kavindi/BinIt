using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartWaste.Application.Reporting.DTOs.Responses;
using SmartWaste.Application.Reporting.Queries;
using SmartWaste.Domain.Entities;
using SmartWaste.Domain.Reporting.Entities;
using SmartWaste.Domain.Reporting.Enums;
using SmartWaste.Infrastructure.Persistence;
using SmartWaste.Infrastructure.Reporting.Services;
using SmartWaste.Tests.Reporting.Fakes;
using Xunit;

namespace SmartWaste.Tests.Reporting.Services;

public class WasteReportServiceAiTests
{
    private static (AppDbContext Db, UserManager<AppUser> UserManager) CreateContext()
    {
        var dbName = $"SmartWaste_Reporting_Ai_{Guid.NewGuid():N}";
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
            loggerMock.Object);

        return (db, userManager);
    }

    private static WasteReportService CreateService(AppDbContext db, UserManager<AppUser> userManager)
    {
        var fakeStorage = new FakeFileStorageService();
        return new WasteReportService(db, userManager, fakeStorage);
    }

    [Fact]
    public async Task GetVerifiedReportsForAiAsync_ReturnsOnlyVerifiedReports()
    {
        var (db, userManager) = CreateContext();
        var service = CreateService(db, userManager);
        var citizenId = Guid.NewGuid();

        db.WasteReports.AddRange(
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "Submitted", Status = WasteReportStatus.Submitted },
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "UnderReview", Status = WasteReportStatus.UnderReview },
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "Verified 1", Status = WasteReportStatus.Verified },
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "Verified 2", Status = WasteReportStatus.Verified },
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "Rejected", Status = WasteReportStatus.Rejected },
            new WasteReport { Id = Guid.NewGuid(), CitizenId = citizenId, Description = "Cancelled", Status = WasteReportStatus.Cancelled }
        );
        await db.SaveChangesAsync();

        var query = new GetVerifiedWasteReportsForAiQuery { Page = 1, PageSize = 10 };
        var result = await service.GetVerifiedReportsForAiAsync(query);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(r => r.Status == WasteReportStatus.Verified);
        result.Items.Select(r => r.Description).Should().Contain(new[] { "Verified 1", "Verified 2" });
    }

    [Fact]
    public async Task GetVerifiedReportsForAiAsync_CalculatesAttachmentCountCorrectly()
    {
        var (db, userManager) = CreateContext();
        var service = CreateService(db, userManager);
        var citizenId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        var report = new WasteReport
        {
            Id = reportId,
            CitizenId = citizenId,
            Description = "Report with 2 attachments",
            Status = WasteReportStatus.Verified,
            Attachments = new List<ReportAttachment>
            {
                new() { Id = Guid.NewGuid(), StorageKey = "key1", FileType = "image/jpeg" },
                new() { Id = Guid.NewGuid(), StorageKey = "key2", FileType = "image/jpeg" }
            }
        };
        db.WasteReports.Add(report);
        await db.SaveChangesAsync();

        var query = new GetVerifiedWasteReportsForAiQuery { Page = 1, PageSize = 10 };
        var result = await service.GetVerifiedReportsForAiAsync(query);

        result.Items.Should().ContainSingle();
        result.Items[0].AttachmentCount.Should().Be(2);
    }
}
