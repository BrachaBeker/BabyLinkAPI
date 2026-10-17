// חריגה: הפעולה מתנגשת במצב הנוכחי (בקשה תפוסה, אימייל קיים, שיבוץ חופף) -> 409.
namespace BabyLink.Application.Exceptions;

public class ConflictException(string message) : Exception(message);
