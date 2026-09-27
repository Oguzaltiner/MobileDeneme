using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyPublicationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicationStatus",
                table: "vocabulary_words",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "vocabulary_words",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.UpdateData(
                table: "vocabulary_words",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "PublicationStatus", "PublishedAtUtc" },
                values: new object[] { "Published", null });

            migrationBuilder.CreateIndex(
                name: "IX_vocabulary_words_PublicationStatus",
                table: "vocabulary_words",
                column: "PublicationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vocabulary_words_PublicationStatus",
                table: "vocabulary_words");

            migrationBuilder.DropColumn(
                name: "PublicationStatus",
                table: "vocabulary_words");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "vocabulary_words");
        }
    }
}
