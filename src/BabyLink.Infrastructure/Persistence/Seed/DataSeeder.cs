// Seed: מריץ את ה-Migrations בעליית השרת, כך שהמערכת עולה בלי שום התערבות ידנית ב-DB.
// (משתמשי דמו יתווספו בשלב 5, בקשות בשלב 6, תגיות בשלב 7.)
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);
    }
}
