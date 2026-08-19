using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceJobOfferConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_JobPostings_Budget_Positive",
                table: "JobPostings",
                sql: "[Budget] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_JobPostingId_Status",
                table: "JobOffers",
                columns: new[] { "JobPostingId", "Status" },
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_JobOffers_ProposedPrice_Positive",
                table: "JobOffers",
                sql: "[ProposedPrice] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_JobPostings_Budget_Positive",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_JobPostingId_Status",
                table: "JobOffers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_JobOffers_ProposedPrice_Positive",
                table: "JobOffers");
        }
    }
}
