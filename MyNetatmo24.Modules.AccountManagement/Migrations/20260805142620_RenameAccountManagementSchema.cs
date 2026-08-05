using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyNetatmo24.Modules.AccountManagement.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accountmanagement");

            migrationBuilder.RenameTable(
                name: "Accounts",
                schema: "accountmamangement",
                newName: "Accounts",
                newSchema: "accountmanagement");

            migrationBuilder.Sql("DROP SCHEMA IF EXISTS accountmamangement;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accountmamangement");

            migrationBuilder.RenameTable(
                name: "Accounts",
                schema: "accountmanagement",
                newName: "Accounts",
                newSchema: "accountmamangement");
        }
    }
}
