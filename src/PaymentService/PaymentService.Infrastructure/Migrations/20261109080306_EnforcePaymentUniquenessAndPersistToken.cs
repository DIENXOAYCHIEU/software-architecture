using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePaymentUniquenessAndPersistToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_TripId",
                table: "payments");

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "payments",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TripId",
                table: "payments",
                column: "TripId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_TripId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "payments");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TripId",
                table: "payments",
                column: "TripId");
        }
    }
}
