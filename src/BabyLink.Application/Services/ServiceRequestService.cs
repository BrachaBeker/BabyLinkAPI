// מימוש IServiceRequestService - הלב של המערכת.
// תלויות: IServiceRequestRepository, ITagRepository, IMapper, ILogger.
//
// ולידציה עסקית: End > Start, Start בעתיד, משך מקסימלי (למשל 24 שעות),
// רק ההורה שיצר יכול לערוך/למחוק (אחרת ForbiddenException).
//
// AssignAsync (תפיסת בקשה):
//   1. טעינה עם Tracking (GetForUpdateAsync). לא קיימת -> NotFoundException (404).
//   2. כבר משובצת לשמרטפית הזו -> מחזירים הצלחה (פעולה אידמפוטנטית - לחיצה כפולה לא נכשלת).
//   3. משובצת לשמרטפית אחרת -> ConflictException (409).
//   4. מועד ההתחלה עבר -> BusinessRuleException (400).
//   5. לשמרטפית יש שיבוץ חופף בזמן -> ConflictException (409).
//   6. AssignedBabysitterId = babysitterId, AssignedAtUtc = עכשיו -> SaveChangesAsync.
//      שמירה נכשלה בגלל Version -> ConcurrencyConflictException (409) + לוג Warning.
//
// מקרי קצה (מפורטים גם ב-README):
//   א. שתי שמרטפיות תופסות באותו רגע -> שתיהן עוברות את שלב 3, ה-Version תופס את השנייה -> 409.
//   ב. הורה עורך בזמן ששמרטפית תופסת -> שני UPDATE על אותה שורה, השני נכשל ב-Version -> 409.
//   ג. הורה מוחק בזמן ששמרטפית תופסת -> גם ה-DELETE בודק Version; מי שמאחר מקבל 409.
//   ד. אותה שמרטפית תופסת שתי בקשות חופפות באותו רגע -> אלה שתי שורות שונות, ולכן ה-Version
//      לא מזהה את זה ושלב 5 עלול לעבור בשתיהן. מגבלה ידועה ומתועדת (שמרטפית אחת, שתי לשוניות,
//      מילישניות - נדיר מאוד). פתרון אפשרי: Concurrency Token גם על User שמתעדכן בכל שיבוץ.
//   ה. עריכה של בקשה שכבר משובצת -> ConflictException (409): השיבוץ נקבע לפי הפרטים המקוריים.
//   ו. מחיקה של בקשה משובצת -> מותרת (ההורה מבטל ומפרסם מחדש). השמרטפית תראה שהבקשה נעלמה.
