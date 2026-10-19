# BabyLink – תיווך בין הורים לשמרטפיות

Web API ב-.NET 10: הורה מפרסם בקשת שמרטפות (מועד, כתובת, ילדים, תעריף ותגיות), שמרטפיות מסננות ו**הראשונה שתופסת זוכה**.
🔗 [שרת](https://babylink-api.onrender.com) · [Swagger](https://babylink-api.onrender.com/swagger) · [לקוח](https://babylinkclient.onrender.com) · [Repository הלקוח](https://github.com/BrachaBeker/BabyLinkClient)

## המשאב המוגבל: תפיסת בקשה (Optimistic Concurrency)
- שתי שמרטפיות שלוחצות יחד על `PUT /api/service-requests/{id}/assignment` עוברות שתיהן את הבדיקה "האם פנויה?", כי היא רצה בזיכרון.
- לכן ל-`ServiceRequest` יש `Version` (Guid, Concurrency Token) שמתחלף בכל שמירה, ו-EF מעדכן רק `WHERE "Version" = @old`. השנייה מעדכנת 0 שורות ← `DbUpdateConcurrencyException`.
- ה-Repository מתרגם אותה ל-`ConcurrencyConflictException` (כך Application לא מכירה את EF), וה-Middleware מחזיר **409** ורושם Warning.
- **הוכחה:** בדיקת אינטגרציה עם שני `DbContext` נפרדים (`tests/.../ServiceRequestConcurrencyTests.cs`). בבדיקת עומס (15 סבבים × 8 שמרטפיות) כל סבב נגמר ב-200 אחד בלבד.

| מקרה קצה | תוצאה |
|---|---|
| תפיסה במקביל / בקשה שכבר תפוסה / לחיצה כפולה של אותה שמרטפית | 409 (Version) / 409 / 200 |
| הורה עורך או מוחק בדיוק כשהיא תופסת · שמרטפית מבטלת כשההורה מוחק | מי שמאחר מקבל 409 |
| עריכת בקשה משובצת / מחיקתה | 409 / 204, והשמרטפית מקבלת הודעה |
| מועד עבר / שיבוץ חופף בזמן | 400 / 409 |
| ביטול שיבוץ ע"י השמרטפית: פחות מ-24 שעות לפני / של אחרת / שכבר בוטל | 400 / 403 / 409 |
| **מגבלה ידועה:** אותה שמרטפית תופסת שתי בקשות חופפות באותה מילישנייה | לא מזוהה (שתי שורות שונות). פתרון: Token גם על User |

## ארכיטקטורה
- **Clean Architecture:** `Domain` (ישויות, בלי תלויות), `Application` (Services, ממשקים, DTOs, AutoMapper), `Infrastructure` (EF Core + PostgreSQL, Repositories, JWT), `Api` (Controllers, Middleware, NLog). תלויות: `Api → Application → Domain`, `Infrastructure → Application` (מממשת את הממשקים שלה).
- **ישויות:** `User`, `ServiceRequest`, `Tag`, `Notification`. קשרי 1:N ו-M:N (`ServiceRequestTags`) ב-Fluent API. 5 Migrations, מ-`Init` ו-`AddTags` ועד `NotificationActorAndRequest`.
- **Pipeline:** `ExceptionHandling → CorrelationId → RequestLogging → Swagger → CORS → Authentication → Authorization`.
- **שגיאות:** ProblemDetails עם `correlationId`. **לוגים:** כל בקשה עם CorrelationId, Error לחריגות, Warning ל-409, בלי סיסמאות ו-tokens.

## Endpoints
| | |
|---|---|
| `POST /api/users` (הרשמה) · `POST /api/tokens` (התחברות) · `GET/PUT /api/users/{id}` (רק המשתמש עצמו) | ללא / ללא / מחובר |
| `GET /api/service-requests` (סינון: `city`, `street`, `fromUtc`, `toUtc`, `tagId`, `minHourlyRate`, `onlyAvailable`, `mine`, `status`; מיון; Pagination עד 50) · `GET .../{id}` | מחובר (הורה רואה רק את שלו) |
| `POST`, `PUT`, `DELETE /api/service-requests/{id}` | הורה (היוצר) |
| `PUT` / `DELETE /api/service-requests/{id}/assignment` (תפיסה / ביטול שיבוץ) | שמרטפית |
| `GET /api/tags` · `GET` / `DELETE /api/notifications[/{id}]` | מחובר |

**ולידציה:** תעריף 20–1000, 1–10 ילדים, התחלה בעתיד, עד 24 שעות. להורה חובה כתובת. **פרטיות:** זהות השמרטפית שתפסה גלויה רק להורה שפרסם ולה.

## הרצה מקומית
```bash
docker run --name babylink-pg -e POSTGRES_PASSWORD=<password> -p 5432:5432 -d postgres:16
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=BabyLinkDb;Username=postgres;Password=<password>" --project src/BabyLink.Api
dotnet user-secrets set "Jwt:Key" "<32+ תווים אקראיים>" --project src/BabyLink.Api
dotnet run --project src/BabyLink.Api      # Migrations ו-Seed רצים אוטומטית; Swagger ב-http://localhost:5083/swagger
dotnet test                                # 32 בדיקות (Moq + שני DbContext על SQLite in-memory)
```
**משתמשי דמו** (סיסמה `Demo123!`): `parent1@babylink.dev`, `parent2@babylink.dev`, `sitter1@babylink.dev`, `sitter2@babylink.dev`.

## פריסה (Render)
Postgres + Web Service מסוג **Docker**. משתני סביבה: `ConnectionStrings__Default` (ה-Internal URL, מומר אוטומטית), `Jwt__Key`, `AutoMapper__LicenseKey`, `Cors__AllowedOrigins__0` (כתובת הלקוח). השרת החינמי "נרדם", והבקשה הראשונה איטית.
