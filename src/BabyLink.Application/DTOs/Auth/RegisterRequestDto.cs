// DTO להרשמה (POST /api/users). Data Annotations נבדקות אוטומטית ע"י [ApiController] -> 400.
using System.ComponentModel.DataAnnotations;
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    // Nullable + [Required] - כדי שחובה יהיה לבחור תפקיד (אחרת ברירת המחדל הייתה Parent).
    [Required]
    public UserRole? Role { get; set; }
}
