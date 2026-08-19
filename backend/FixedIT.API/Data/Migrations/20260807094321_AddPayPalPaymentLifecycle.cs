using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixedIT.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayPalPaymentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalCaptureId",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalRefundId",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayPalCaptureId",
                table: "Payments",
                column: "PayPalCaptureId",
                unique: true,
                filter: "[PayPalCaptureId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayPalRefundId",
                table: "Payments",
                column: "PayPalRefundId",
                unique: true,
                filter: "[PayPalRefundId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "[Amount] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_PayPalCaptureId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PayPalRefundId",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayPalCaptureId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayPalRefundId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Payments");
        }
    }
}
