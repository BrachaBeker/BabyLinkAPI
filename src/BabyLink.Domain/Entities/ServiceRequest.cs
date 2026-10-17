// ישות בקשת שמרטפות - זה המשאב המוגבל שמתחרים עליו.
// שדות: Id, ParentId, StartTimeUtc, EndTimeUtc, ChildrenCount, HourlyRate, Description, CreatedAtUtc.
// כתובת: City, Street, HouseNumber (string - כדי לאפשר "12א"), AddressNotes (אופציונלי: קומה, כניסה).
// שדות התחרות: AssignedBabysitterId (null = פנויה), AssignedAtUtc,
//              Version (Guid) - ה-Concurrency Token, מתחלף בכל שמירה.
// ניווטים: Parent (N:1), AssignedBabysitter (N:1), Tags (M:N).
// אין עמודת סטטוס: פנויה/תפוסה/פג תוקף מחושבים בשאילתה.
