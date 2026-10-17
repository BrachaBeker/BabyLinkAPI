// הגדרות JWT (IOptions<JwtOptions> מסעיף "Jwt"). Key הוא סוד - רק ב-User Secrets / משתני סביבה.
namespace BabyLink.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
