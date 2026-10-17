// ממשק גישה לנתוני בקשות (המימוש ב-Infrastructure).
// מתודות: GetPagedAsync (סינון/מיון/Pagination בתוך השאילתה), GetByIdAsync (קריאה בלבד),
//         GetForUpdateAsync (עם Tracking - לפני עדכון), HasOverlappingAssignmentAsync,
//         AddAsync, Remove, SaveChangesAsync.
// SaveChangesAsync זורק ConcurrencyConflictException (לא חריגה של EF!) כשיש התנגשות.
