// DTO מקוצר לשורה ברשימה: עיר ורחוב בלי מספר בית, שמות התגיות; IsAssigned ו-IsExpired מחושבים.
namespace BabyLink.Application.DTOs.ServiceRequests;

public class ServiceRequestListItemDto
{
    public int Id { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int ChildrenCount { get; set; }
    public decimal HourlyRate { get; set; }
    public List<string> TagNames { get; set; } = [];
    public bool IsAssigned { get; set; }
    public bool IsExpired { get; set; }
}
