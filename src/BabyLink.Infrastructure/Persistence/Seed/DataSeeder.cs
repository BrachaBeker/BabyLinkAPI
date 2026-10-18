// Seed: מריץ את ה-Migrations, ומכניס תגיות, 4 משתמשי דמו (סיסמה: Demo123!), בקשות (עתידיות ומהעבר)
// והודעת ביטול לדוגמה - כל טבלה רק אם היא ריקה. כך המערכת עולה בלי שום התערבות ידנית ב-DB.
// בנוסף, "דמו חי": אם נשארו מעט בקשות פנויות (כי בקשות ה-Seed כבר עברו), מתווסף סבב בקשות חדש.
using BabyLink.Application.Interfaces.Security;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public const string DemoPassword = "Demo123!";

    private const string Parent1Email = "parent1@babylink.dev";
    private const string Parent2Email = "parent2@babylink.dev";
    private const string Sitter1Email = "sitter1@babylink.dev";
    private const string Sitter2Email = "sitter2@babylink.dev";

    private const string Baby = "תינוק";
    private const string Homework = "עזרה בשיעורים";
    private const string SleepWatch = "שמירה בזמן שינה";
    private const string NightShift = "משמרת לילה";
    private const string SpecialNeeds = "צרכים מיוחדים";

    // פחות מזה בקשות פנויות עתידיות - מוסיפים סבב בקשות דמו חדש.
    private const int MinOpenRequests = 3;

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Tags.AnyAsync(cancellationToken))
        {
            db.Tags.AddRange(new[] { Baby, Homework, SleepWatch, NightShift, SpecialNeeds }.Select(name => new Tag { Name = name }));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var hash = passwordHasher.Hash(DemoPassword);
            var now = DateTime.UtcNow;

            db.Users.AddRange(
                new User { Email = Parent1Email, FullName = "רחל כהן", Phone = "050-1111111", City = "ירושלים", Street = "הרצל", HouseNumber = "12", Role = UserRole.Parent, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = Parent2Email, FullName = "שרה לוי", Phone = "050-2222222", City = "בני ברק", Street = "רבי עקיבא", HouseNumber = "54", Role = UserRole.Parent, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = Sitter1Email, FullName = "מיכל אברהם", Phone = "050-3333333", City = "ירושלים", Role = UserRole.Babysitter, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = Sitter2Email, FullName = "נועה פרץ", Phone = "050-4444444", City = "בני ברק", Role = UserRole.Babysitter, PasswordHash = hash, CreatedAtUtc = now });

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.ServiceRequests.AnyAsync(cancellationToken))
        {
            await SeedServiceRequestsAsync(db, cancellationToken);
        }

        await EnsureOpenRequestsAsync(db, cancellationToken);
    }

    // בקשות עתידיות בכמה ערים ורחובות (כדי שהסינון וה-Pagination יהיו מעניינים), חלקן כבר משובצות;
    // בקשות מהעבר (ללשונית "היסטוריה" של ההורה); והודעת ביטול לדוגמה לשמרטפית 1.
    private static async Task SeedServiceRequestsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var demo = await DemoContext.LoadAsync(db, cancellationToken);
        if (demo is null)
        {
            return;
        }

        // בקשה עתידית שכבר נתפסה - עליה ההודעה לדוגמה "נמצאה שמרטפית!" להורה 1.
        var takenBySitter1 = demo.Request(demo.Parent1, 3, 17, 5, "ירושלים", "יפו", "101", 3, 55m, null, demo.Sitter1, SleepWatch, Homework);

        db.ServiceRequests.AddRange(
            // עתידיות
            demo.Request(demo.Parent1, 1, 15, 4, "ירושלים", "הרצל", "12", 2, 45m, "שני ילדים, 4 ו-7", null, Homework),
            demo.Request(demo.Parent1, 2, 16, 3, "ירושלים", "שדרות הרצל", "5א", 1, 50m, "תינוקת בת שנה", null, Baby),
            takenBySitter1,
            demo.Request(demo.Parent1, 4, 14, 3, "ירושלים", "בן יהודה", "8", 2, 40m),
            demo.Request(demo.Parent2, 1, 13, 4, "בני ברק", "רבי עקיבא", "54", 4, 60m, "ארבעה ילדים, עזרה בארוחת ערב", null, Baby, Homework),
            demo.Request(demo.Parent2, 2, 15, 3, "בני ברק", "ז'בוטינסקי", "120", 2, 45m, null, demo.Sitter2, SleepWatch),
            demo.Request(demo.Parent2, 5, 18, 6, "בני ברק", "רבי עקיבא", "3", 3, 50m, "משמרת ערב", null, NightShift),
            demo.Request(demo.Parent2, 3, 9, 8, "תל אביב", "דיזנגוף", "200", 1, 65m, null, null, Baby, SpecialNeeds),
            demo.Request(demo.Parent1, 6, 15, 3, "חיפה", "הרצל", "40", 2, 42m, null, demo.Sitter1),
            demo.Request(demo.Parent2, 7, 16, 4, "חיפה", "מוריה", "17", 1, 48m, "ילד עם אלרגיה לבוטנים", null, SpecialNeeds),

            // מהעבר - עסקאות שהיו, ובקשה אחת שאף שמרטפית לא תפסה
            demo.Request(demo.Parent1, -2, 16, 4, "ירושלים", "הרצל", "12", 2, 45m, "שני ילדים, 4 ו-7", demo.Sitter1, Homework),
            demo.Request(demo.Parent1, -6, 17, 3, "ירושלים", "הרצל", "12", 2, 50m, null, demo.Sitter2, SleepWatch),
            demo.Request(demo.Parent1, -4, 15, 3, "ירושלים", "הרצל", "12", 2, 40m),
            demo.Request(demo.Parent2, -3, 14, 5, "בני ברק", "רבי עקיבא", "54", 4, 55m, "ארבעה ילדים", demo.Sitter2, Baby));

        // הודעות לדוגמה, כדי שפעמון ההודעות לא יהיה ריק בהדגמה:
        // לשמרטפית 1 - ההורה ביטל בקשה שתפסה; להורה 1 - שמרטפית 1 תפסה את הבקשה ביפו.
        var cancelledStart = demo.Today.AddDays(2).AddHours(17);
        db.Notifications.AddRange(
            new Notification
            {
                UserId = demo.Sitter1,
                Type = NotificationType.RequestCancelled,
                RequestStartTimeUtc = cancelledStart,
                RequestEndTimeUtc = cancelledStart.AddHours(3),
                City = "בני ברק",
                Street = "רבי עקיבא",
                HouseNumber = "54",
                ActorFullName = "שרה לוי",
                CreatedAtUtc = demo.Now.AddHours(-1)
            },
            new Notification
            {
                UserId = demo.Parent1,
                Type = NotificationType.RequestAssigned,
                Request = takenBySitter1,
                RequestStartTimeUtc = takenBySitter1.StartTimeUtc,
                RequestEndTimeUtc = takenBySitter1.EndTimeUtc,
                City = takenBySitter1.City,
                Street = takenBySitter1.Street,
                HouseNumber = takenBySitter1.HouseNumber,
                ActorFullName = "מיכל אברהם",
                CreatedAtUtc = takenBySitter1.AssignedAtUtc ?? demo.Now
            });

        await db.SaveChangesAsync(cancellationToken);
    }

    // דמו חי: אחרי כמה ימים כל בקשות ה-Seed כבר "עברו", ולשמרטפית שנכנסת אין מה לתפוס.
    // בכל עלייה של השרת בודקים, ואם נשארו פחות מ-MinOpenRequests בקשות פנויות - מוסיפים סבב חדש להורי הדמו.
    private static async Task EnsureOpenRequestsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var openCount = await db.ServiceRequests.CountAsync(r => r.AssignedBabysitterId == null && r.StartTimeUtc > now, cancellationToken);
        if (openCount >= MinOpenRequests)
        {
            return;
        }

        var demo = await DemoContext.LoadAsync(db, cancellationToken);
        if (demo is null)
        {
            return;
        }

        db.ServiceRequests.AddRange(
            demo.Request(demo.Parent1, 1, 16, 4, "ירושלים", "הרצל", "12", 2, 45m, "שני ילדים, 4 ו-7", null, Homework),
            demo.Request(demo.Parent2, 2, 15, 3, "בני ברק", "רבי עקיבא", "54", 4, 55m, "ארבעה ילדים, עזרה בארוחת ערב", null, Baby),
            demo.Request(demo.Parent1, 3, 18, 4, "ירושלים", "הרצל", "12", 2, 50m, null, null, SleepWatch),
            demo.Request(demo.Parent2, 4, 9, 8, "תל אביב", "דיזנגוף", "200", 1, 65m, null, null, Baby, SpecialNeeds),
            demo.Request(demo.Parent1, 5, 15, 3, "חיפה", "הרצל", "40", 2, 42m),
            demo.Request(demo.Parent2, 6, 18, 6, "בני ברק", "רבי עקיבא", "54", 3, 50m, "משמרת ערב", null, NightShift));

        await db.SaveChangesAsync(cancellationToken);
    }

    // מה שה-Seed צריך: מזהי משתמשי הדמו, התגיות, ופונקציה שבונה בקשה יחסית ל"היום".
    private sealed class DemoContext
    {
        public required int Parent1 { get; init; }
        public required int Parent2 { get; init; }
        public required int Sitter1 { get; init; }
        public required int Sitter2 { get; init; }
        public required Dictionary<string, Tag> Tags { get; init; }
        public DateTime Now { get; } = DateTime.UtcNow;
        public DateTime Today => DateTime.SpecifyKind(Now.Date, DateTimeKind.Utc);

        // null אם משתמשי הדמו לא קיימים (למשל DB שנוצר לפני שהיו).
        public static async Task<DemoContext?> LoadAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var users = await db.Users
                .Where(u => u.Email == Parent1Email || u.Email == Parent2Email || u.Email == Sitter1Email || u.Email == Sitter2Email)
                .ToDictionaryAsync(u => u.Email, u => u.Id, cancellationToken);

            if (users.Count < 4)
            {
                return null;
            }

            return new DemoContext
            {
                Parent1 = users[Parent1Email],
                Parent2 = users[Parent2Email],
                Sitter1 = users[Sitter1Email],
                Sitter2 = users[Sitter2Email],
                Tags = await db.Tags.ToDictionaryAsync(t => t.Name, cancellationToken)
            };
        }

        // daysFromToday שלילי = בקשה מהעבר. בקשה מהעבר "פורסמה" 3 ימים לפני המועד; עתידית - לפני 3 שעות.
        // בקשה משובצת "נתפסה" 35 דקות אחרי הפרסום.
        public ServiceRequest Request(int parentId, int daysFromToday, int startHourUtc, int hours, string city, string street,
            string houseNumber, int children, decimal rate, string? description = null, int? babysitterId = null,
            params string[] tagNames)
        {
            var start = Today.AddDays(daysFromToday).AddHours(startHourUtc);
            var createdAt = start < Now ? start.AddDays(-3) : Now.AddHours(-3);

            return new ServiceRequest
            {
                ParentId = parentId,
                StartTimeUtc = start,
                EndTimeUtc = start.AddHours(hours),
                City = city,
                Street = street,
                HouseNumber = houseNumber,
                ChildrenCount = children,
                HourlyRate = rate,
                Description = description,
                CreatedAtUtc = createdAt,
                AssignedBabysitterId = babysitterId,
                AssignedAtUtc = babysitterId is null ? null : createdAt.AddMinutes(35),
                Tags = [.. tagNames.Where(Tags.ContainsKey).Select(name => Tags[name])]
            };
        }
    }
}
