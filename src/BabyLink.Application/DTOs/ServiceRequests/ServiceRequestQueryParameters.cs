// פרמטרים לרשימה (מגיעים מ-[FromQuery]):
// סינון: City (התאמה מלאה, בלי תלות ברישיות), Street (חיפוש חלקי - "הרצל" ימצא "שדרות הרצל"),
//        FromUtc, ToUtc, TagId, MinHourlyRate, OnlyAvailable, Mine (הבקשות שלי).
// מיון: SortBy (startTime / hourlyRate), Descending.
// Pagination: Page (ברירת מחדל 1), PageSize (ברירת מחדל 10, מקסימום 50 - ערך גדול יותר יוגבל ל-50).
// דוגמה: GET /api/service-requests?city=ירושלים&street=הרצל&onlyAvailable=true&page=1
