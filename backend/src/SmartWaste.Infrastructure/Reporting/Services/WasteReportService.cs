using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartWaste.Application.Common.Exceptions;
using SmartWaste.Application.Common.Models;
using SmartWaste.Application.Reporting.DTOs.Requests;
using SmartWaste.Application.Reporting.DTOs.Responses;
using SmartWaste.Application.Reporting.Interfaces;
using SmartWaste.Application.Reporting.Queries;
using SmartWaste.Domain.Common;
using SmartWaste.Domain.Entities;
using SmartWaste.Domain.Reporting.Entities;
using SmartWaste.Domain.Reporting.Enums;
using SmartWaste.Infrastructure.Persistence;
using SmartWaste.Infrastructure.Reporting.Storage;

namespace SmartWaste.Infrastructure.Reporting.Services;

/// <summary>
/// Component 1 — Waste Report business service.
/// Implements all deterministic business rules, state machine transitions,
/// role-based access control, and atomic persistence for waste reporting.
/// </summary>
public sealed class WasteReportService : IWasteReportService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IFileStorageService _fileStorageService;
    private readonly SupabaseStorageOptions _storageOptions;

    public WasteReportService(
        AppDbContext db,
        UserManager<AppUser> userManager,
        IFileStorageService fileStorageService,
        IOptions<SupabaseStorageOptions>? storageOptions = null)
    {
        _db = db;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _storageOptions = storageOptions?.Value ?? new SupabaseStorageOptions();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CREATE
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> CreateAsync(
        CreateWasteReportRequest request,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        // Role gate: only Citizens can create waste reports
        if (actorRole != AppRoles.Citizen)
        {
            throw new ForbiddenException("Only Citizens can create waste reports.");
        }

        var now = DateTime.UtcNow;

        var report = new WasteReport
        {
            Id = Guid.NewGuid(),
            CitizenId = actorUserId,            // sourced from JWT claims, never from request
            Description = request.Description,
            WasteType = request.WasteType!.Value,
            Latitude = request.Latitude!.Value,
            Longitude = request.Longitude!.Value,
            AddressText = request.AddressText,
            Status = WasteReportStatus.Submitted,
            Priority = null,                    // C1 invariant: priority never assigned here
            VerifiedByUserId = null,
            VerifiedAt = null,
            CreatedAt = now,
            UpdatedAt = null
        };

        var history = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(),
            WasteReportId = report.Id,
            FromStatus = null,                  // initial creation has no previous status
            ToStatus = WasteReportStatus.Submitted,
            ChangedByUserId = actorUserId,
            Notes = null,
            ChangedAt = now
        };

        // Atomic: both entities added and persisted in a single SaveChanges call
        _db.WasteReports.Add(report);
        _db.WasteReportStatusHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET BY ID
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> GetByIdAsync(
        Guid reportId,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        EnforceNotDriver(actorRole);

        var report = await _db.WasteReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        EnforceCitizenOwnership(actorRole, actorUserId, report.CitizenId);

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LIST / SEARCH / FILTER / PAGINATE
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<WasteReportSummaryDto>> GetListAsync(
        WasteReportListQuery query,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        EnforceNotDriver(actorRole);

        var page = query.Page > 0 ? query.Page : 1;
        var pageSize = query.PageSize is > 0 and <= 100 ? query.PageSize : 20;

        // 1. Role scope (applied first — Citizens cannot bypass with filters)
        IQueryable<WasteReport> baseQuery = _db.WasteReports.AsNoTracking();

        if (actorRole == AppRoles.Citizen)
        {
            baseQuery = baseQuery.Where(r => r.CitizenId == actorUserId);
        }
        // WasteOfficer and MunicipalManager see all reports — no scope restriction needed

        // 2. Status filter
        if (query.Status.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.Status == query.Status.Value);
        }

        // 3. WasteType filter
        if (query.WasteType.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.WasteType == query.WasteType.Value);
        }

        // 4. Case-insensitive search across Description and AddressText
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Description.ToLower().Contains(search) ||
                (r.AddressText != null && r.AddressText.ToLower().Contains(search)));
        }

        // 5. Date range filter on CreatedAt (UTC)
        if (query.FromDate.HasValue)
        {
            var from = NormalizeToUtc(query.FromDate.Value);
            baseQuery = baseQuery.Where(r => r.CreatedAt >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = NormalizeToUtc(query.ToDate.Value);
            baseQuery = baseQuery.Where(r => r.CreatedAt <= to);
        }

        // 6. Total count (before pagination)
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // 7. Sorting (safe explicit switch, no dynamic property access)
        var sortDir = query.SortDirection?.ToLowerInvariant() ?? "desc";
        baseQuery = (query.SortBy?.ToLowerInvariant() ?? "createdat") switch
        {
            "updatedat" => sortDir == "asc"
                ? baseQuery.OrderBy(r => r.UpdatedAt).ThenBy(r => r.Id)
                : baseQuery.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id),
            _ => sortDir == "asc"
                ? baseQuery.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
                : baseQuery.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
        };

        // 8. Skip / Take
        var pagedReports = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // 9. Resolve Citizen names for staff roles (batched to avoid N+1)
        var citizenIds = pagedReports.Select(r => r.CitizenId).Distinct().ToList();
        var citizenNames = await ResolveUserNamesAsync(citizenIds, cancellationToken);

        // 10. Project to summary DTOs
        var isStaff = actorRole is AppRoles.WasteOfficer or AppRoles.MunicipalManager;
        var items = pagedReports.Select(r => new WasteReportSummaryDto
        {
            Id = r.Id,
            Description = r.Description,
            WasteType = r.WasteType,
            Status = r.Status,
            Priority = r.Priority,
            AddressText = r.AddressText,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            // Staff roles see Citizen identity; Citizens get null to avoid redundancy
            CitizenId = isStaff ? r.CitizenId : null,
            CitizenName = isStaff ? citizenNames.GetValueOrDefault(r.CitizenId) : null,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return new PagedResult<WasteReportSummaryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE (PATCH)
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> UpdateAsync(
        Guid reportId,
        UpdateWasteReportRequest request,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        // Only Citizens can update evidence
        if (actorRole != AppRoles.Citizen)
        {
            throw new ForbiddenException("Only the Citizen owner can update a waste report.");
        }

        var report = await _db.WasteReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        // Ownership check
        if (report.CitizenId != actorUserId)
        {
            throw new ForbiddenException("You can only update your own waste reports.");
        }

        // Status guard — only Submitted reports may be edited
        if (report.Status != WasteReportStatus.Submitted)
        {
            throw new BusinessRuleConflictException(
                $"Waste report cannot be updated. Current status '{report.Status}' does not allow evidence edits. Only Submitted reports can be updated.");
        }

        // Apply partial PATCH — only non-null fields are changed
        if (request.Description is not null)
        {
            report.Description = request.Description;
        }

        if (request.WasteType.HasValue)
        {
            report.WasteType = request.WasteType.Value;
        }

        if (request.Latitude.HasValue)
        {
            report.Latitude = request.Latitude.Value;
        }

        if (request.Longitude.HasValue)
        {
            report.Longitude = request.Longitude.Value;
        }

        // AddressText frozen contract:
        //   null → no change
        //   ""   → clear to null
        //   non-empty → set supplied value
        if (request.AddressText is not null)
        {
            report.AddressText = request.AddressText.Length == 0 ? null : request.AddressText;
        }

        report.UpdatedAt = DateTime.UtcNow;

        // No status transition → no history entry created
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CANCEL
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> CancelAsync(
        Guid reportId,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != AppRoles.Citizen)
        {
            throw new ForbiddenException("Only the Citizen owner can cancel a waste report.");
        }

        var report = await _db.WasteReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        if (report.CitizenId != actorUserId)
        {
            throw new ForbiddenException("You can only cancel your own waste reports.");
        }

        if (report.Status != WasteReportStatus.Submitted)
        {
            throw new BusinessRuleConflictException(
                $"Waste report cannot be cancelled. Current status '{report.Status}' does not allow cancellation. Only Submitted reports can be cancelled.");
        }

        var now = DateTime.UtcNow;

        report.Status = WasteReportStatus.Cancelled;
        report.UpdatedAt = now;

        var history = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(),
            WasteReportId = report.Id,
            FromStatus = WasteReportStatus.Submitted,
            ToStatus = WasteReportStatus.Cancelled,
            ChangedByUserId = actorUserId,
            Notes = "Cancelled by citizen",
            ChangedAt = now
        };

        _db.WasteReportStatusHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);     // atomic

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // START REVIEW
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> StartReviewAsync(
        Guid reportId,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != AppRoles.WasteOfficer)
        {
            throw new ForbiddenException("Only a Waste Officer can initiate report review.");
        }

        var report = await _db.WasteReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        if (report.Status != WasteReportStatus.Submitted)
        {
            throw new BusinessRuleConflictException(
                $"Cannot start review. Report is currently '{report.Status}'. Only Submitted reports can be moved to UnderReview.");
        }

        var now = DateTime.UtcNow;

        report.Status = WasteReportStatus.UnderReview;
        report.UpdatedAt = now;

        var history = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(),
            WasteReportId = report.Id,
            FromStatus = WasteReportStatus.Submitted,
            ToStatus = WasteReportStatus.UnderReview,
            ChangedByUserId = actorUserId,
            Notes = null,
            ChangedAt = now
        };

        _db.WasteReportStatusHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);     // atomic

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VERIFY
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> VerifyAsync(
        Guid reportId,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != AppRoles.WasteOfficer)
        {
            throw new ForbiddenException("Only a Waste Officer can verify a waste report.");
        }

        var report = await _db.WasteReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        if (report.Status != WasteReportStatus.UnderReview)
        {
            throw new BusinessRuleConflictException(
                $"Cannot verify report. Current status '{report.Status}'. Only UnderReview reports can be verified.");
        }

        var now = DateTime.UtcNow;

        report.Status = WasteReportStatus.Verified;
        report.VerifiedByUserId = actorUserId;
        report.VerifiedAt = now;
        report.UpdatedAt = now;
        // Priority MUST remain null — C1 invariant, never assigned during Component 1 verification

        var history = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(),
            WasteReportId = report.Id,
            FromStatus = WasteReportStatus.UnderReview,
            ToStatus = WasteReportStatus.Verified,
            ChangedByUserId = actorUserId,
            Notes = null,
            ChangedAt = now
        };

        _db.WasteReportStatusHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);     // atomic

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // REJECT
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<WasteReportDetailDto> RejectAsync(
        Guid reportId,
        RejectWasteReportRequest request,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        if (actorRole != AppRoles.WasteOfficer)
        {
            throw new ForbiddenException("Only a Waste Officer can reject a waste report.");
        }

        var report = await _db.WasteReports
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        if (report.Status != WasteReportStatus.UnderReview)
        {
            throw new BusinessRuleConflictException(
                $"Cannot reject report. Current status '{report.Status}'. Only UnderReview reports can be rejected.");
        }

        var now = DateTime.UtcNow;

        report.Status = WasteReportStatus.Rejected;
        report.UpdatedAt = now;
        // VerifiedByUserId and VerifiedAt are NOT set on rejection (only set on Verify)

        var history = new WasteReportStatusHistory
        {
            Id = Guid.NewGuid(),
            WasteReportId = report.Id,
            FromStatus = WasteReportStatus.UnderReview,
            ToStatus = WasteReportStatus.Rejected,
            ChangedByUserId = actorUserId,
            Notes = request.Reason,             // rejection reason stored in history notes only
            ChangedAt = now
        };

        _db.WasteReportStatusHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);     // atomic

        return await BuildDetailDtoAsync(report, actorUserId, actorRole, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET HISTORY
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<WasteReportStatusHistoryDto>> GetHistoryAsync(
        Guid reportId,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        EnforceNotDriver(actorRole);

        // Verify the report exists and enforce ownership for Citizens
        var report = await _db.WasteReports
            .AsNoTracking()
            .Select(r => new { r.Id, r.CitizenId })
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            throw new NotFoundException($"Waste report '{reportId}' was not found.");
        }

        EnforceCitizenOwnership(actorRole, actorUserId, report.CitizenId);

        var historyRows = await _db.WasteReportStatusHistories
            .AsNoTracking()
            .Where(h => h.WasteReportId == reportId)
            .OrderBy(h => h.ChangedAt)      // chronological ascending for UI timeline
            .ToListAsync(cancellationToken);

        // Resolve display names for changers (batch)
        var changerIds = historyRows
            .Where(h => h.ChangedByUserId.HasValue)
            .Select(h => h.ChangedByUserId!.Value)
            .Distinct()
            .ToList();

        var changerNames = await ResolveUserNamesAsync(changerIds, cancellationToken);

        return historyRows.Select(h => new WasteReportStatusHistoryDto
        {
            Id = h.Id,
            WasteReportId = h.WasteReportId,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByUserId = h.ChangedByUserId,
            ChangedByUserName = h.ChangedByUserId.HasValue
                ? changerNames.GetValueOrDefault(h.ChangedByUserId.Value)
                : null,
            Notes = h.Notes,
            ChangedAt = h.ChangedAt
        }).ToList().AsReadOnly();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a full WasteReportDetailDto for a given report entity.
    /// Resolves Citizen and VerifiedBy display names. Returns empty attachment list
    /// until Step 9A.7 implements secure FileUrl generation via IFileStorageService.
    /// </summary>
    private async Task<WasteReportDetailDto> BuildDetailDtoAsync(
        WasteReport report,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken)
    {
        // Resolve Citizen display name
        var citizenName = await ResolveUserNameAsync(report.CitizenId, cancellationToken);

        // Resolve VerifiedBy display name if available
        string? verifiedByUserName = null;
        if (report.VerifiedByUserId.HasValue)
        {
            verifiedByUserName = await ResolveUserNameAsync(report.VerifiedByUserId.Value, cancellationToken);
        }

        // Resolve persisted photographic attachments and generate short-lived signed read URLs
        var attachments = await _db.ReportAttachments
            .AsNoTracking()
            .Where(a => a.WasteReportId == report.Id)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var expiry = TimeSpan.FromSeconds(_storageOptions.SignedUrlExpirySeconds > 0 ? _storageOptions.SignedUrlExpirySeconds : 900);
        var attachmentDtos = new List<ReportAttachmentDto>(attachments.Count);

        foreach (var attachment in attachments)
        {
            var signedUrl = await _fileStorageService.GetReadUrlAsync(attachment.StorageKey, expiry, cancellationToken);
            attachmentDtos.Add(new ReportAttachmentDto
            {
                Id = attachment.Id,
                WasteReportId = attachment.WasteReportId,
                FileType = attachment.FileType,
                CreatedAt = attachment.CreatedAt,
                FileUrl = signedUrl
            });
        }

        return new WasteReportDetailDto
        {
            Id = report.Id,
            CitizenId = report.CitizenId,
            CitizenName = citizenName ?? string.Empty,
            Description = report.Description,
            WasteType = report.WasteType,
            Latitude = report.Latitude,
            Longitude = report.Longitude,
            AddressText = report.AddressText,
            Status = report.Status,
            Priority = report.Priority,
            VerifiedByUserId = report.VerifiedByUserId,
            VerifiedByUserName = verifiedByUserName,
            VerifiedAt = report.VerifiedAt,
            Attachments = attachmentDtos,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    /// <summary>Resolves a single user's FullName by Id, or null if not found.</summary>
    private async Task<string?> ResolveUserNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves a batch of user FullNames keyed by UserId.
    /// Uses a single DB query to avoid N+1 when populating list/history DTOs.
    /// </summary>
    private async Task<Dictionary<Guid, string?>> ResolveUserNamesAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        return await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => (string?)u.FullName, cancellationToken);
    }

    /// <summary>
    /// Throws ForbiddenException for Driver actors, who have no Component 1 access.
    /// </summary>
    private static void EnforceNotDriver(string actorRole)
    {
        if (actorRole == AppRoles.Driver)
        {
            throw new ForbiddenException("Drivers do not have access to waste report operations.");
        }
    }

    /// <summary>
    /// For Citizen actors: throws ForbiddenException if the report does not belong to them.
    /// Non-Citizen actors (WasteOfficer, MunicipalManager) have unrestricted access.
    /// </summary>
    private static void EnforceCitizenOwnership(string actorRole, Guid actorUserId, Guid reportCitizenId)
    {
        if (actorRole == AppRoles.Citizen && reportCitizenId != actorUserId)
        {
            throw new ForbiddenException("You can only access your own waste reports.");
        }
    }

    /// <summary>
    /// Normalizes a DateTime to UTC, treating Unspecified kind as UTC.
    /// Prevents accidental local-timezone interpretation for date-range filters.
    /// </summary>
    private static DateTime NormalizeToUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)     // Unspecified → treat as UTC
    };

    // ──────────────────────────────────────────────────────────────────────────
    // AI INTERNAL TOOL: GET VERIFIED WASTE REPORTS
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<PagedResult<VerifiedWasteReportToolItemDto>> GetVerifiedReportsForAiAsync(
        GetVerifiedWasteReportsForAiQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page > 0 ? query.Page : 1;
        var pageSize = query.PageSize is > 0 and <= 50 ? query.PageSize : 20;

        // 1. Authoritative eligibility: strictly Verified status only, AsNoTracking for read-only
        var baseQuery = _db.WasteReports
            .AsNoTracking()
            .Where(r => r.Status == WasteReportStatus.Verified);

        // 2. Total count
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // 3. Deterministic order: CreatedAt DESC, Id DESC (stable tie-breaker)
        var pagedItems = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new VerifiedWasteReportToolItemDto
            {
                Id = r.Id,
                Description = r.Description,
                WasteType = r.WasteType,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                AddressText = r.AddressText,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                VerifiedAt = r.VerifiedAt,
                AttachmentCount = r.Attachments.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<VerifiedWasteReportToolItemDto>
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
