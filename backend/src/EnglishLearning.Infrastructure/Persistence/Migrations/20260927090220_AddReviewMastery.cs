using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLearning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewMastery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectReviews",
                table: "user_word_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Lapses",
                table: "user_word_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastRating",
                table: "user_word_progress",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MasteryScore",
                table: "user_word_progress",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "user_word_progress",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectReviews",
                table: "user_word_progress");

            migrationBuilder.DropColumn(
                name: "Lapses",
                table: "user_word_progress");

            migrationBuilder.DropColumn(
                name: "LastRating",
                table: "user_word_progress");

            migrationBuilder.DropColumn(
                name: "MasteryScore",
                table: "user_word_progress");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "user_word_progress");
        }
    }
}
