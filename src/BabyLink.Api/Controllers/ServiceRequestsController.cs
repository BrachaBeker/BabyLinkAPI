// [Authorize] - כל ה-endpoints דורשים התחברות.
// GET    /api/service-requests             - רשימה עם סינון/מיון/Pagination ([FromQuery] ServiceRequestQueryParameters)
//                                             סינון לפי city, street, זמן, תגית, תעריף -> 200
// GET    /api/service-requests/{id}        - פרטי בקשה (כולל כתובת מלאה) -> 200 / 404
// POST   /api/service-requests             - [Authorize(Roles=Parent)] יצירה -> 201 + Location / 400
// PUT    /api/service-requests/{id}        - [Authorize(Roles=Parent)] עדכון -> 204 / 403 / 404 / 409
// DELETE /api/service-requests/{id}        - [Authorize(Roles=Parent)] מחיקה -> 204 / 403 / 404 / 409
// PUT    /api/service-requests/{id}/assignment - [Authorize(Roles=Babysitter)] תפיסת בקשה
//                                               -> 200 / 400 / 404 / 409 (מישהי הקדימה)
// ה-Controller דק: שולף UserId מה-Claims, קורא ל-Service, מחזיר סטטוס. אין כאן לוגיקה עסקית.
