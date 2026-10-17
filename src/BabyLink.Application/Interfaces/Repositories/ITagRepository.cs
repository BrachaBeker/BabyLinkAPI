// ממשק גישה לתגיות (המימוש ב-Infrastructure).
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Interfaces.Repositories;

public interface ITagRepository
{
    // קריאה בלבד (AsNoTracking) - לרשימת הסינון.
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken);

    // עם Tracking - כדי לשייך את התגיות לבקשה בלי ש-EF ינסה להכניס אותן מחדש.
    Task<IReadOnlyList<Tag>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
