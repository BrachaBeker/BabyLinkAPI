// סוגי ההודעות למשתמש. נשמר כמחרוזת ב-DB.
namespace BabyLink.Domain.Enums;

public enum NotificationType
{
    // ההורה ביטל (מחק) בקשה שהשמרטפית כבר תפסה. נשלחת לשמרטפית.
    RequestCancelled,

    // שמרטפית תפסה בקשה של ההורה. נשלחת להורה.
    RequestAssigned,

    // השמרטפית ביטלה את השיבוץ, והבקשה חזרה להמתין לשמרטפית. נשלחת להורה.
    AssignmentCancelled
}
