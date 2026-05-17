using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkerBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerProfilesAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumePath",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkerReviews",
                columns: table => new
                {
                    WorkerReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    BookingId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewerName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsAdminReview = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerReviews", x => x.WorkerReviewId);
                    table.ForeignKey(
                        name: "FK_WorkerReviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId");
                    table.ForeignKey(
                        name: "FK_WorkerReviews_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId");
                    table.ForeignKey(
                        name: "FK_WorkerReviews_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "WorkerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkerReviews_BookingId",
                table: "WorkerReviews",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerReviews_ClientId",
                table: "WorkerReviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerReviews_WorkerId_CreatedDate",
                table: "WorkerReviews",
                columns: new[] { "WorkerId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerReviews");

            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ResumePath",
                table: "Workers");
        }
    }
}
