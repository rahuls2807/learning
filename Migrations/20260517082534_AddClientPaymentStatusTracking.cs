using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkerBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPaymentStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaidOnline",
                table: "Bookings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaidToWorker",
                table: "Bookings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ClientStatusNote",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastClientStatusUpdate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaidOnline",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AmountPaidToWorker",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ClientStatusNote",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "LastClientStatusUpdate",
                table: "Bookings");
        }
    }
}
