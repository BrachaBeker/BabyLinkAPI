// ממשק שירות הבקשות - כל הלוגיקה העסקית. המשתמש (מזהה ותפקיד) תמיד מגיע מה-JWT (דרך ה-Controller).
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;

namespace BabyLink.Application.Interfaces.Services;

public interface IServiceRequestService
{
    // הורה מקבל רק את הבקשות שהוא פרסם; שמרטפית - את כל הבקשות לפי הסינון.
    Task<PagedResult<ServiceRequestListItemDto>> GetPagedAsync(ServiceRequestQueryParameters query, CurrentUser currentUser, CancellationToken cancellationToken);

    // הורה רואה רק בקשה שהוא פרסם (אחרת 403).
    // זהות השמרטפית שתפסה (מזהה, שם, טלפון) מוצגת רק להורה שפרסם ולשמרטפית עצמה. מועד התפיסה גלוי לכולם.
    Task<ServiceRequestDetailsDto> GetByIdAsync(int id, CurrentUser currentUser, CancellationToken cancellationToken);

    Task<ServiceRequestDetailsDto> CreateAsync(CreateServiceRequestDto dto, int parentId, CancellationToken cancellationToken);
    Task UpdateAsync(int id, UpdateServiceRequestDto dto, int parentId, CancellationToken cancellationToken);

    // ביטול בקשה שכבר נתפסה יוצר הודעה לשמרטפית.
    Task DeleteAsync(int id, int parentId, CancellationToken cancellationToken);

    // הפעולה המתחרה - תפיסת בקשה. ההורה מקבל הודעה.
    Task<ServiceRequestDetailsDto> AssignAsync(int requestId, int babysitterId, CancellationToken cancellationToken);

    // ביטול שיבוץ ע"י השמרטפית שתפסה (לפני המועד). הבקשה חוזרת להמתין, וההורה מקבל הודעה.
    Task UnassignAsync(int requestId, int babysitterId, CancellationToken cancellationToken);
}
