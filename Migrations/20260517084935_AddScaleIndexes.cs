using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkerBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddScaleIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HourlyRates_WorkerId",
                table: "HourlyRates");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ClientId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_WorkerId",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "Skill",
                table: "Workers",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Workers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_IsActive",
                table: "Workers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_PhoneNumber",
                table: "Workers",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_Skill",
                table: "Workers",
                column: "Skill");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRates_WorkerId_IsActive_EffectiveDate",
                table: "HourlyRates",
                columns: new[] { "WorkerId", "IsActive", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ClientId_BookingDate",
                table: "Bookings",
                columns: new[] { "ClientId", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentStatus",
                table: "Bookings",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WorkerId_BookingDate",
                table: "Bookings",
                columns: new[] { "WorkerId", "BookingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workers_IsActive",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_PhoneNumber",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_Workers_Skill",
                table: "Workers");

            migrationBuilder.DropIndex(
                name: "IX_HourlyRates_WorkerId_IsActive_EffectiveDate",
                table: "HourlyRates");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ClientId_BookingDate",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentStatus",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_WorkerId_BookingDate",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "Skill",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyRates_WorkerId",
                table: "HourlyRates",
                column: "WorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ClientId",
                table: "Bookings",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_WorkerId",
                table: "Bookings",
                column: "WorkerId");
        }
    }
}
