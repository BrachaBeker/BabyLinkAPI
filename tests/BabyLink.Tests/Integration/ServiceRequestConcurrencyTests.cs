// בדיקת אינטגרציה - ההוכחה שה-Concurrency עובד (נבדקת בנפרד ע"י המורה!).
// שני AppDbContext נפרדים מול אותו DB (SQLite in-memory או PostgreSQL ב-Testcontainers):
// 1. שניהם טוענים את אותה בקשה פנויה.
// 2. ctx1 משבץ שמרטפית A ושומר - מצליח.
// 3. ctx2 משבץ שמרטפית B ושומר - Assert.ThrowsAsync<DbUpdateConcurrencyException>.
// 4. בדיקה שב-DB נשמרה שמרטפית A בלבד.
