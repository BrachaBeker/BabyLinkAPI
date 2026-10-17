# BabyLink – תיווך בין הורים לשמרטפיות

BabyLink היא מערכת פשוטה לחיבור בין הורים לשמרטפיות. הורה מפרסם בקשה (מועד, כתובת, מספר ילדים, תעריף לשעה ותגיות כמו "תינוק" או "עזרה בשיעורים"). שמרטפיות רואות את כל הבקשות, מסננות לפי עיר, רחוב, זמן, תגית ותעריף, ו**הראשונה שתופסת בקשה מקבלת אותה**.

> 🔗 שרת: `<קישור ל-Render>` · Swagger: `<קישור>/swagger` · לקוח: `<קישור ל-Render>` · Repository הלקוח: `<קישור ל-GitHub>`

---

## המשאב המוגבל והטיפול בתחרות

**המשאב:** בקשת שמרטפות (`ServiceRequest`). כל בקשה יכולה להיות משובצת לשמרטפית **אחת** בלבד.

**איפה התחרות:** שתי שמרטפיות לוחצות "אני לוקחת" על אותה בקשה בהפרש של מילישניות (`PUT /api/service-requests/{id}/assignment`). שתיהן קוראות את הבקשה כשהיא עדיין פנויה, ושתיהן עוברות את הבדיקה "האם פנויה?". בלי הגנה, שתיהן היו מקבלות אישור.

**איך המערכת מגיבה (Optimistic Concurrency):**
- לישות `ServiceRequest` יש שדה `Version` (Guid) שמוגדר כ-Concurrency Token ומתחלף בכל שמירה (בדריסה של `SaveChangesAsync` ב-`AppDbContext`).
- EF Core מוסיף את הערך הישן של `Version` לתנאי ה-`WHERE` של ה-`UPDATE`. השמירה הראשונה מצליחה ומחליפה את ה-Version, ולכן השמירה השנייה לא מוצאת שורה לעדכן ו-EF זורק `DbUpdateConcurrencyException`.
- ה-Repository (בשכבת Infrastructure) תופס את החריגה **הספציפית** הזו ומתרגם אותה ל-`ConcurrencyConflictException`. כך שכבת Application לא תלויה ב-EF Core.
- ה-Middleware הגלובלי מחזיר **409 Conflict** עם הודעה ברורה ("הבקשה נתפסה על ידי שמרטפית אחרת") וכותב לוג ברמת **Warning**.
- הלקוח מציג את ההודעה ומרענן את הרשימה.

**ההוכחה:** בדיקת האינטגרציה `tests/BabyLink.Tests/Integration/ServiceRequestConcurrencyTests.cs` פותחת שני `DbContext` נפרדים מול אותו DB. שניהם טוענים את אותה בקשה, הראשון שומר בהצלחה והשני מקבל `DbUpdateConcurrencyException`.

### מקרי קצה

| # | מצב | תוצאה |
|---|---|---|
| א | שתי שמרטפיות תופסות את אותה בקשה באותו רגע | הראשונה 200, השנייה **409** (Version) |
| ב | שמרטפית תופסת בקשה שכבר תפוסה (מסך לא מעודכן) | **409** (בדיקה עסקית ב-Service) |
| ג | אותה שמרטפית לוחצת פעמיים על אותה בקשה | 200 בשתי הפעמים (פעולה אידמפוטנטית) |
| ד | הורה עורך או מוחק בקשה בדיוק כששמרטפית תופסת אותה | מי שמאחר מקבל **409** (Version נבדק גם ב-UPDATE וגם ב-DELETE) |
| ה | עריכה של בקשה שכבר משובצת | **409**: השיבוץ נקבע לפי הפרטים המקוריים. אפשר למחוק ולפרסם מחדש |
| ו | תפיסת בקשה שמועד ההתחלה שלה עבר | **400** |
| ז | שמרטפית תופסת בקשה שחופפת בזמן לשיבוץ קיים שלה | **409** |
| ח | **מגבלה ידועה:** אותה שמרטפית תופסת **שתי בקשות שונות וחופפות** באותה מילישנייה (שתי לשוניות) | אלה שתי שורות שונות, ולכן ה-Version של כל בקשה לא מזהה את ההתנגשות. המצב נדיר מאוד. פתרון אפשרי: Concurrency Token גם על המשתמש, שמתעדכן בכל שיבוץ |

**ביטולים:** כדי לשמור על פשטות אין "ביטול שיבוץ". אם הורה או שמרטפית מבטלים, ההורה מוחק את הבקשה ומפרסם בקשה חדשה.

---

## ארכיטקטורה (Clean Architecture)

```
src/BabyLink.Domain          ישויות ו-Enums. אפס תלויות, אפס חבילות
src/BabyLink.Application     לוגיקה עסקית, ממשקים, DTOs, AutoMapper, חריגות
src/BabyLink.Infrastructure  EF Core + PostgreSQL, Repositories, Migrations, JWT, Hash לסיסמאות
src/BabyLink.Api             Controllers, Middleware, Program.cs, NLog
tests/BabyLink.Tests         Unit (xUnit + Moq) + Integration (שני DbContext)
```

