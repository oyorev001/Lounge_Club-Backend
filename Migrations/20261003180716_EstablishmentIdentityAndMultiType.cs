using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoungeClubsApi.Migrations
{
    /// <inheritdoc />
    public partial class EstablishmentIdentityAndMultiType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalTypes",
                table: "Establishments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerNin",
                table: "Establishments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalTypes",
                table: "Establishments");

            migrationBuilder.DropColumn(
                name: "OwnerNin",
                table: "Establishments");
        }
    }
}
