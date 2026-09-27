using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptivePlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "quiz_questions",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "placement_test_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    AnsweredCount = table.Column<int>(type: "integer", nullable: false),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    ScorePercent = table.Column<int>(type: "integer", nullable: true),
                    EstimatedLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placement_test_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_placement_test_attempts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "placement_test_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    VocabularyWordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Answered = table.Column<bool>(type: "boolean", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placement_test_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_placement_test_questions_placement_test_attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "placement_test_attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_placement_test_questions_vocabulary_words_VocabularyWordId",
                        column: x => x.VocabularyWordId,
                        principalTable: "vocabulary_words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "placement_test_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_placement_test_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_placement_test_options_placement_test_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "placement_test_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_placement_test_attempts_UserId_StartedAtUtc",
                table: "placement_test_attempts",
                columns: new[] { "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_placement_test_options_QuestionId_Key",
                table: "placement_test_options",
                columns: new[] { "QuestionId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_placement_test_questions_AttemptId_Order",
                table: "placement_test_questions",
                columns: new[] { "AttemptId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_placement_test_questions_VocabularyWordId",
                table: "placement_test_questions",
                column: "VocabularyWordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "placement_test_options");

            migrationBuilder.DropTable(
                name: "placement_test_questions");

            migrationBuilder.DropTable(
                name: "placement_test_attempts");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "quiz_questions");
        }
    }
}
