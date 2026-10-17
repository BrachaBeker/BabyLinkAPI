// מימוש IServiceRequestService - הלב של המערכת. כל הוולידציה העסקית כאן:
// סיום אחרי התחלה, התחלה בעתיד, עד 24 שעות; רק ההורה שיצר עורך/מוחק (403); בקשה משובצת לא נערכת (409).
// (AssignAsync - תפיסת בקשה - ייכתב בשלב 8.)
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
    IMapper mapper,
    ILogger<ServiceRequestService> logger) : IServiceRequestService
{
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(24);

    public async Task<PagedResult<ServiceRequestListItemDto>> GetPagedAsync(ServiceRequestQueryParameters query, int currentUserId, CancellationToken cancellationToken)
    {
        query.FromUtc = ToUtc(query.FromUtc);
        query.ToUtc = ToUtc(query.ToUtc);

        var page = await repository.GetPagedAsync(query, currentUserId, cancellationToken);

        return new PagedResult<ServiceRequestListItemDto>
        {
            Items = mapper.Map<List<ServiceRequestListItemDto>>(page.Items),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ServiceRequestDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(id, cancellationToken) ?? throw NotFound(id);
        return mapper.Map<ServiceRequestDetailsDto>(request);
    }

    public async Task<ServiceRequestDetailsDto> CreateAsync(CreateServiceRequestDto dto, int parentId, CancellationToken cancellationToken)
    {
        dto.StartTimeUtc = ToUtc(dto.StartTimeUtc);
        dto.EndTimeUtc = ToUtc(dto.EndTimeUtc);
        ValidateTimes(dto.StartTimeUtc, dto.EndTimeUtc);

        var request = mapper.Map<ServiceRequest>(dto);
        request.ParentId = parentId;
        request.CreatedAtUtc = DateTime.UtcNow;

        await repository.AddAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} created by parent {ParentId}", request.Id, parentId);

        // טעינה מחדש - כדי שהתשובה תכלול את פרטי ההורה.
        return await GetByIdAsync(request.Id, cancellationToken);
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

        mapper.Map(dto, request);
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

    public Task<ServiceRequestDetailsDto> AssignAsync(int requestId, int babysitterId, CancellationToken cancellationToken) =>
        throw new NotImplementedException("Assignment is implemented in stage 8.");

    private async Task<ServiceRequest> GetOwnedForUpdateAsync(int id, int parentId, CancellationToken cancellationToken)
    {
        var request = await repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound(id);

        if (request.ParentId != parentId)
        {
            throw new ForbiddenException("רק ההורה שפרסם את הבקשה יכול לשנות אותה.");
        }

        return request;
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
