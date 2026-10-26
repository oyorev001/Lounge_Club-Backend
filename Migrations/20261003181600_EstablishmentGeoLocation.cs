using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoungeClubsApi.Migrations
{
    /// <inheritdoc />
    public partial class EstablishmentGeoLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Establishments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Establishments",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Establishments");
        }
    }
}
