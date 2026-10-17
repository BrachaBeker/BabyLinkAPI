// DTO לעדכון בקשה (PUT) - אותם שדות ואותה ולידציה כמו ביצירה (כולל הכתובת).
using System.ComponentModel.DataAnnotations;

namespace BabyLink.Application.DTOs.ServiceRequests;

public class UpdateServiceRequestDto
{
    [Required]
    public DateTime StartTimeUtc { get; set; }

    [Required]
    public DateTime EndTimeUtc { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Street { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string HouseNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AddressNotes { get; set; }

    [Range(1, 10)]
    public int ChildrenCount { get; set; }

    [Range(typeof(decimal), "1", "1000")]
    public decimal HourlyRate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // מזהי תגיות (לא חובה). מזהה שלא קיים -> 400.
    [MaxLength(10)]
    public List<int> TagIds { get; set; } = [];
}
