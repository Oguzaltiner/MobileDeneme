using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeVocabularySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "vocabulary_words",
                columns: new[] { "Id", "Category", "Definition", "ExampleSentence", "Level", "PartOfSpeech", "Pronunciation", "Term", "Translation" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000006"), "Daily Life", "to make something better", "Practice helps you improve.", "A2", "verb", "/ɪmˈpruːv/", "improve", "geliştirmek" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "Daily Life", "to get ready for something", "I prepare for the quiz every morning.", "A2", "verb", "/prɪˈpeə/", "prepare", "hazırlanmak" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "Personality", "giving attention to avoid mistakes", "Be careful with the answer.", "A2", "adjective", "/ˈkeəfəl/", "careful", "dikkatli" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"));
        }
    }
}
