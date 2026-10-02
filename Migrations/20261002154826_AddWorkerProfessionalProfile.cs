using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkerBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerProfessionalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "Workers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalSummary",
                table: "Workers",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsExperience",
                table: "Workers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "ProfessionalSummary",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "YearsExperience",
                table: "Workers");
        }
    }
}
