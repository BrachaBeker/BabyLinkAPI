// ממשק גישה לנתוני משתמשים (המימוש ב-Infrastructure). הכול async + CancellationToken.
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // עם Tracking - לפני עדכון פרטי הפרופיל.
    Task<User?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
