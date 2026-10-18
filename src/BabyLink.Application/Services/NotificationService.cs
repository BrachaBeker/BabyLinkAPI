// מימוש INotificationService. ההודעות נוצרות ב-ServiceRequestService (בביטול בקשה שנתפסה),
// וכאן המשתמש קורא אותן וסוגר אותן.
using AutoMapper;
using BabyLink.Application.DTOs.Notifications;
using BabyLink.Application.Exceptions;
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BabyLink.Application.Services;

public class NotificationService(
    INotificationRepository repository,
    IMapper mapper,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(int userId, CancellationToken cancellationToken)
    {
        var notifications = await repository.GetForUserAsync(userId, cancellationToken);
        return mapper.Map<List<NotificationDto>>(notifications);
    }

    public async Task DismissAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"הודעה {id} לא נמצאה.");

        if (notification.UserId != userId)
        {
            throw new ForbiddenException("אפשר לסגור רק הודעות שלך.");
        }

        repository.Remove(notification);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DismissAllAsync(int userId, CancellationToken cancellationToken)
    {
        var count = await repository.RemoveAllForUserAsync(userId, cancellationToken);
        logger.LogInformation("User {UserId} dismissed {Count} notifications", userId, count);
    }
}
