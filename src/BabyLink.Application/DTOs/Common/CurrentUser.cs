// המשתמש המחובר כפי שהוא מופיע ב-JWT: מזהה ותפקיד. ה-Controller בונה אותו, וה-Service מחליט לפיו מה מותר לראות.
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Common;

public record CurrentUser(int Id, UserRole Role)
{
    public bool IsParent => Role == UserRole.Parent;
}
