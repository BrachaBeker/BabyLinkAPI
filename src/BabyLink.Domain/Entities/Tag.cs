// תגית לבקשה (תינוק, עזרה בשיעורים, שמירה בזמן שינה...). קשר Many-to-Many עם ServiceRequest; נוצרות ב-Seed.
namespace BabyLink.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
