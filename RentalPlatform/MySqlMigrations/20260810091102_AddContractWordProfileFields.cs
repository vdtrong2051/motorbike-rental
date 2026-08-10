using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPlatform.MySqlMigrations
{
    /// <inheritdoc />
    public partial class AddContractWordProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "Users",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoCCCD",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoGPLX",
                table: "Users",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChiChuXe",
                table: "RentalOrderContracts",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailChuXe",
                table: "RentalOrderContracts",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoTenChuXe",
                table: "RentalOrderContracts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SDTChuXe",
                table: "RentalOrderContracts",
                type: "varchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoCCCDChuXe",
                table: "RentalOrderContracts",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SoCCCD",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SoGPLX",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiaChiChuXe",
                table: "RentalOrderContracts");

            migrationBuilder.DropColumn(
                name: "EmailChuXe",
                table: "RentalOrderContracts");

            migrationBuilder.DropColumn(
                name: "HoTenChuXe",
                table: "RentalOrderContracts");

            migrationBuilder.DropColumn(
                name: "SDTChuXe",
                table: "RentalOrderContracts");

            migrationBuilder.DropColumn(
                name: "SoCCCDChuXe",
                table: "RentalOrderContracts");
        }
    }
}
