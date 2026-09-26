using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_usage",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    WordsUsed = table.Column<int>(type: "integer", nullable: false),
                    QuizzesStarted = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_usage", x => new { x.UserId, x.DateUtc });
                    table.ForeignKey(
                        name: "FK_daily_usage_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_usage");
        }
    }
}
