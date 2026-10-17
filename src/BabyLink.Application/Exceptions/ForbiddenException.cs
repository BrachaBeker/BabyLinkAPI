// חריגה: המשתמש מחובר אבל אין לו הרשאה למשאב הזה (למשל עריכת בקשה של הורה אחר) -> 403.
namespace BabyLink.Application.Exceptions;

public class ForbiddenException(string message) : Exception(message);
