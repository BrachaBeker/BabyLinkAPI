// חריגה: מישהו אחר שמר את הבקשה בין הקריאה לשמירה -> 409 + לוג Warning.
// נזרקת מה-Repository במקום DbUpdateConcurrencyException, כדי ש-Application לא תכיר את EF.
namespace BabyLink.Application.Exceptions;

public class ConcurrencyConflictException(Exception? innerException = null)
    : Exception("הבקשה עודכנה או נתפסה בינתיים על ידי משתמש אחר. יש לרענן ולנסות שוב.", innerException);
