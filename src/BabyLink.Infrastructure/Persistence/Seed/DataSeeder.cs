// Seed: מריץ את ה-Migrations ומכניס 4 משתמשי דמו אם אין משתמשים (סיסמה לכולם: Demo123!).
// כך המערכת עולה בלי שום התערבות ידנית ב-DB. (בקשות יתווספו בשלב 6, תגיות בשלב 7.)
using BabyLink.Application.Interfaces.Security;
using BabyLink.Domain.Entities;
using BabyLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public const string DemoPassword = "Demo123!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);

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
    }
}
