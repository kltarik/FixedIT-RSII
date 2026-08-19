using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_ProfessionalProfileId_ScheduledAt",
                table: "Reservations");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Reservations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ProfessionalProfileId_ScheduledAt",
                table: "Reservations",
                columns: new[] { "ProfessionalProfileId", "ScheduledAt" },
                unique: true,
                filter: "[Status] <> 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_DurationMinutes_Positive",
                table: "Reservations",
                sql: "[DurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_TotalPrice_Positive",
                table: "Reservations",
                sql: "[TotalPrice] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_ProfessionalProfileId_ScheduledAt",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_DurationMinutes_Positive",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_TotalPrice_Positive",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ProfessionalProfileId_ScheduledAt",
                table: "Reservations",
                columns: new[] { "ProfessionalProfileId", "ScheduledAt" },
                unique: true);
        }
    }
}
