using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace RentalPlatform.MySqlMigrations
{
    /// <inheritdoc />
    public partial class AdvancedPlatformFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Vehicles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HangXe",
                table: "Vehicles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BiTamKhoa",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuXacThuc",
                table: "Users",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GioiTinh",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GuiXacThucAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthProvider",
                table: "Users",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OAuthSubject",
                table: "Users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoLanViPham",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TamKhoaDen",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiXacThuc",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.Sql("UPDATE `Users` SET `TrangThaiXacThuc` = 'ChuaGui' WHERE `TrangThaiXacThuc` IS NULL OR `TrangThaiXacThuc` = '';");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThaiXacThuc",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "XacThucAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "RentalOrders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoHuy",
                table: "RentalOrders",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiDichVu",
                table: "RentalOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienHoanDuKien",
                table: "RentalOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TienDatCoc",
                table: "RentalOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TienThue",
                table: "RentalOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TyLeHoanTien",
                table: "RentalOrders",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuDoiSoat",
                table: "RentalOrderPayments",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAt",
                table: "RentalOrderPayments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TyLeDatCoc = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TyLePhiDichVu = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HoanTienTruoc72Gio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HoanTienTruoc24Gio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HoanTienTrong24Gio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PhatTraMuonMoiGio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuyDinhViPham = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPolicies", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql("UPDATE `RentalOrders` SET `TienThue` = `TongTien` WHERE `TienThue` = 0;");
            migrationBuilder.InsertData(
                table: "SystemPolicies",
                columns: new[] { "Id", "TyLeDatCoc", "TyLePhiDichVu", "HoanTienTruoc72Gio", "HoanTienTruoc24Gio", "HoanTienTrong24Gio", "PhatTraMuonMoiGio", "QuyDinhViPham", "UpdatedAt" },
                values: new object[] { 1, 20m, 5m, 100m, 70m, 30m, 100000m, "Tuân thủ hợp đồng, giao nhận đúng hẹn và bảo quản phương tiện.", DateTime.UtcNow });

            migrationBuilder.CreateTable(
                name: "ViolationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReporterId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "char(36)", nullable: true),
                    LoaiViPham = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    BangChungUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(255)", nullable: false),
                    HinhThucXuLy = table.Column<string>(type: "longtext", nullable: false),
                    GhiChuAdmin = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViolationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViolationReports_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ViolationReports_Users_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViolationReports_Users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OAuthProvider_OAuthSubject",
                table: "Users",
                columns: new[] { "OAuthProvider", "OAuthSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViolationReports_RentalOrderId",
                table: "ViolationReports",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationReports_ReportedUserId",
                table: "ViolationReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationReports_ReporterId",
                table: "ViolationReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ViolationReports_TrangThai_CreatedAt",
                table: "ViolationReports",
                columns: new[] { "TrangThai", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemPolicies");

            migrationBuilder.DropTable(
                name: "ViolationReports");

            migrationBuilder.DropIndex(
                name: "IX_Users_OAuthProvider_OAuthSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "HangXe",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "BiTamKhoa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GhiChuXacThuc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GioiTinh",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuiXacThucAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OAuthSubject",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SoLanViPham",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TamKhoaDen",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TrangThaiXacThuc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "XacThucAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "LyDoHuy",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "PhiDichVu",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "SoTienHoanDuKien",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "TienDatCoc",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "TienThue",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "TyLeHoanTien",
                table: "RentalOrders");

            migrationBuilder.DropColumn(
                name: "GhiChuDoiSoat",
                table: "RentalOrderPayments");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "RentalOrderPayments");
        }
    }
}
