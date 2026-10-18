// מימוש IServiceRequestRepository: סינון, מיון ו-Pagination בתוך השאילתה (AsNoTracking + Skip/Take ב-DB).
// SaveChangesAsync מתרגם DbUpdateConcurrencyException (ורק אותה) ל-ConcurrencyConflictException.
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Domain.Entities;
using BabyLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Repositories;

public class ServiceRequestRepository(AppDbContext db) : IServiceRequestRepository
{
    private const string LikeEscape = "\\";

    public async Task<PagedResult<ServiceRequest>> GetPagedAsync(ServiceRequestQueryParameters query, int currentUserId, bool isBabysitter, CancellationToken cancellationToken)
    {
        IQueryable<ServiceRequest> requests = db.ServiceRequests.AsNoTracking();
        var now = DateTime.UtcNow;

        // שמרטפית רואה רק בקשות פנויות או בקשות שהיא עצמה תפסה - לא בקשות של שמרטפיות אחרות.
        if (isBabysitter)
        {
            requests = requests.Where(r => r.AssignedBabysitterId == null || r.AssignedBabysitterId == currentUserId);
        }

        // ברירת מחדל: בלי בקשות שמועד ההתחלה שלהן עבר.
        if (!query.IncludePast)
        {
            requests = requests.Where(r => r.StartTimeUtc > now);
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            // התאמה מלאה בלי תלות ברישיות.
            var city = EscapeLike(query.City.Trim());
            requests = requests.Where(r => EF.Functions.ILike(r.City, city, LikeEscape));
        }

        if (!string.IsNullOrWhiteSpace(query.Street))
        {
            // חיפוש חלקי: "הרצל" ימצא גם "שדרות הרצל".
            var street = $"%{EscapeLike(query.Street.Trim())}%";
            requests = requests.Where(r => EF.Functions.ILike(r.Street, street, LikeEscape));
        }

        if (query.FromUtc is { } fromUtc)
        {
            requests = requests.Where(r => r.StartTimeUtc >= fromUtc);
        }

        if (query.ToUtc is { } toUtc)
        {
            requests = requests.Where(r => r.StartTimeUtc <= toUtc);
        }

        if (query.TagId is { } tagId)
        {
            requests = requests.Where(r => r.Tags.Any(t => t.Id == tagId));
        }

        if (query.MinHourlyRate is { } minHourlyRate)
        {
            requests = requests.Where(r => r.HourlyRate >= minHourlyRate);
        }

        if (query.OnlyAvailable)
        {
            requests = requests.Where(r => r.AssignedBabysitterId == null && r.StartTimeUtc > now);
        }

        if (query.Mine)
        {
            requests = requests.Where(r => r.ParentId == currentUserId || r.AssignedBabysitterId == currentUserId);
        }

        // ThenBy(Id) - סדר יציב, כדי שאותה בקשה לא תופיע בשני עמודים.
        requests = (query.SortBy, query.Descending) switch
        {
            ("hourlyRate", false) => requests.OrderBy(r => r.HourlyRate).ThenBy(r => r.Id),
            ("hourlyRate", true) => requests.OrderByDescending(r => r.HourlyRate).ThenBy(r => r.Id),
            (_, false) => requests.OrderBy(r => r.StartTimeUtc).ThenBy(r => r.Id),
            (_, true) => requests.OrderByDescending(r => r.StartTimeUtc).ThenBy(r => r.Id)
        };

        var totalCount = await requests.CountAsync(cancellationToken);
        // Include של התגיות באותה שאילתה - בלי N+1.
        var items = await requests
            .Include(r => r.Tags)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceRequest>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.ServiceRequests
            .AsNoTracking()
            .Include(r => r.Parent)
            .Include(r => r.AssignedBabysitter)
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<ServiceRequest?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        db.ServiceRequests
            .Include(r => r.Parent)
            .Include(r => r.AssignedBabysitter)
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> HasOverlappingAssignmentAsync(int babysitterId, DateTime startUtc, DateTime endUtc, int excludeRequestId, CancellationToken cancellationToken) =>
        db.ServiceRequests.AnyAsync(r =>
            r.AssignedBabysitterId == babysitterId &&
            r.Id != excludeRequestId &&
            r.StartTimeUtc < endUtc &&
            startUtc < r.EndTimeUtc,
            cancellationToken);

    public async Task AddAsync(ServiceRequest request, CancellationToken cancellationToken) =>
        await db.ServiceRequests.AddAsync(request, cancellationToken);

    public void Remove(ServiceRequest request) => db.ServiceRequests.Remove(request);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }

    // תווים מיוחדים של LIKE (% _ \) שהמשתמש הקליד נחשבים לטקסט רגיל.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
