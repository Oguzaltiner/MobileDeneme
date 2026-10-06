using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyMissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "user_settings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeZoneUpdatedAtUtc",
                table: "user_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "daily_missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PracticeSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    XpAwarded = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswers = table.Column<int>(type: "integer", nullable: false),
                    ReviewedWords = table.Column<int>(type: "integer", nullable: false),
                    NewWordsLearned = table.Column<int>(type: "integer", nullable: false),
                    WordStepsCapped = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_missions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_daily_missions_practice_sessions_PracticeSessionId",
                        column: x => x.PracticeSessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_daily_missions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_missions_CompletedAtUtc",
                table: "daily_missions",
                column: "CompletedAtUtc",
                filter: "\"Status\" = 'Completed'")
                .Annotation("Npgsql:IndexInclude", new[] { "UserId", "XpAwarded" });

            migrationBuilder.CreateIndex(
                name: "IX_daily_missions_PracticeSessionId",
                table: "daily_missions",
                column: "PracticeSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_missions_UserId_MissionDate",
                table: "daily_missions",
                columns: new[] { "UserId", "MissionDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_missions");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "TimeZoneUpdatedAtUtc",
                table: "user_settings");
        }
    }
}
