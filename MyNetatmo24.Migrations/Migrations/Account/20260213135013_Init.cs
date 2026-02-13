using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyNetatmo24.Migrations.Migrations.Account
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accountmamangement");

            migrationBuilder.CreateTable(
                name: "Accounts",
                schema: "accountmamangement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Auth0Id = table.Column<string>(type: "text", nullable: true),
                    NickName = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name_FirstName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name_LastName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NetatmoAuthInfo_AccessToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NetatmoAuthInfo_ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NetatmoAuthInfo_RefreshToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Auth0Id",
                schema: "accountmamangement",
                table: "Accounts",
                column: "Auth0Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts",
                schema: "accountmamangement");
        }
    }
}
