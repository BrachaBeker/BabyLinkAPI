// חריגה: אימייל או סיסמה שגויים -> 401. ההודעה כללית בכוונה, בלי לפרט מה שגוי.
namespace BabyLink.Application.Exceptions;

public class InvalidCredentialsException() : Exception("אימייל או סיסמה שגויים.");
