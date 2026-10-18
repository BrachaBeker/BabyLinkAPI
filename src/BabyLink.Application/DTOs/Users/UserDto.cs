// DTO להחזרת פרטי משתמש (אחרי הרשמה, ובפרופיל) - בלי PasswordHash!
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Users;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public UserRole Role { get; set; }
}
