using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vocabulary_words",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Pronunciation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Definition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Translation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExampleSentence = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vocabulary_words", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentLevel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DailyGoal = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    LearningPurpose = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_user_settings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "vocabulary_words",
                columns: new[] { "Id", "Category", "Definition", "ExampleSentence", "Level", "PartOfSpeech", "Pronunciation", "Term", "Translation" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "Daily Life", "to succeed in doing something", "You can achieve your goals.", "A2", "verb", "/əˈtʃiːv/", "achieve", "başarmak" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Personality", "wanting to know or learn something", "Children are naturally curious.", "A2", "adjective", "/ˈkjʊəriəs/", "curious", "meraklı" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Work", "working well without wasting time or energy", "This is an efficient way to study.", "B1", "adjective", "/ɪˈfɪʃənt/", "efficient", "verimli" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "Travel", "an act of travelling from one place to another", "The journey took three hours.", "B1", "noun", "/ˈdʒɜːni/", "journey", "yolculuk" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "Academic", "important or noticeable", "The study found a significant difference.", "B2", "adjective", "/sɪɡˈnɪfɪkənt/", "significant", "önemli" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAtUtc",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_words_Level_Category",
                table: "vocabulary_words",
                columns: new[] { "Level", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_words_Term",
                table: "vocabulary_words",
                column: "Term",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "vocabulary_words");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
