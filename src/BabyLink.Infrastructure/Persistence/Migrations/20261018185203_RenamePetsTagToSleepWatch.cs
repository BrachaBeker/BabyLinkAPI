using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabyLink.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migration של נתונים בלבד (המודל לא משתנה): התגית "בעלי חיים בבית" הופכת ל"שמירה בזמן שינה".
    /// בקשות שסומנו בתגית הישנה נשארות מקושרות לאותה שורה, ולכן מקבלות את השם החדש.
    /// ב-DB חדש הטבלה עדיין ריקה בשלב הזה (ה-Seed רץ אחרי ה-Migrations), כך שה-UPDATE לא משנה כלום.
    /// </summary>
    public partial class RenamePetsTagToSleepWatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Tags" SET "Name" = 'שמירה בזמן שינה' WHERE "Name" = 'בעלי חיים בבית';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Tags" SET "Name" = 'בעלי חיים בבית' WHERE "Name" = 'שמירה בזמן שינה';""");
        }
    }
}
