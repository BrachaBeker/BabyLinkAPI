// מימוש IServiceRequestService - הלב של המערכת. כל הוולידציה העסקית כאן:
// סיום אחרי התחלה, התחלה בעתיד, עד 24 שעות; רק ההורה שיצר עורך/מוחק (403); בקשה משובצת לא נערכת (409).
// מקרי הקצה של התחרות מתועדים ב-README. מגבלה ידועה: שתי בקשות שונות וחופפות שאותה שמרטפית תופסת
// באותה מילישנייה - שתי שורות שונות, ולכן ה-Version לא מזהה את זה (פתרון אפשרי: Concurrency Token על User).
using AutoMapper;
using BabyLink.Application.DTOs.Common;
using BabyLink.Application.DTOs.ServiceRequests;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Services;
using BabyLink.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BabyLink.Application.Services;

public class ServiceRequestService(
    IServiceRequestRepository repository,
    ITagRepository tagRepository,
    IMapper mapper,
    ILogger<ServiceRequestService> logger) : IServiceRequestService
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    public async Task<PagedResult<ServiceRequestListItemDto>> GetPagedAsync(ServiceRequestQueryParameters query, int currentUserId, bool isBabysitter, CancellationToken cancellationToken)
    {
        query.FromUtc = ToUtc(query.FromUtc);
        query.ToUtc = ToUtc(query.ToUtc);

        var page = await repository.GetPagedAsync(query, currentUserId, isBabysitter, cancellationToken);

        return new PagedResult<ServiceRequestListItemDto>
        {
            Items = mapper.Map<List<ServiceRequestListItemDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ServiceRequestDetailsDto> GetByIdAsync(int id, int currentUserId, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(id, cancellationToken) ?? throw NotFound(id);
        var dto = mapper.Map<ServiceRequestDetailsDto>(request);

        // פרטיות: מי תפסה את הבקשה - רק ההורה שפרסם והשמרטפית שתפסה רואים. לשאר נשאר רק IsAssigned.
        if (currentUserId != request.ParentId && currentUserId != request.AssignedBabysitterId)
        {
            dto.AssignedBabysitterId = null;
            dto.AssignedBabysitterFullName = null;
            dto.AssignedBabysitterPhone = null;
            dto.AssignedAtUtc = null;
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
        return await GetByIdAsync(request.Id, parentId, cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateServiceRequestDto dto, int parentId, CancellationToken cancellationToken)
    {
        var request = await GetOwnedForUpdateAsync(id, parentId, cancellationToken);

        if (request.AssignedBabysitterId is not null)
        {
            throw new ConflictException("לא ניתן לערוך בקשה שכבר נתפסה. אפשר למחוק אותה ולפרסם בקשה חדשה.");
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
        // מחיקה מותרת גם לבקשה משובצת (כך מבטלים). ה-Version נבדק גם ב-DELETE.
        var request = await GetOwnedForUpdateAsync(id, parentId, cancellationToken);

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

        // 6. שיבוץ ושמירה. אם מישהו שמר בינתיים - ה-Repository זורק ConcurrencyConflictException (409).
        request.AssignedBabysitterId = babysitterId;
        request.AssignedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} assigned to babysitter {BabysitterId}", requestId, babysitterId);

        // טעינה מחדש - כדי שהתשובה תכלול את פרטי השמרטפית.
        return await GetByIdAsync(requestId, babysitterId, cancellationToken);
    }

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
