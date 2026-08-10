using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPlatform.MySqlMigrations
{
    /// <inheritdoc />
    public partial class AddIdentityDocumentImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnhCCCDMatSau",
                table: "Users",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnhCCCDMatTruoc",
                table: "Users",
                type: "varchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhCCCDMatSau",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AnhCCCDMatTruoc",
                table: "Users");
        }
    }
}
