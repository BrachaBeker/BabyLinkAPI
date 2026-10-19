# BabyLink – תיווך בין הורים לשמרטפיות

BabyLink היא מערכת פשוטה לחיבור בין הורים לשמרטפיות. הורה מפרסם בקשה (מועד, כתובת, מספר ילדים, תעריף לשעה של 20 ש"ח לפחות, ותגיות כמו "תינוק" או "עזרה בשיעורים"). שמרטפיות רואות את הבקשות, מסננות לפי עיר, רחוב, זמן, תגית ותעריף, ו**הראשונה שתופסת בקשה מקבלת אותה**. ההורה לא מחויב לשמרטפית שתפסה: הוא יכול לבטל את הבקשה ולפרסם אותה מחדש, והשמרטפית מקבלת הודעה על הביטול בכניסה הבאה שלה.

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
| ט | שמרטפית מבטלת את השיבוץ שלה בדיוק כששמרטפית אחרת מנסה לתפוס את הבקשה | אם האחרת קראה את הבקשה לפני שהביטול נשמר - **409** ("כבר נתפסה"), ושנייה אחר כך היא יכולה לנסות שוב. אחרי הביטול - היא תופסת. אף פעם לא שתיהן |
| י | שמרטפית מבטלת את השיבוץ בדיוק כשההורה מבטל או עורך את הבקשה | מי שמאחר מקבל **409** (Version). מוכח בבדיקת האינטגרציה `TwoContexts_UnassignAndDeleteSameRequest_...` |
| כ | ביטול שיבוץ שכבר בוטל (לחיצה כפולה), של שמרטפית אחרת, או אחרי המועד | **409** / **403** / **400** |

**ביטולים:**
- **ההורה** יכול לבטל (למחוק) בקשה בכל שלב, גם אחרי שנתפסה - הוא לא מחויב לשמרטפית שתפסה. השמרטפית מקבלת הודעה.
- **השמרטפית** יכולה לבטל את השיבוץ שלה עד מועד ההתחלה (`DELETE .../assignment`). הבקשה חוזרת להמתין לשמרטפית, וההורה מקבל הודעה.
- בכל שינוי כזה ההודעה לצד השני נשמרת **באותה `SaveChanges`** של השינוי עצמו - שניהם מצליחים או שניהם נכשלים.

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

**ישויות:** `User` (הורה או שמרטפית; להורה גם כתובת בית), `ServiceRequest` (המשאב המוגבל), `Tag`, `Notification` (הודעה למשתמש). כל ההגדרות ב-Fluent API, בלי Attributes של EF ב-Domain.
- One-to-Many: הורה → הבקשות שפרסם (Cascade), שמרטפית → הבקשות שתפסה (Restrict), משתמש → ההודעות שלו (Cascade), בקשה → ההודעות עליה (SetNull - הודעה נשארת גם אחרי שהבקשה נמחקה).
- Many-to-Many: `ServiceRequest` ↔ `Tag`, דרך טבלת הקישור `ServiceRequestTags`.
- **Migrations:** `Init` (משתמשים ובקשות), `AddTags` (תגיות וטבלת הקישור), `RenamePetsTagToSleepWatch` (Migration של נתונים בלבד: התגית "בעלי חיים בבית" הוחלפה ב"שמירה בזמן שינה") `AddUserAddressAndNotifications` (כתובת בית למשתמש וטבלת ההודעות) ו-`NotificationActorAndRequest` (`ParentFullName` הפך ל-`ActorFullName` ב-`RenameColumn`, כך שהנתונים נשמרים, ונוסף `RequestId` לקישור לבקשה).

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

29 בדיקות: 20 Unit Tests ל-`ServiceRequestService` (כולל המקרה התקין והמקרה הנדחה של תפיסת בקשה), 3 ל-`AuthService`, 3 לוולידציה של ההרשמה (`RegisterRequestDto`: כתובת חובה רק להורה), ו-3 בדיקות אינטגרציה עם שני `DbContext` (תפיסה כפולה, תפיסה דרך ה-Repository, וביטול שיבוץ מול מחיקה). בדיקות האינטגרציה רצות על SQLite in-memory, ולכן לא צריך PostgreSQL כדי להריץ אותן.

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

ה-Seed יוצר גם 5 תגיות (תינוק, עזרה בשיעורים, שמירה בזמן שינה, משמרת לילה, צרכים מיוחדים), כתובת בית להורי הדמו, 10 בקשות עתידיות בירושלים, בני ברק, תל אביב וחיפה (3 מהן כבר משובצות), 4 בקשות מהעבר (ל"היסטוריה" של ההורה), ושתי הודעות לדוגמה בפעמון: ביטול ל-`sitter1` ו"נמצאה שמרטפית!" ל-`parent1`.

**דמו חי:** בכל עלייה של השרת, אם נשארו פחות מ-3 בקשות פנויות עתידיות (למשל כי בקשות ה-Seed כבר עברו), נוסף סבב בקשות חדש להורי הדמו. כך מי שנכנס לאתר תמיד מוצא בקשות לתפוס.

**הדגמת התחרות:** פותחים שני דפדפנים (או חלון רגיל וחלון גלישה בסתר), מתחברים כ-`sitter1` וכ-`sitter2`, נכנסים לאותה בקשה פנויה ולוחצים "אני לוקחת" כמעט יחד. אחת מקבלת אישור, והשנייה מקבלת הודעה שהבקשה נתפסה.

---

## Endpoints

| Method | URL | הרשאה | תשובות |
|---|---|---|---|
| POST | `/api/users` | ללא (הרשמה) | 201 + Location, 400, 409 |
| GET | `/api/users/{id}` | המשתמש עצמו בלבד | 200, 401, 403, 404 |
| PUT | `/api/users/{id}` | המשתמש עצמו בלבד (עדכון פרופיל) | 200, 400, 401, 403, 404 |
| POST | `/api/tokens` | ללא (התחברות) | 200, 400, 401 |
| GET | `/api/service-requests` | מחובר (הורה - רק הבקשות שלו) | 200, 400, 401 |
| GET | `/api/service-requests/{id}` | מחובר (הורה - רק בקשה שלו) | 200, 401, 403, 404 |
| POST | `/api/service-requests` | Parent | 201 + Location, 400, 401, 403 |
| PUT | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 400, 401, 403, 404, 409 |
| DELETE | `/api/service-requests/{id}` | Parent (היוצר בלבד) | 204, 401, 403, 404, 409 |
| PUT | `/api/service-requests/{id}/assignment` | Babysitter | 200, 400, 401, 403, 404, **409** |
| DELETE | `/api/service-requests/{id}/assignment` | Babysitter (זו שתפסה - ביטול השיבוץ) | 204, 400, 401, 403, 404, **409** |
| GET | `/api/tags` | מחובר | 200, 401 |
| GET | `/api/notifications` | מחובר - ההודעות שלו | 200, 401 |
| DELETE | `/api/notifications/{id}` | מחובר - הודעה שלו ("הבנתי") | 204, 401, 403, 404 |
| DELETE | `/api/notifications` | מחובר - כל ההודעות שלו | 204, 401 |

אין שמות פעולות ב-URL: התחברות היא "יצירת token" (`POST /api/tokens`), תפיסה היא "עדכון השיבוץ" (`PUT .../assignment`), ביטול שיבוץ הוא מחיקה שלו (`DELETE .../assignment`), וסגירת הודעה היא מחיקה שלה.

**ולידציה:**
- **בקשה:** תעריף לשעה בין 20 ל-1000 ש"ח, 1 עד 10 ילדים, סיום אחרי התחלה, התחלה בעתיד, ומשך של עד 24 שעות.
- **הרשמה:** להורה חובה כתובת בית (רחוב ומספר בית - `IValidatableObject` ב-DTO, כי זה תלוי בתפקיד). ממנה ממולאת מראש הכתובת בבקשה חדשה. לשמרטפית הכתובת לא נשמרת.

**נראות ופרטיות:**
- **הורה** רואה רק את הבקשות שהוא פרסם - ברשימה (השרת מכריח `mine`) ובפרטים (בקשה של הורה אחר -> 403).
- **שמרטפית** רואה את כל הבקשות לפי הסינון. פריט ברשימה לא כולל שום פרט על השמרטפית שתפסה.
- **זהות השמרטפית שתפסה** (`assignedBabysitterId`, שם וטלפון) מוצגת רק להורה שפרסם ולשמרטפית שתפסה. כל משתמש אחר מקבל את השדות האלה ריקים (`null`), ורואה רק פרטים לא מזהים: `isAssigned: true` ומועד התפיסה (`assignedAtUtc`).

**הודעות (`Notification`):** נוצרות בשרת, באותה `SaveChanges` של הפעולה שגרמה להן, ומוצגות בפעמון של הלקוח:

| סוג | מתי | למי |
|---|---|---|
| `RequestAssigned` | שמרטפית תפסה בקשה ("נמצאה שמרטפית!") | להורה, עם קישור לבקשה |
| `AssignmentCancelled` | השמרטפית ביטלה את השיבוץ, והבקשה חזרה להמתין | להורה, עם קישור לבקשה |
| `RequestCancelled` | ההורה ביטל בקשה שהשמרטפית כבר תפסה | לשמרטפית (בלי קישור - הבקשה נמחקה) |

כל הודעה שומרת את פרטי הבקשה ואת שם מי שביצע את הפעולה (`actorFullName`), כך שהיא נשארת ברורה גם אחרי שהבקשה נמחקה.

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
| `status` | `open` (ממתינה לשמרטפית), `assigned` (שובצה ועוד לא התחילה) או `past` (מועדה עבר). קובע גם את טווח הזמן |
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
| `Cors__AllowedOrigins__0` | כתובת הלקוח, בלי `/` בסוף: `https://babylinkclient.onrender.com` |

אין צורך להגדיר פורט: Render מעביר אותו במשתנה `PORT`, והשרת מאזין לו. ה-Migrations וה-Seed רצים בעלייה הראשונה, כך שהמערכת מוכנה מיד עם משתמשי הדמו.

**4. לקוח:** Static Site נפרד, מתוך ה-Repository של הלקוח.

> בתוכנית החינמית של Render השרת "נרדם" אחרי זמן ללא שימוש, והבקשה הראשונה אחרי זה לוקחת כחצי דקה.