**כיוון התלויות:** `Api → Application → Domain`, `Infrastructure → Application`. ה-Api מכיר את Infrastructure רק לצורך רישום שירותים ב-`Program.cs`.

**ישויות:** `User` (הורה או שמרטפית), `ServiceRequest` (המשאב המוגבל), `Tag`.
- One-to-Many: הורה → הבקשות שפרסם, שמרטפית → הבקשות שתפסה.
- Many-to-Many: `ServiceRequest` ↔ `Tag`.

**Middleware Pipeline:** `ExceptionHandling → CorrelationId → RequestLogging → CORS → Authentication → Authorization → Controllers`

**לוגים (NLog):** כל בקשה נכנסת נרשמת ב-Information עם CorrelationId, חריגות ב-Error והתנגשויות ב-Warning. סיסמאות, tokens ותוכן בקשות הרשמה לא נכתבים ללוג.

---

## הרצה מקומית

**דרישות:** [.NET 10 SDK](https://dotnet.microsoft.com/download) ו-PostgreSQL 16. הדרך הקלה להרים PostgreSQL היא Docker:

```bash
docker run --name babylink-pg -e POSTGRES_PASSWORD=<password> -p 5432:5432 -d postgres:16
```

**הגדרת סודות (User Secrets, פעם אחת):**

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=BabyLinkDb;Username=postgres;Password=<password>" --project src/BabyLink.Api
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית של 32 תווים לפחות>" --project src/BabyLink.Api
dotnet user-secrets set "AutoMapper:LicenseKey" "<מפתח Community חינמי>" --project src/BabyLink.Api   # אופציונלי
```

**הרצה:**

```bash
dotnet run --project src/BabyLink.Api
```

ה-Migrations וה-Seed רצים אוטומטית בעליית השרת, ואין צורך בשום פעולה ידנית ב-DB. Swagger זמין ב-`/swagger`.

**בדיקות:**

```bash
dotnet test
```

בדיקת האינטגרציה רצה על SQLite in-memory, ולכן לא צריך PostgreSQL כדי להריץ אותה.

**Migration חדש (לפיתוח):**

```bash
dotnet ef migrations add <Name> -p src/BabyLink.Infrastructure -s src/BabyLink.Api -o Persistence/Migrations
```

---

## משתמשי דמו

| Role | Email | Password |
|---|---|---|
| Parent | parent1@babylink.dev | Demo123! |
| Parent | parent2@babylink.dev | Demo123! |
| Babysitter | sitter1@babylink.dev | Demo123! |
| Babysitter | sitter2@babylink.dev | Demo123! |

**הדגמת התחרות:** פותחים שני דפדפנים (או חלון רגיל וחלון גלישה בסתר), מתחברים כ-`sitter1` וכ-`sitter2`, נכנסים לאותה בקשה פנויה ולוחצים "אני לוקחת" כמעט יחד. אחת מקבלת אישור, והשנייה מקבלת הודעה שהבקשה נתפסה.

---

## Endpoints

| Method | URL | הרשאה | תשובות |
|---|---|---|---|
| POST | `/api/users` | ללא (הרשמה) | 201, 400, 409 |
| POST | `/api/tokens` | ללא (התחברות) | 200, 400, 401 |
| GET | `/api/service-requests` | מחובר | 200, 401 |
| GET | `/api/service-requests/{id}` | מחובר | 200, 401, 404 |
| POST | `/api/service-requests` | Parent | 201, 400, 401, 403 |
| PUT | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 400, 401, 403, 404, 409 |
| DELETE | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 401, 403, 404, 409 |
| PUT | `/api/service-requests/{id}/assignment` | Babysitter | 200, 400, 401, 403, 404, **409** |
| GET | `/api/tags` | מחובר | 200, 401 |

**סינון, מיון ו-Pagination ב-`GET /api/service-requests`:**

| פרמטר | תיאור |
|---|---|
| `city` | עיר (התאמה מלאה) |
| `street` | רחוב (חיפוש חלקי: "הרצל" ימצא גם "שדרות הרצל") |
| `fromUtc`, `toUtc` | טווח זמן |
| `tagId` | תגית |
| `minHourlyRate` | תעריף מינימלי לשעה |
| `onlyAvailable` | רק בקשות פנויות |
| `mine` | הבקשות שלי (שפרסמתי או שתפסתי) |
| `sortBy`, `descending` | מיון לפי `startTime` או `hourlyRate` |
| `page`, `pageSize` | עמוד (ברירת מחדל 1) וגודל עמוד (ברירת מחדל 10, מקסימום 50) |

דוגמה: `GET /api/service-requests?city=ירושלים&street=הרצל&onlyAvailable=true&page=1&pageSize=10`

---

## פריסה (Render)

- **שרת:** Web Service מסוג Docker (לפי ה-`Dockerfile` בשורש), מחובר ל-PostgreSQL של Render.
- **משתני סביבה ב-Render:** `ConnectionStrings__Default`, `Jwt__Key`, `AutoMapper__LicenseKey`, `Cors__AllowedOrigins__0`.
- **לקוח:** Static Site נפרד, מתוך ה-Repository של הלקוח.
