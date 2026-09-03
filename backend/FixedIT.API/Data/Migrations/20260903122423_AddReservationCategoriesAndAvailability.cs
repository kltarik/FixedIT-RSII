using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationCategoriesAndAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE [Reservations]
                SET [CategoryId] = COALESCE(
                    (SELECT MIN([pc].[CategoryId])
                     FROM [ProfessionalCategories] AS [pc]
                     WHERE [pc].[ProfessionalProfileId] = [Reservations].[ProfessionalProfileId]),
                    (SELECT MIN([c].[Id]) FROM [Categories] AS [c]));
                """);

            migrationBuilder.CreateTable(
                name: "ProfessionalAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalAvailabilities", x => x.Id);
                    table.CheckConstraint("CK_ProfessionalAvailabilities_TimeRange", "[StartTime] < [EndTime]");
                    table.ForeignKey(
                        name: "FK_ProfessionalAvailabilities_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CategoryId",
                table: "Reservations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalAvailabilities_ProfessionalProfileId_DayOfWeek_StartTime",
                table: "ProfessionalAvailabilities",
                columns: new[] { "ProfessionalProfileId", "DayOfWeek", "StartTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Categories_CategoryId",
                table: "Reservations",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Categories_CategoryId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ProfessionalAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CategoryId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Reservations");
        }
    }
}
