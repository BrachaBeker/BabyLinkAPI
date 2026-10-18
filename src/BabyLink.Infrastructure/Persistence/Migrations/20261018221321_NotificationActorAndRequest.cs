using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabyLink.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// הכללה של ההודעות לסוגים נוספים (שמרטפית תפסה / ביטלה שיבוץ):
    /// ParentFullName הופך ל-ActorFullName (RenameColumn - הנתונים נשמרים),
    /// ו-RequestId אופציונלי לקישור לבקשה (SetNull כשהבקשה נמחקת).
    /// </summary>
    public partial class NotificationActorAndRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ParentFullName",
                table: "Notifications",
                newName: "ActorFullName");

            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RequestId",
                table: "Notifications",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_ServiceRequests_RequestId",
                table: "Notifications",
                column: "RequestId",
                principalTable: "ServiceRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_ServiceRequests_RequestId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RequestId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "ActorFullName",
                table: "Notifications",
                newName: "ParentFullName");
        }
    }
}
