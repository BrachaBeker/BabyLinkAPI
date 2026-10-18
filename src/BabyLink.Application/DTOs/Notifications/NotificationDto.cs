// DTO להודעה למשתמש. Type כמחרוזת ("RequestCancelled" / "RequestAssigned" / "AssignmentCancelled");
// את הנוסח מרכיב הלקוח מהשדות. RequestId - לקישור לפרטי הבקשה (null אם הבקשה נמחקה).
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public int? RequestId { get; set; }
    public DateTime RequestStartTimeUtc { get; set; }
    public DateTime RequestEndTimeUtc { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string ActorFullName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
