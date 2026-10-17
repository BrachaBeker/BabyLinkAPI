תיקיית Migrations - הקבצים נוצרים אוטומטית ע"י:
dotnet ef migrations add <Name> -p src/BabyLink.Infrastructure -s src/BabyLink.Api -o Persistence/Migrations
דרישה: לפחות 2 migrations בהיסטוריה (למשל Init ואחר כך AddTags).
(את הקובץ הזה אפשר למחוק אחרי ה-migration הראשון.)
