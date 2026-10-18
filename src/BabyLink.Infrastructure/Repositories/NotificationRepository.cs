// מימוש INotificationRepository מול AppDbContext.
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Domain.Entities;
using BabyLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, CancellationToken cancellationToken) =>
        await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .ToListAsync(cancellationToken);

    public Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public void Add(Notification notification) => db.Notifications.Add(notification);

    public void Remove(Notification notification) => db.Notifications.Remove(notification);

    public Task<int> RemoveAllForUserAsync(int userId, CancellationToken cancellationToken) =>
        db.Notifications.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await db.SaveChangesAsync(cancellationToken);
}
