// DTO לעדכון פרטי הפרופיל (PUT /api/users/{id}). אימייל, סיסמה ותפקיד לא משתנים כאן.
// הכתובת חובה להורה - נבדק ב-Service, כי התפקיד לא מגיע ב-Body אלא מה-DB.
using System.ComponentModel.DataAnnotations;

namespace BabyLink.Application.DTOs.Users;

public class UpdateUserDto
{
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
}
