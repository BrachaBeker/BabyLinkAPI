// הודעה למשתמש, שמוצגת לו בכניסה הבאה למערכת (פעמון ההודעות בלקוח).
// פרטי הבקשה נשמרים בהודעה עצמה, כי בביטול הבקשה נמחקת. את הזמנים מציג הלקוח בשעון המקומי.
using BabyLink.Domain.Enums;

namespace BabyLink.Domain.Entities;

public class Notification
{
    public int Id { get; set; }

    // מי מקבל את ההודעה.
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    // הבקשה שההודעה עוסקת בה - כל עוד היא קיימת (בקשה שנמחקה -> null).
    public int? RequestId { get; set; }
    public ServiceRequest? Request { get; set; }

    // פרטי הבקשה (נשמרים גם אחרי שהבקשה נמחקה).
    public DateTime RequestStartTimeUtc { get; set; }
    public DateTime RequestEndTimeUtc { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;

    // מי ביצע את הפעולה: ההורה שביטל, או השמרטפית שתפסה / ביטלה את השיבוץ.
    public string ActorFullName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
