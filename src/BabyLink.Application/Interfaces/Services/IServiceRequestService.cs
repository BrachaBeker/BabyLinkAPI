// ממשק שירות הבקשות - כל הלוגיקה העסקית. ה-UserId תמיד מגיע מה-JWT (דרך ה-Controller).
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;

namespace BabyLink.Application.Interfaces.Services;

public interface IServiceRequestService
{
    Task<PagedResult<ServiceRequestListItemDto>> GetPagedAsync(ServiceRequestQueryParameters query, int currentUserId, CancellationToken cancellationToken);

    // זהות השמרטפית שתפסה (מזהה, שם, טלפון) מוצגת רק להורה שפרסם ולשמרטפית עצמה. מועד התפיסה גלוי לכולם.
    Task<ServiceRequestDetailsDto> GetByIdAsync(int id, int currentUserId, CancellationToken cancellationToken);
    Task<ServiceRequestDetailsDto> CreateAsync(CreateServiceRequestDto dto, int parentId, CancellationToken cancellationToken);
    Task UpdateAsync(int id, UpdateServiceRequestDto dto, int parentId, CancellationToken cancellationToken);
    Task DeleteAsync(int id, int parentId, CancellationToken cancellationToken);

    // הפעולה המתחרה - תפיסת בקשה.
    Task<ServiceRequestDetailsDto> AssignAsync(int requestId, int babysitterId, CancellationToken cancellationToken);
}
