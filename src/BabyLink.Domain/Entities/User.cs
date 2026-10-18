// ישות משתמש/ת - הורה או שמרטפית. POCO בלי Attributes; ההגדרות ב-Fluent API ב-Infrastructure.
using BabyLink.Domain.Enums;

namespace BabyLink.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // כתובת הבית - חובה להורה (ממלאת מראש את הכתובת בבקשה חדשה), לא נשמרת לשמרטפית.
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }

    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<ServiceRequest> CreatedRequests { get; set; } = new List<ServiceRequest>();
    public ICollection<ServiceRequest> AssignedRequests { get; set; } = new List<ServiceRequest>();
}
