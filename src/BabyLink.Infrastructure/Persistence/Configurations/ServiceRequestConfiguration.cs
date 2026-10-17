// Fluent API ל-ServiceRequest:
// - One-to-Many: Parent -> CreatedRequests (מחיקת הורה מוחקת בקשות).
// - One-to-Many: AssignedBabysitter -> AssignedRequests (אופציונלי, Restrict).
// - Many-to-Many: Tags <-> ServiceRequests עם טבלת קישור ServiceRequestTags.
// - Version מוגדר כ-IsConcurrencyToken() - זה ה-Concurrency Token!
// - City, Street, HouseNumber: Required + MaxLength.
// - אינדקסים: (City, Street) לסינון לפי כתובת, StartTimeUtc לסינון לפי זמן.
