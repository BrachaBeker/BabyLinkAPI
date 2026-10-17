// מימוש IServiceRequestRepository מול AppDbContext.
// GetPagedAsync: AsNoTracking + Where (סינונים) + OrderBy + CountAsync + Skip/Take - הכול ב-DB.
//              סינון כתובת: City עם EF.Functions.ILike (התאמה מלאה בלי רישיות),
//                           Street עם EF.Functions.ILike("%" + street + "%") (חיפוש חלקי).
//              Include ל-Tags/Parent (או ProjectTo) - בלי N+1.
// GetForUpdateAsync: עם Tracking (נדרש כדי ש-EF יבדוק את ה-Version בעדכון).
// SaveChangesAsync: try/catch על DbUpdateConcurrencyException בלבד
//                   -> זורק ConcurrencyConflictException.
