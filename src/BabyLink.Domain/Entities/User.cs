// ישות משתמש/ת - הורה או שמרטפית.
// שדות: Id, Email (ייחודי), PasswordHash (Hash, לא הצפנה!), FullName, Phone, City, Role (UserRole).
// ניווטים: CreatedRequests - בקשות שההורה פרסם (1:N),
//          AssignedRequests - בקשות שהשמרטפית תפסה (1:N).
// ללא Attributes של EF - כל ההגדרות ב-Fluent API בשכבת Infrastructure.
