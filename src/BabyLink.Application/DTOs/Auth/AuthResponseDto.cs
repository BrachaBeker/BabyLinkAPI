// DTO שמוחזר אחרי התחברות: ה-JWT, מועד התפוגה ופרטי המשתמש הבסיסיים.
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
