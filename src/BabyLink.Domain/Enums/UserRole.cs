// תפקידי המשתמשים. נכנס ל-JWT כ-Role Claim ומשמש ב-[Authorize(Roles = ...)].
namespace BabyLink.Domain.Enums;

public enum UserRole
{
    Parent,
    Babysitter
}
