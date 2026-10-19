// מימוש IServiceRequestService - הלב של המערכת. כל הוולידציה העסקית כאן:
// סיום אחרי התחלה, התחלה בעתיד, עד 24 שעות; רק ההורה שיצר עורך/מוחק (403); בקשה משובצת לא נערכת (409);
// רק השמרטפית שתפסה מבטלת את השיבוץ, ורק עד 24 שעות לפני המועד. כל שינוי מצב מול הצד השני יוצר לו הודעה,
// באותה SaveChanges של השינוי עצמו.
// מקרי הקצה של התחרות מתועדים ב-README. מגבלה ידועה: שתי בקשות שונות וחופפות שאותה שמרטפית תופסת
// באותה מילישנייה - שתי שורות שונות, ולכן ה-Version לא מזהה את זה (פתרון אפשרי: Concurrency Token על User).
using AutoMapper;
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Services;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BabyLink.Application.Services;

public class ServiceRequestService(
    IServiceRequestRepository repository,
    ITagRepository tagRepository,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IMapper mapper,
    ILogger<ServiceRequestService> logger) : IServiceRequestService
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    // שמרטפית יכולה לבטל את השיבוץ שלה רק עד 24 שעות לפני המועד. פחות מזה - עליה להשיג מחליפה.
    // ציבורי, כי גם ה-DTO מחשב לפיו את CanCancelAssignment (בשביל הכפתור בלקוח).
    public static readonly TimeSpan AssignmentCancellationNotice = TimeSpan.FromHours(24);

    public async Task<PagedResult<ServiceRequestListItemDto>> GetPagedAsync(ServiceRequestQueryParameters query, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        query.FromUtc = ToUtc(query.FromUtc);
        query.ToUtc = ToUtc(query.ToUtc);

        // הורה רואה רק את הבקשות שהוא פרסם - בלי קשר למה שנשלח ב-Query.
        if (currentUser.IsParent)
        {
            query.Mine = true;
        }

        var page = await repository.GetPagedAsync(query, currentUser.Id, cancellationToken);

        return new PagedResult<ServiceRequestListItemDto>
        {
            Items = mapper.Map<List<ServiceRequestListItemDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ServiceRequestDetailsDto> GetByIdAsync(int id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(id, cancellationToken) ?? throw NotFound(id);

        if (currentUser.IsParent && request.ParentId != currentUser.Id)
        {
            throw new ForbiddenException("אפשר לצפות רק בבקשות שפרסמת.");
        }

        var dto = mapper.Map<ServiceRequestDetailsDto>(request);

        // פרטיות: מי תפסה את הבקשה - רק ההורה שפרסם והשמרטפית שתפסה רואים.
        // לשאר נשארים רק פרטים לא מזהים: IsAssigned ומועד התפיסה (AssignedAtUtc).
        if (currentUser.Id != request.ParentId && currentUser.Id != request.AssignedBabysitterId)
        {
            dto.AssignedBabysitterId = null;
            dto.AssignedBabysitterFullName = null;
            dto.AssignedBabysitterPhone = null;
        }

        return dto;
    }

    public async Task<ServiceRequestDetailsDto> CreateAsync(CreateServiceRequestDto dto, int parentId, CancellationToken cancellationToken)
    {
        dto.StartTimeUtc = ToUtc(dto.StartTimeUtc);
        dto.EndTimeUtc = ToUtc(dto.EndTimeUtc);
        ValidateTimes(dto.StartTimeUtc, dto.EndTimeUtc);

        var request = mapper.Map<ServiceRequest>(dto);
        request.ParentId = parentId;
        request.CreatedAtUtc = DateTime.UtcNow;
        request.Tags = [.. await GetTagsAsync(dto.TagIds, cancellationToken)];

        await repository.AddAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} created by parent {ParentId}", request.Id, parentId);

        // טעינה מחדש - כדי שהתשובה תכלול את פרטי ההורה.
        return await GetByIdAsync(request.Id, new CurrentUser(parentId, UserRole.Parent), cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateServiceRequestDto dto, int parentId, CancellationToken cancellationToken)
    {
        var request = await GetOwnedForUpdateAsync(id, parentId, cancellationToken);

        if (request.AssignedBabysitterId is not null)
        {
            throw new ConflictException("לא ניתן לערוך בקשה שכבר נתפסה. אפשר לבטל אותה ולפרסם בקשה חדשה.");
        }

        dto.StartTimeUtc = ToUtc(dto.StartTimeUtc);
        dto.EndTimeUtc = ToUtc(dto.EndTimeUtc);
        ValidateTimes(dto.StartTimeUtc, dto.EndTimeUtc);

        var tags = await GetTagsAsync(dto.TagIds, cancellationToken);

        mapper.Map(dto, request);
        request.Tags.Clear();
        foreach (var tag in tags)
        {
            request.Tags.Add(tag);
        }

        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} updated by parent {ParentId}", id, parentId);
    }

    public async Task DeleteAsync(int id, int parentId, CancellationToken cancellationToken)
    {
        // מחיקה מותרת גם לבקשה משובצת (כך מבטלים - ההורה לא מחויב לשמרטפית שתפסה). ה-Version נבדק גם ב-DELETE.
        var request = await GetOwnedForUpdateAsync(id, parentId, cancellationToken);

        // ביטול בקשה שכבר נתפסה - השמרטפית תקבל הודעה בכניסה הבאה שלה.
        // ההודעה נשמרת באותה SaveChanges של המחיקה, כך שאין מצב של מחיקה בלי הודעה (או להפך).
        if (request.AssignedBabysitterId is { } babysitterId)
        {
            notificationRepository.Add(CreateNotification(
                request, babysitterId, NotificationType.RequestCancelled, request.Parent.FullName, linkToRequest: false));
        }

        repository.Remove(request);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} deleted by parent {ParentId}", id, parentId);
    }

    // תפיסת בקשה - "הראשונה שתופסת זוכה". הבדיקות העסקיות מסננות את רוב המקרים,
    // וה-Version (Optimistic Concurrency) תופס את המקרה ששתי שמרטפיות עברו את הבדיקות באותו רגע.
    public async Task<ServiceRequestDetailsDto> AssignAsync(int requestId, int babysitterId, CancellationToken cancellationToken)
    {
        // 1. טעינה עם Tracking - כדי ש-EF יכניס את ה-Version הנוכחי ל-WHERE של ה-UPDATE.
        var request = await repository.GetForUpdateAsync(requestId, cancellationToken) ?? throw NotFound(requestId);

        // 2. כבר שלה - הצלחה (אידמפוטנטי: לחיצה כפולה לא נכשלת).
        if (request.AssignedBabysitterId == babysitterId)
        {
            return mapper.Map<ServiceRequestDetailsDto>(request);
        }

        // 3. תפוסה ע"י שמרטפית אחרת.
        if (request.AssignedBabysitterId is not null)
        {
            throw new ConflictException("הבקשה כבר נתפסה על ידי שמרטפית אחרת.");
        }

        // 4. מועד ההתחלה עבר.
        if (request.StartTimeUtc <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("לא ניתן לתפוס בקשה שמועד ההתחלה שלה עבר.");
        }

        // 5. לשמרטפית כבר יש שיבוץ שחופף בזמן.
        if (await repository.HasOverlappingAssignmentAsync(babysitterId, request.StartTimeUtc, request.EndTimeUtc, request.Id, cancellationToken))
        {
            throw new ConflictException("יש לך כבר שיבוץ אחר שחופף בזמן לבקשה הזו.");
        }

        // 6. שיבוץ, והודעה להורה ("נמצאה שמרטפית!") - באותה SaveChanges, כך שאין שיבוץ בלי הודעה (או להפך).
        //    אם מישהו שמר בינתיים - ה-Repository זורק ConcurrencyConflictException (409), ושום דבר לא נשמר.
        var babysitter = await userRepository.GetByIdAsync(babysitterId, cancellationToken);
        request.AssignedBabysitterId = babysitterId;
        request.AssignedAtUtc = DateTime.UtcNow;
        notificationRepository.Add(CreateNotification(
            request, request.ParentId, NotificationType.RequestAssigned, babysitter?.FullName ?? "שמרטפית", linkToRequest: true));
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} assigned to babysitter {BabysitterId}", requestId, babysitterId);

        // טעינה מחדש - כדי שהתשובה תכלול את פרטי השמרטפית.
        return await GetByIdAsync(requestId, new CurrentUser(babysitterId, UserRole.Babysitter), cancellationToken);
    }

    // ביטול שיבוץ ע"י השמרטפית שתפסה, עד 24 שעות לפני המועד - הבקשה חוזרת להמתין לשמרטפית, וההורה מקבל הודעה.
    // גם כאן ה-Version מגן: ביטול שקורה בדיוק כשמישהו אחר משנה את הבקשה (תפיסה, עריכה, מחיקה) - מי שמאחר מקבל 409.
    public async Task UnassignAsync(int requestId, int babysitterId, CancellationToken cancellationToken)
    {
        var request = await repository.GetForUpdateAsync(requestId, cancellationToken) ?? throw NotFound(requestId);

        if (request.AssignedBabysitterId is null)
        {
            throw new ConflictException("הבקשה כבר לא משובצת.");
        }

        if (request.AssignedBabysitterId != babysitterId)
        {
            throw new ForbiddenException("אפשר לבטל רק שיבוץ שלך.");
        }

        if (request.StartTimeUtc <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("לא ניתן לבטל שיבוץ אחרי שמועד ההתחלה עבר.");
        }

        if (request.StartTimeUtc - DateTime.UtcNow < AssignmentCancellationNotice)
        {
            throw new BusinessRuleException("אין אפשרות לבטל פחות מ-24 שעות לפני המועד. אם אינך יכולה להגיע, עליך להשיג מחליפה.");
        }

        var babysitterName = request.AssignedBabysitter?.FullName ?? "השמרטפית";
        request.AssignedBabysitterId = null;
        request.AssignedBabysitter = null;
        request.AssignedAtUtc = null;
        notificationRepository.Add(CreateNotification(
            request, request.ParentId, NotificationType.AssignmentCancelled, babysitterName, linkToRequest: true));
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Babysitter {BabysitterId} cancelled the assignment of service request {RequestId}", babysitterId, requestId);
    }

    // הודעה למשתמש עם פרטי הבקשה. linkToRequest=false כשהבקשה נמחקת (אין לאן לקשר).
    private static Notification CreateNotification(ServiceRequest request, int recipientId, NotificationType type,
        string actorFullName, bool linkToRequest) => new()
    {
        UserId = recipientId,
        Type = type,
        RequestId = linkToRequest ? request.Id : null,
        RequestStartTimeUtc = request.StartTimeUtc,
        RequestEndTimeUtc = request.EndTimeUtc,
        City = request.City,
        Street = request.Street,
        HouseNumber = request.HouseNumber,
        ActorFullName = actorFullName,
        CreatedAtUtc = DateTime.UtcNow
    };

    private async Task<ServiceRequest> GetOwnedForUpdateAsync(int id, int parentId, CancellationToken cancellationToken)
    {
        var request = await repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);

        if (request.ParentId != parentId)
        {
            throw new ForbiddenException("רק ההורה שפרסם את הבקשה יכול לשנות אותה.");
        }

        return request;
    }

    private async Task<IReadOnlyList<Tag>> GetTagsAsync(List<int> tagIds, CancellationToken cancellationToken)
    {
        var distinctIds = tagIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return [];
        }

        var tags = await tagRepository.GetByIdsAsync(distinctIds, cancellationToken);
        if (tags.Count != distinctIds.Count)
        {
            var missing = distinctIds.Except(tags.Select(t => t.Id));
            throw new BusinessRuleException($"תגיות לא קיימות: {string.Join(", ", missing)}.");
        }

        return tags;
    }

    private static void ValidateTimes(DateTime startUtc, DateTime endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new BusinessRuleException("שעת הסיום חייבת להיות אחרי שעת ההתחלה.");
        }

        if (startUtc <= DateTime.UtcNow)
        {
            throw new BusinessRuleException("מועד ההתחלה חייב להיות בעתיד.");
        }

        if (endUtc - startUtc > MaxDuration)
        {
            throw new BusinessRuleException("משך השמרטפות לא יכול לעלות על 24 שעות.");
        }
    }

    // Npgsql דורש DateTime מסוג UTC. ערך בלי אזור זמן נחשב כ-UTC; ערך עם אזור זמן מומר ל-UTC.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? ToUtc(DateTime? value) => value is { } v ? ToUtc(v) : null;

    private static NotFoundException NotFound(int id) => new($"בקשה {id} לא נמצאה.");
}
