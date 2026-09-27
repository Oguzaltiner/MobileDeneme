using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizQuestionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorTag",
                table: "quiz_questions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "quiz_questions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skill",
                table: "quiz_questions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "vocabulary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorTag",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "quiz_questions");

            migrationBuilder.DropColumn(
                name: "Skill",
                table: "quiz_questions");
        }
    }
}
