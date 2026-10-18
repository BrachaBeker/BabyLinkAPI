// DTO להרשמה (POST /api/users). Data Annotations נבדקות אוטומטית ע"י [ApiController] -> 400.
// כתובת (רחוב ומספר בית) - חובה להורה בלבד; הבדיקה ב-Validate (IValidatableObject), כי היא תלויה בתפקיד.
using System.ComponentModel.DataAnnotations;
using BabyLink.Domain.Enums;

namespace BabyLink.Application.DTOs.Auth;

public class RegisterRequestDto : IValidatableObject
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

    [MaxLength(150)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? HouseNumber { get; set; }

    // Nullable + [Required] - כדי שחובה יהיה לבחור תפקיד (אחרת ברירת המחדל הייתה Parent).
    [Required]
    public UserRole? Role { get; set; }

    // רץ אחרי בדיקת ה-Attributes (רק אם הן עברו).
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role != UserRole.Parent)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Street))
        {
            yield return new ValidationResult("להורה חובה למלא רחוב.", [nameof(Street)]);
        }

        if (string.IsNullOrWhiteSpace(HouseNumber))
        {
            yield return new ValidationResult("להורה חובה למלא מספר בית.", [nameof(HouseNumber)]);
        }
    }
}
