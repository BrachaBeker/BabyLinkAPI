// ממשק גישה להודעות למשתמשים (המימוש ב-Infrastructure).
using BabyLink.Domain.Entities;

namespace BabyLink.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    // ההודעות של המשתמש, מהחדשה לישנה (קריאה בלבד).
    Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, CancellationToken cancellationToken);

    // עם Tracking - לפני מחיקה.
    Task<Notification?> GetByIdAsync(int id, CancellationToken cancellationToken);

    // הוספה בלי שמירה. ה-DbContext משותף לכל ה-Repositories בבקשה (Scoped), ולכן הודעה שנוספת
    // בזמן ביטול בקשה נשמרת באותה SaveChanges של המחיקה - שתיהן מצליחות או שתיהן נכשלות.
    void Add(Notification notification);

    void Remove(Notification notification);

    // מחיקת כל ההודעות של המשתמש ישירות ב-DB. מחזיר כמה נמחקו.
    Task<int> RemoveAllForUserAsync(int userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
