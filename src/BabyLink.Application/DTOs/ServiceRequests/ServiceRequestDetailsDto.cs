// DTO מלא למסך פרטי בקשה: כתובת מלאה, פרטי ההורה, פרטי השמרטפית שתפסה (אם יש) ותגיות.
// השמות ParentFullName / AssignedBabysitterFullName מאפשרים ל-AutoMapper לבצע Flattening אוטומטי.
using BabyLink.Application.DTOs.Tags;

namespace BabyLink.Application.DTOs.ServiceRequests;

public class ServiceRequestDetailsDto
{
    public int Id { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string HouseNumber { get; set; } = string.Empty;
    public string? AddressNotes { get; set; }

    public int ChildrenCount { get; set; }
    public decimal HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public int ParentId { get; set; }
    public string ParentFullName { get; set; } = string.Empty;
    public string ParentPhone { get; set; } = string.Empty;

    public int? AssignedBabysitterId { get; set; }
    public string? AssignedBabysitterFullName { get; set; }
    public string? AssignedBabysitterPhone { get; set; }
    public DateTime? AssignedAtUtc { get; set; }

    public List<TagDto> Tags { get; set; } = [];

    public bool IsAssigned { get; set; }
    public bool IsExpired { get; set; }
}
