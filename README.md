# BabyLink – תיווך בין הורים לשמרטפיות

BabyLink היא מערכת פשוטה לחיבור בין הורים לשמרטפיות. הורה מפרסם בקשה (מועד, כתובת, מספר ילדים, תעריף לשעה של 20 ש"ח לפחות, ותגיות כמו "תינוק" או "עזרה בשיעורים"). שמרטפיות רואות את הבקשות הפנויות, מסננות לפי עיר, רחוב, זמן, תגית ותעריף, ו**הראשונה שתופסת בקשה מקבלת אותה**.

> 🔗 שרת: https://babylink-api.onrender.com · Swagger: https://babylink-api.onrender.com/swagger · לקוח: https://babylinkclient.onrender.com · Repository הלקוח: https://github.com/BrachaBeker/BabyLinkClient

---

## המשאב המוגבל והטיפול בתחרות

**המשאב:** בקשת שמרטפות (`ServiceRequest`). כל בקשה יכולה להיות משובצת לשמרטפית **אחת** בלבד.

**איפה התחרות:** שתי שמרטפיות לוחצות "אני לוקחת" על אותה בקשה בהפרש של מילישניות (`PUT /api/service-requests/{id}/assignment`). שתיהן קוראות את הבקשה כשהיא עדיין פנויה, ושתיהן עוברות את הבדיקה "האם פנויה?". בלי הגנה, שתיהן היו מקבלות אישור.

**איך המערכת מגיבה (Optimistic Concurrency):**
- לישות `ServiceRequest` יש שדה `Version` (Guid) שמוגדר כ-Concurrency Token ומתחלף בכל שמירה (בדריסה של `SaveChangesAsync` ב-`AppDbContext`, כולל שינוי של התגיות בלבד).
- EF Core מוסיף את הערך הישן של `Version` לתנאי ה-`WHERE`:
  ```sql
  UPDATE "ServiceRequests" SET "AssignedBabysitterId" = @sitter, ..., "Version" = @new
  WHERE "Id" = @id AND "Version" = @old;
  ```
  השמירה הראשונה מצליחה ומחליפה את ה-Version, ולכן השמירה השנייה לא מוצאת שורה לעדכן ו-EF זורק `DbUpdateConcurrencyException`.
- ה-Repository (בשכבת Infrastructure) תופס את החריגה **הספציפית** הזו ומתרגם אותה ל-`ConcurrencyConflictException`. כך שכבת Application לא תלויה ב-EF Core.
- ה-Middleware הגלובלי מחזיר **409 Conflict** עם הודעה ברורה וכותב לוג ברמת **Warning**.
- הלקוח מציג את ההודעה ומרענן את הרשימה.

**למה הבדיקה העסקית לבדה לא מספיקה:** היא רצה בזיכרון, אחרי הקריאה ולפני השמירה. שתי שמרטפיות שקראו באותו רגע עוברות אותה שתיהן. רק ה-`WHERE "Version" = @old` בתוך ה-UPDATE בודק את המצב ב-DB ברגע השמירה. בבדיקת עומס ידנית (15 סבבים, 8 שמרטפיות במקביל בכל סבב) כל סבב הסתיים בדיוק ב-200 אחד, ו-94 מתוך 105 תשובות ה-409 נתפסו רק בזכות ה-Version.

**ההוכחה:** בדיקת האינטגרציה `tests/BabyLink.Tests/Integration/ServiceRequestConcurrencyTests.cs` פותחת שני `DbContext` נפרדים מול אותו DB (SQLite in-memory). שניהם טוענים את אותה בקשה, הראשון שומר בהצלחה, השני מקבל `DbUpdateConcurrencyException`, ובסוף נבדק שב-DB נשמרה השמרטפית הראשונה בלבד.

### מקרי קצה

| # | מצב | תוצאה |
|---|---|---|
| א | שתי שמרטפיות תופסות את אותה בקשה באותו רגע | הראשונה 200, השנייה **409** (Version) |
| ב | שמרטפית תופסת בקשה שכבר תפוסה (מסך לא מעודכן) | **409** (בדיקה עסקית ב-Service) |
| ג | אותה שמרטפית לוחצת פעמיים על אותה בקשה | 200 בשתי הפעמים (פעולה אידמפוטנטית) |
| ד | הורה עורך או מוחק בקשה בדיוק כששמרטפית תופסת אותה | מי שמאחר מקבל **409** (Version נבדק גם ב-UPDATE וגם ב-DELETE) |
| ה | עריכה של בקשה שכבר משובצת | **409**: השיבוץ נקבע לפי הפרטים המקוריים. מחיקה שלה מותרת (204) |
| ו | תפיסת בקשה שמועד ההתחלה שלה עבר | **400** |
| ז | שמרטפית תופסת בקשה שחופפת בזמן לשיבוץ קיים שלה | **409** |
| ח | **מגבלה ידועה:** אותה שמרטפית תופסת **שתי בקשות שונות וחופפות** באותה מילישנייה (שתי לשוניות) | אלה שתי שורות שונות, ולכן ה-Version של כל בקשה לא מזהה את ההתנגשות. המצב נדיר מאוד. פתרון אפשרי: Concurrency Token גם על המשתמש, שמתעדכן בכל שיבוץ |

**ביטולים:** כדי לשמור על פשטות אין "ביטול שיבוץ". אם הורה או שמרטפית מבטלים, ההורה מוחק את הבקשה ומפרסם בקשה חדשה.

---

## ארכיטקטורה (Clean Architecture)

```
src/BabyLink.Domain          ישויות ו-Enums. אפס תלויות, אפס חבילות
src/BabyLink.Application     לוגיקה עסקית, ממשקים, DTOs, AutoMapper, חריגות
src/BabyLink.Infrastructure  EF Core + PostgreSQL, Repositories, Migrations, Seed, JWT, Hash לסיסמאות
src/BabyLink.Api             Controllers, Middleware, Program.cs, NLog, Swagger
tests/BabyLink.Tests         Unit (xUnit + Moq) + Integration (שני DbContext)
```

**כיוון התלויות:** `Api → Application → Domain`, `Infrastructure → Application + Domain`. ה-Api מכיר את Infrastructure רק לצורך רישום שירותים ב-`Program.cs`. הממשקים (`IServiceRequestRepository` וכו') מוגדרים ב-Application וממומשים ב-Infrastructure (Dependency Inversion).

**ישויות:** `User` (הורה או שמרטפית), `ServiceRequest` (המשאב המוגבל), `Tag`. כל ההגדרות ב-Fluent API, בלי Attributes של EF ב-Domain.
- One-to-Many: הורה → הבקשות שפרסם (Cascade), שמרטפית → הבקשות שתפסה (Restrict).
- Many-to-Many: `ServiceRequest` ↔ `Tag`, דרך טבלת הקישור `ServiceRequestTags`.
- **Migrations:** `Init` (משתמשים ובקשות), `AddTags` (תגיות וטבלת הקישור) ו-`RenamePetsTagToSleepWatch` (Migration של נתונים בלבד: התגית "בעלי חיים בבית" הוחלפה ב"שמירה בזמן שינה").

**Middleware Pipeline:** `ExceptionHandling → CorrelationId → RequestLogging → Swagger → CORS → Authentication → Authorization → Controllers`

**שגיאות:** כל שגיאה חוזרת כ-JSON אחיד (ProblemDetails) עם `correlationId`:
`NotFound → 404`, `Forbidden → 403`, `BusinessRule → 400`, `InvalidCredentials → 401`, `Conflict / ConcurrencyConflict → 409`, כל השאר → `500` בלי פרטים פנימיים.

**לוגים (NLog):** קובץ יומי (`logs/babylink-DATE.log`) וקונסול. כל בקשה נרשמת ב-Information עם CorrelationId (מה-Header `X-Correlation-ID` או חדש), חריגות ב-Error והתנגשויות ב-Warning. סיסמאות, tokens ותוכן בקשות לא נכתבים ללוג.

---

## הרצה מקומית

**דרישות:** [.NET 10 SDK](https://dotnet.microsoft.com/download) ו-PostgreSQL. הדרך הקלה להרים PostgreSQL היא Docker:

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

ה-Migrations וה-Seed רצים אוטומטית בעליית השרת, ואין צורך בשום פעולה ידנית ב-DB. Swagger נפתח ב-`http://localhost:5083/swagger`: מתחברים ב-`POST /api/tokens`, מעתיקים את ה-`token`, לוחצים **Authorize** ומדביקים אותו.

**בדיקות:**

```bash
dotnet test
```

14 בדיקות: 9 Unit Tests ל-`ServiceRequestService` (כולל המקרה התקין והמקרה הנדחה של תפיסת בקשה), 3 ל-`AuthService`, ו-2 בדיקות אינטגרציה עם שני `DbContext`. בדיקות האינטגרציה רצות על SQLite in-memory, ולכן לא צריך PostgreSQL כדי להריץ אותן.

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

ה-Seed יוצר גם 5 תגיות (תינוק, עזרה בשיעורים, שמירה בזמן שינה, משמרת לילה, צרכים מיוחדים) ו-10 בקשות עתידיות בירושלים, בני ברק, תל אביב וחיפה (3 מהן כבר משובצות).

**הדגמת התחרות:** פותחים שני דפדפנים (או חלון רגיל וחלון גלישה בסתר), מתחברים כ-`sitter1` וכ-`sitter2`, נכנסים לאותה בקשה פנויה ולוחצים "אני לוקחת" כמעט יחד. אחת מקבלת אישור, והשנייה מקבלת הודעה שהבקשה נתפסה.

---

## Endpoints

| Method | URL | הרשאה | תשובות |
|---|---|---|---|
| POST | `/api/users` | ללא (הרשמה) | 201 + Location, 400, 409 |
| GET | `/api/users/{id}` | המשתמש עצמו בלבד | 200, 401, 403, 404 |
| POST | `/api/tokens` | ללא (התחברות) | 200, 400, 401 |
| GET | `/api/service-requests` | מחובר | 200, 400, 401 |
| GET | `/api/service-requests/{id}` | מחובר | 200, 401, 404 |
| POST | `/api/service-requests` | Parent | 201 + Location, 400, 401, 403 |
| PUT | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 400, 401, 403, 404, 409 |
| DELETE | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 401, 403, 404, 409 |
| PUT | `/api/service-requests/{id}/assignment` | Babysitter | 200, 400, 401, 403, 404, **409** |
| GET | `/api/tags` | מחובר | 200, 401 |

אין שמות פעולות ב-URL: התחברות היא "יצירת token" (`POST /api/tokens`), ותפיסה היא "עדכון השיבוץ" (`PUT .../assignment`).

**ולידציה של בקשה:** תעריף לשעה בין 20 ל-1000 ש"ח, 1 עד 10 ילדים, סיום אחרי התחלה, התחלה בעתיד, ומשך של עד 24 שעות.

**נראות ופרטיות:**
- **שמרטפית** רואה ברשימה רק בקשות פנויות ובקשות שהיא עצמה תפסה. בקשות שתפסה שמרטפית אחרת לא מופיעות לה.
- **מי תפסה את הבקשה** (שם, טלפון, מועד התפיסה) מוצג רק להורה שפרסם ולשמרטפית שתפסה. כל משתמש אחר מקבל `isAssigned: true` והשדות האלה ריקים (`null`).
- **הורה** רואה את כל הבקשות.

**סינון, מיון ו-Pagination ב-`GET /api/service-requests`** (הכול מתבצע בתוך שאילתת ה-SQL, כולל `OFFSET/LIMIT`):

| פרמטר | תיאור |
|---|---|
| `city` | עיר (התאמה מלאה, בלי תלות ברישיות) |
| `street` | רחוב (חיפוש חלקי: "הרצל" ימצא גם "שדרות הרצל") |
| `fromUtc`, `toUtc` | טווח זמן להתחלת הבקשה |
| `tagId` | תגית |
| `minHourlyRate` | תעריף מינימלי לשעה |
| `onlyAvailable` | רק בקשות פנויות שעוד לא התחילו |
| `mine` | הבקשות שלי (שפרסמתי או שתפסתי) |
| `includePast` | גם בקשות שמועד ההתחלה שלהן עבר. ברירת המחדל (`false`): רק בקשות עתידיות, גם ב-`mine` |
| `sortBy`, `descending` | מיון לפי `startTime` (ברירת מחדל) או `hourlyRate` |
| `page`, `pageSize` | עמוד (ברירת מחדל 1) וגודל עמוד (ברירת מחדל 10, מקסימום 50) |

דוגמה: `GET /api/service-requests?city=ירושלים&street=הרצל&onlyAvailable=true&page=1&pageSize=10`

---

## פריסה (Render)

**1. מסד נתונים:** ב-Render ← **New ← Postgres**. אחרי היצירה מעתיקים את ה-**Internal Database URL**.

**2. שרת:** **New ← Web Service** ← מחברים את ה-Repository הזה ← **Language: Docker** (Render בונה לפי ה-`Dockerfile` שבשורש).

**3. משתני סביבה (Environment) של ה-Web Service:**

| משתנה | ערך |
|---|---|
| `ConnectionStrings__Default` | ה-Internal Database URL (`postgresql://...`). השרת ממיר אותו אוטומטית לפורמט של Npgsql |
| `Jwt__Key` | מחרוזת אקראית של 32 תווים לפחות (שונה מזו של הפיתוח) |
| `AutoMapper__LicenseKey` | המפתח החינמי (אופציונלי) |
| `Cors__AllowedOrigins__0` | כתובת הלקוח, למשל `https://babylink-client.onrender.com` |

אין צורך להגדיר פורט: Render מעביר אותו במשתנה `PORT`, והשרת מאזין לו. ה-Migrations וה-Seed רצים בעלייה הראשונה, כך שהמערכת מוכנה מיד עם משתמשי הדמו.

**4. לקוח:** Static Site נפרד, מתוך ה-Repository של הלקוח.

> בתוכנית החינמית של Render השרת "נרדם" אחרי זמן ללא שימוש, והבקשה הראשונה אחרי זה לוקחת כחצי דקה.
