using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoungeClubsApi.Migrations
{
    /// <inheritdoc />
    public partial class PendingTransactionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Context_ContextId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Context_ContextId",
                table: "Transactions",
                columns: new[] { "Context", "ContextId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Context_ContextId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Context_ContextId",
                table: "Transactions",
                columns: new[] { "Context", "ContextId" });
        }
    }
}
