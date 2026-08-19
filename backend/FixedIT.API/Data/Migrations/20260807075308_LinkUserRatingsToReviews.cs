using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkUserRatingsToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewId",
                table: "UserRatings",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [rating]
                SET [rating].[ReviewId] = [review].[Id]
                FROM [UserRatings] AS [rating]
                INNER JOIN [Reviews] AS [review]
                    ON [review].[ClientUserId] = [rating].[UserId]
                    AND [review].[ProfessionalProfileId] = [rating].[ProfessionalProfileId]
                    AND [review].[Rating] = [rating].[Rating]
                    AND [review].[CreatedAt] = [rating].[Timestamp]
                WHERE [rating].[ReviewId] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserRatings_ReviewId",
                table: "UserRatings",
                column: "ReviewId",
                unique: true,
                filter: "[ReviewId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRatings_Reviews_ReviewId",
                table: "UserRatings",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRatings_Reviews_ReviewId",
                table: "UserRatings");

            migrationBuilder.DropIndex(
                name: "IX_UserRatings_ReviewId",
                table: "UserRatings");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "UserRatings");
        }
    }
}
