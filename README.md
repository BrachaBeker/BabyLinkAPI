# BabyLink – תיווך בין הורים לשמרטפיות

Web API ב-ASP.NET Core (.NET 10) עם PostgreSQL ו-EF Core. **הורה** מפרסם בקשת שמרטפות (מועד, כתובת, ילדים, תעריף ותגיות) ויכול לבטל אותה. **שמרטפית** מסננת בקשות ותופסת אחת, ו**הראשונה שתופסת זוכה**. על כל תפיסה וביטול של שיבוץ הצד השני מקבל הודעה.
🔗 [שרת](https://babylink-api.onrender.com) · [Swagger](https://babylink-api.onrender.com/swagger) · [לקוח](https://babylinkclient.onrender.com) · [Repository הלקוח](https://github.com/BrachaBeker/BabyLinkClient)

## המשאב המוגבל והתחרות עליו
- **המשאב:** בקשת שמרטפות (`ServiceRequest`). אפשר לשבץ לה שמרטפית **אחת** בלבד.
- **התחרות:** שתי שמרטפיות לוחצות "אני לוקחת" (`PUT /api/service-requests/{id}/assignment`) באותו רגע. שתיהן קוראות "פנויה" ועוברות את הבדיקה, כי היא רצה בזיכרון.
- **הפתרון (Optimistic Concurrency):** ל-`ServiceRequest` יש `Version` (Guid) שמוגדר כ-Concurrency Token ב-Fluent API ומתחלף בכל שמירה בדריסת `SaveChangesAsync` של ה-DbContext. EF מעדכן רק `WHERE "Version" = @old`, ולכן השמירה השנייה מעדכנת 0 שורות ← `DbUpdateConcurrencyException`.
- **התגובה:** ה-Repository תופס רק את החריגה הזו ומתרגם אותה ל-`ConcurrencyConflictException` (כך Application לא מכירה את EF). ה-Middleware מחזיר **409** עם הודעה ברורה ורושם Warning, והלקוח מציג "הבקשה כבר נתפסה" ומרענן.
- **הוכחה:** בדיקת אינטגרציה (`tests/.../ServiceRequestConcurrencyTests.cs`): שני `DbContext` נפרדים טוענים את אותה בקשה, הראשון שומר והשני מקבל `DbUpdateConcurrencyException`. בבדיקת עומס (15 סבבים × 8 שמרטפיות) כל סבב נגמר ב-200 אחד בלבד.

| מקרה קצה | תוצאה |
|---|---|
| תפיסה במקביל / בקשה שכבר תפוסה / לחיצה כפולה של אותה שמרטפית | 409 (Version) / 409 / 200 |
| הורה עורך או מוחק בדיוק כשהיא תופסת · שמרטפית מבטלת כשההורה מוחק | מי שמאחר מקבל 409 |
| עריכת בקשה משובצת / מחיקתה | 409 / 204, והשמרטפית מקבלת הודעה |
| מועד עבר / שיבוץ חופף בזמן | 400 / 409 |
| ביטול שיבוץ ע"י השמרטפית: פחות מ-24 שעות לפני / של אחרת / שכבר בוטל | 400 / 403 / 409 |
| **מגבלה ידועה:** אותה שמרטפית תופסת שתי בקשות חופפות באותה מילישנייה | לא מזוהה (שתי שורות שונות). פתרון: Token גם על User |

## עמידה בדרישות
| נושא | מימוש |
|---|---|
| REST + Swagger | משאבים ברבים ופעולות ב-HTTP methods: `/api/users`, `/api/tokens` (התחברות), `/api/service-requests` (+ `/{id}/assignment`), `/api/tags`, `/api/notifications`. סטטוסים 200, 201 + Location, 204, 400, 401, 403, 404, 409. Swagger מתעד הכול, עם כפתור Authorize |
| Validation | `[FromRoute]`, `[FromQuery]`, `[FromBody]`. Data Annotations ב-DTOs (ו-`IValidatableObject` בהרשמה), ו-`[ApiController]` מחזיר 400 על ModelState לא תקין. כללים שתלויים בנתונים - ב-Service |
| שכבות | 4 פרויקטים ב-`BabyLink.slnx` (Clean Architecture): `Domain` (ישויות, אפס תלויות), `Application` (Services, ממשקים, DTOs, AutoMapper), `Infrastructure` (EF Core, Repositories, JWT), `Api` (Controllers, Middleware). Controllers עובדים רק מול Services |
| DI | הזרקה ב-constructor. ממשקים ב-Application, מימושים ב-Infrastructure, רישום ב-`Program.cs`. DbContext, Repositories ו-Services הם Scoped; Singleton שמחזיק Scoped היה משתף DbContext אחד בין בקשות |
| EF Core | Code-First, `DbSet` לכל ישות, 5 Migrations, Seed אוטומטי, Connection String מהקונפיגורציה. CRUD מלא; סינון, מיון ו-Pagination בתוך ה-SQL (`Skip`/`Take`), ו-`AsNoTracking` בקריאות |
| קשרים | 1:N (הורה ← בקשות, שמרטפית ← שיבוצים, משתמש ← הודעות) ו-M:N (בקשות ↔ תגיות) ב-Fluent API. `Include` טוען את הקשרים באותה שאילתה, בלי N+1. **השאילתות טוענות רק קשרים בדרגה אחת (בקשה ← הורה, שמרטפית, תגיות), ולכן `ThenInclude` (קשר של קשר) לא נדרש** |
| DTO + AutoMapper | ישויות לא נכנסות ולא יוצאות מה-API: DTOs נפרדים ל-request ול-response, ומיפוי ב-AutoMapper Profiles |
| Async | async מה-Controller ועד ה-DbContext, `CancellationToken` לאורך כל הדרך, בלי `.Result` ו-`.Wait()` |
| Middleware | `ExceptionHandling → CorrelationId → RequestLogging → Swagger → CORS → Authentication → Authorization`. כל שגיאה חוזרת כ-ProblemDetails עם `correlationId` |
| Configuration | `appsettings.json` + `appsettings.Development.json`, ו-`IOptions<JwtOptions>`. אין סודות בקוד וב-repo: User Secrets בפיתוח, משתני סביבה ב-Render |
| JWT | הרשמה (`POST /api/users`) והתחברות (`POST /api/tokens`), token עם claims `sub`, `email`, `name`, `role`. שני roles, `Parent` ו-`Babysitter`, נאכפים בשרת ב-`[Authorize(Roles = ...)]`, והבעלות נבדקת ב-Service (הורה רואה ועורך רק את שלו, אחרת 403) |
| NLog | `nlog.config` (NLog.Web.AspNetCore מתחת ל-`ILogger`): קובץ יומי וקונסול. כל בקשה ב-Information עם CorrelationId, חריגות ב-Error, התנגשויות ב-Warning, Debug רק בפיתוח. בלי סיסמאות ו-tokens |
| בדיקות | 32 בדיקות xUnit, בהן Unit Tests ל-Services עם Repositories מדומים ב-Moq (כולל תפיסה תקינה ותפיסה שנדחית) ו-3 בדיקות אינטגרציה עם שני `DbContext` על SQLite in-memory |

## הרצה מקומית
דרישות: .NET 10 SDK ו-**PostgreSQL** (למשל 16 ב-Docker). ה-Migrations וה-Seed רצים אוטומטית בעלייה, בלי שום פעולה ידנית ב-DB.
```bash
docker run --name babylink-pg -e POSTGRES_PASSWORD=<password> -p 5432:5432 -d postgres:16
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=BabyLinkDb;Username=postgres;Password=<password>" --project src/BabyLink.Api
dotnet user-secrets set "Jwt:Key" "<32+ תווים אקראיים>" --project src/BabyLink.Api
dotnet run --project src/BabyLink.Api      # Swagger: http://localhost:5083/swagger
dotnet test
```
**משתמשי דמו** (סיסמה `Demo123!`): הורה – `parent1@babylink.dev`, `parent2@babylink.dev` · שמרטפית – `sitter1@babylink.dev`, `sitter2@babylink.dev`.

**פריסה (Render):** Postgres + Web Service מסוג Docker. משתני סביבה: `ConnectionStrings__Default` (ה-Internal URL, מומר אוטומטית), `Jwt__Key`, `AutoMapper__LicenseKey`, `Cors__AllowedOrigins__0` (כתובת הלקוח). השרת החינמי "נרדם", והבקשה הראשונה איטית.
