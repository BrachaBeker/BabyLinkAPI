// פרמטרים לרשימה ([FromQuery]): סינון, מיון ו-Pagination. PageSize גדול מ-50 מוגבל ל-50.
// דוגמה: GET /api/service-requests?city=ירושלים&street=הרצל&onlyAvailable=true&page=1
using System.ComponentModel.DataAnnotations;

namespace BabyLink.Application.DTOs.ServiceRequests;

public class ServiceRequestQueryParameters
{
    public const int MaxPageSize = 50;

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(150)]
    public string? Street { get; set; }

    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public int? TagId { get; set; }
    public decimal? MinHourlyRate { get; set; }
    public bool OnlyAvailable { get; set; }
    public bool Mine { get; set; }

    // ברירת מחדל: רק בקשות שמועד ההתחלה שלהן עוד לא הגיע. true - כולל בקשות שעבר זמנן.
    public bool IncludePast { get; set; }

    [RegularExpression("^(startTime|hourlyRate)$", ErrorMessage = "sortBy must be 'startTime' or 'hourlyRate'.")]
    public string SortBy { get; set; } = "startTime";

    public bool Descending { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(value, MaxPageSize);
    }
}
