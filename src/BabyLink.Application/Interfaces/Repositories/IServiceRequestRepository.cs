// ממשק גישה לנתוני בקשות (המימוש ב-Infrastructure).
// SaveChangesAsync זורק ConcurrencyConflictException (לא חריגה של EF!) כשיש התנגשות Version.
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Interfaces.Repositories;

public interface IServiceRequestRepository
{
    // סינון, מיון ו-Pagination בתוך השאילתה. currentUserId משמש לסינון "mine",
    // ו-isBabysitter להסתרת בקשות שנתפסו ע"י שמרטפית אחרת.
    Task<PagedResult<ServiceRequest>> GetPagedAsync(ServiceRequestQueryParameters query, int currentUserId, bool isBabysitter, CancellationToken cancellationToken);

    // קריאה בלבד (AsNoTracking), כולל ההורה והשמרטפית.
    Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // עם Tracking - לפני עדכון/מחיקה/שיבוץ, כדי ש-EF יבדוק את ה-Version.
    Task<ServiceRequest?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    // האם לשמרטפית יש שיבוץ אחר שחופף לטווח הזמן (בלי הבקשה excludeRequestId).
    Task<bool> HasOverlappingAssignmentAsync(int babysitterId, DateTime startUtc, DateTime endUtc, int excludeRequestId, CancellationToken cancellationToken);

    Task AddAsync(ServiceRequest request, CancellationToken cancellationToken);
    void Remove(ServiceRequest request);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
