// מימוש ITagRepository מול AppDbContext. GetAllAsync לקריאה בלבד (AsNoTracking);
// GetByIdsAsync עם Tracking, כי התגיות משויכות לבקשה ונשמרות באותו DbContext.
using BabyLink.Application.Interfaces.Repositories;
using BabyLink.Domain.Entities;
using BabyLink.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyLink.Infrastructure.Repositories;

public class TagRepository(AppDbContext db) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await db.Tags.Where(t => ids.Contains(t.Id)).ToListAsync(cancellationToken);
}
