using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingReliabilityAndStatusLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedNotificationMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedNotificationMessages", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_NewStatus",
                table: "ReservationStatusHistories",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistories_PreviousStatus",
                table: "ReservationStatusHistories",
                column: "PreviousStatus");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_PublishedAt_NextAttemptAt",
                table: "OutboxMessages",
                columns: new[] { "PublishedAt", "NextAttemptAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationStatusHistories_ReservationStatusDefinitions_NewStatus",
                table: "ReservationStatusHistories",
                column: "NewStatus",
                principalTable: "ReservationStatusDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationStatusHistories_ReservationStatusDefinitions_PreviousStatus",
                table: "ReservationStatusHistories",
                column: "PreviousStatus",
                principalTable: "ReservationStatusDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationStatusHistories_ReservationStatusDefinitions_NewStatus",
                table: "ReservationStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationStatusHistories_ReservationStatusDefinitions_PreviousStatus",
                table: "ReservationStatusHistories");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProcessedNotificationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ReservationStatusHistories_NewStatus",
                table: "ReservationStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_ReservationStatusHistories_PreviousStatus",
                table: "ReservationStatusHistories");
        }
    }
}
