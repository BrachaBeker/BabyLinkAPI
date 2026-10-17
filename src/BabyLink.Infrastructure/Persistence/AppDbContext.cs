// ה-DbContext: טוען את כל ה-Configurations מה-Assembly,
// ומחליף את Version של כל ServiceRequest שנוסף/השתנה בכל שמירה (כך אף Service לא צריך לזכור את זה).
using BabyLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ServiceRequest>())
        {
            // גם שינוי של התגיות בלבד נחשב שינוי של הבקשה - כדי שה-Version ייבדק ויתחלף.
            if (entry.State is EntityState.Added or EntityState.Modified ||
                (entry.State == EntityState.Unchanged && entry.Collections.Any(c => c.IsModified)))
            {
                // הערך הישן נשאר כ-OriginalValue ונכנס ל-WHERE של ה-UPDATE.
                entry.Entity.Version = Guid.NewGuid();
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
