using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistories_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [ReservationStatusHistories]
                    ([ReservationId], [PreviousStatus], [NewStatus], [ChangedByUserId], [Reason], [ChangedAt])
                SELECT [Id], NULL, [Status], [ClientUserId], [CancellationReason], [UpdatedAt]
                FROM [Reservations];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_ReservationId_ChangedAt",
                table: "ReservationStatusHistories",
                columns: new[] { "ReservationId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationStatusHistories");
        }
    }
}
