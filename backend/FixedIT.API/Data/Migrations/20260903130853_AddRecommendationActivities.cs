using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationActivities", x => x.Id);
                    table.CheckConstraint("CK_RecommendationActivities_Target", "([Type] = 1 AND [ProfessionalProfileId] IS NOT NULL AND [CategoryId] IS NULL) OR ([Type] = 2 AND [ProfessionalProfileId] IS NULL AND [CategoryId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RecommendationActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationActivities_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationActivities_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationActivities_CategoryId",
                table: "RecommendationActivities",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationActivities_ProfessionalProfileId",
                table: "RecommendationActivities",
                column: "ProfessionalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationActivities_Type_ProfessionalProfileId_CategoryId",
                table: "RecommendationActivities",
                columns: new[] { "Type", "ProfessionalProfileId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationActivities_UserId_CreatedAt",
                table: "RecommendationActivities",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationActivities");
        }
    }
}
