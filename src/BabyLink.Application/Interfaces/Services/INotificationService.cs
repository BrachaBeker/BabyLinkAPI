// ממשק שירות ההודעות למשתמש המחובר (למשל: ההורה ביטל בקשה שתפסת).
using BabyLink.Application.DTOs.Notifications;

namespace BabyLink.Application.Interfaces.Services;

public interface INotificationService
{
    // ההודעות של המשתמש, מהחדשה לישנה.
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(int userId, CancellationToken cancellationToken);

    // סגירת הודעה ("הבנתי"). לא קיימת -> 404; של משתמש אחר -> 403.
    Task DismissAsync(int id, int userId, CancellationToken cancellationToken);

    // סגירת כל ההודעות של המשתמש.
    Task DismissAllAsync(int userId, CancellationToken cancellationToken);
}
