using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "user_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "QuietHoursEnd",
                table: "user_settings",
                type: "integer",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<int>(
                name: "QuietHoursStart",
                table: "user_settings",
                type: "integer",
                nullable: false,
                defaultValue: 22);

            migrationBuilder.AddColumn<int>(
                name: "ReminderHour",
                table: "user_settings",
                type: "integer",
                nullable: false,
                defaultValue: 19);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "QuietHoursEnd",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "QuietHoursStart",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "ReminderHour",
                table: "user_settings");
        }
    }
}
