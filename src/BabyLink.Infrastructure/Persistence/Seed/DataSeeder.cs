// Seed: מריץ את ה-Migrations, ומכניס תגיות, 4 משתמשי דמו (סיסמה: Demo123!) ו-10 בקשות עם תגיות -
// כל טבלה רק אם היא ריקה. כך המערכת עולה בלי שום התערבות ידנית ב-DB.
using BabyLink.Application.Interfaces.Security;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public const string DemoPassword = "Demo123!";

    private const string Baby = "תינוק";
    private const string Homework = "עזרה בשיעורים";
    private const string SleepWatch = "שמירה בזמן שינה";
    private const string NightShift = "משמרת לילה";
    private const string SpecialNeeds = "צרכים מיוחדים";

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
                new User { Email = "parent1@babylink.dev", FullName = "רחל כהן", Phone = "050-1111111", City = "ירושלים", Role = UserRole.Parent, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = "parent2@babylink.dev", FullName = "שרה לוי", Phone = "050-2222222", City = "בני ברק", Role = UserRole.Parent, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = "sitter1@babylink.dev", FullName = "מיכל אברהם", Phone = "050-3333333", City = "ירושלים", Role = UserRole.Babysitter, PasswordHash = hash, CreatedAtUtc = now },
                new User { Email = "sitter2@babylink.dev", FullName = "נועה פרץ", Phone = "050-4444444", City = "בני ברק", Role = UserRole.Babysitter, PasswordHash = hash, CreatedAtUtc = now });

            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.ServiceRequests.AnyAsync(cancellationToken))
        {
            await SeedServiceRequestsAsync(db, cancellationToken);
        }
    }

    // 10 בקשות עתידיות בכמה ערים ורחובות (כדי שהסינון וה-Pagination יהיו מעניינים); 3 מהן כבר משובצות.
    // דורש שהתגיות והמשתמשים כבר קיימים.
    private static async Task SeedServiceRequestsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var users = await db.Users.ToDictionaryAsync(u => u.Email, u => u.Id, cancellationToken);
        var tags = await db.Tags.ToDictionaryAsync(t => t.Name, cancellationToken);
        var parent1 = users["parent1@babylink.dev"];
        var parent2 = users["parent2@babylink.dev"];
        var sitter1 = users["sitter1@babylink.dev"];
        var sitter2 = users["sitter2@babylink.dev"];

        var now = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

        ServiceRequest Request(int parentId, int daysFromToday, int startHourUtc, int hours, string city, string street,
            string houseNumber, int children, decimal rate, string? description = null, int? babysitterId = null,
            params string[] tagNames) => new()
        {
            ParentId = parentId,
            StartTimeUtc = today.AddDays(daysFromToday).AddHours(startHourUtc),
            EndTimeUtc = today.AddDays(daysFromToday).AddHours(startHourUtc + hours),
            City = city,
            Street = street,
            HouseNumber = houseNumber,
            ChildrenCount = children,
            HourlyRate = rate,
            Description = description,
            CreatedAtUtc = now,
            AssignedBabysitterId = babysitterId,
            AssignedAtUtc = babysitterId is null ? null : now,
            Tags = [.. tagNames.Select(name => tags[name])]
        };

        db.ServiceRequests.AddRange(
            Request(parent1, 1, 15, 4, "ירושלים", "הרצל", "12", 2, 45m, "שני ילדים, 4 ו-7", null, Homework),
            Request(parent1, 2, 16, 3, "ירושלים", "שדרות הרצל", "5א", 1, 50m, "תינוקת בת שנה", null, Baby),
            Request(parent1, 3, 17, 5, "ירושלים", "יפו", "101", 3, 55m, null, sitter1, SleepWatch, Homework),
            Request(parent1, 4, 14, 3, "ירושלים", "בן יהודה", "8", 2, 40m),
            Request(parent2, 1, 13, 4, "בני ברק", "רבי עקיבא", "54", 4, 60m, "ארבעה ילדים, עזרה בארוחת ערב", null, Baby, Homework),
            Request(parent2, 2, 15, 3, "בני ברק", "ז'בוטינסקי", "120", 2, 45m, null, sitter2, SleepWatch),
            Request(parent2, 5, 18, 6, "בני ברק", "רבי עקיבא", "3", 3, 50m, "משמרת ערב", null, NightShift),
            Request(parent2, 3, 9, 8, "תל אביב", "דיזנגוף", "200", 1, 65m, null, null, Baby, SpecialNeeds),
            Request(parent1, 6, 15, 3, "חיפה", "הרצל", "40", 2, 42m, null, sitter1),
            Request(parent2, 7, 16, 4, "חיפה", "מוריה", "17", 1, 48m, "ילד עם אלרגיה לבוטנים", null, SpecialNeeds));

        await db.SaveChangesAsync(cancellationToken);
    }
}
