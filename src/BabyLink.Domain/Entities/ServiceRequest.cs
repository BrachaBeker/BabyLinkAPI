// בקשת שמרטפות - המשאב המוגבל. AssignedBabysitterId == null פירושו פנויה; Version הוא ה-Concurrency Token.
// אין עמודת סטטוס: פנויה/תפוסה/פג תוקף מחושבים בשאילתה. (Tags יתווספו בשלב 7.)
namespace BabyLink.Domain.Entities;

public class ServiceRequest
{
    public int Id { get; set; }
    public int ParentId { get; set; }
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

    public int? AssignedBabysitterId { get; set; }
    public DateTime? AssignedAtUtc { get; set; }
    public Guid Version { get; set; }

    public User Parent { get; set; } = null!;
    public User? AssignedBabysitter { get; set; }
}
