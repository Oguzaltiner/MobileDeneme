using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "practice_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practice_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_practice_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "practice_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    Rating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ClientEventId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practice_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_practice_events_practice_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_practice_events_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_practice_events_vocabulary_words_VocabularyWordId",
                        column: x => x.VocabularyWordId,
                        principalTable: "vocabulary_words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "practice_session_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_practice_session_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_practice_session_steps_practice_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "practice_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_practice_events_SessionId",
                table: "practice_events",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_practice_events_UserId_ClientEventId",
                table: "practice_events",
                columns: new[] { "UserId", "ClientEventId" },
                unique: true,
                filter: "\"ClientEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_practice_events_UserId_CreatedAtUtc",
                table: "practice_events",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_practice_events_VocabularyWordId",
                table: "practice_events",
                column: "VocabularyWordId");

            migrationBuilder.CreateIndex(
                name: "IX_practice_session_steps_SessionId_Order",
                table: "practice_session_steps",
                columns: new[] { "SessionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_practice_sessions_UserId_StartedAtUtc",
                table: "practice_sessions",
                columns: new[] { "UserId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "practice_events");

            migrationBuilder.DropTable(
                name: "practice_session_steps");

            migrationBuilder.DropTable(
                name: "practice_sessions");
        }
    }
}
