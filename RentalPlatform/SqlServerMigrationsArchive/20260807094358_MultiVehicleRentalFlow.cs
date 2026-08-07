using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPlatform.Migrations
{
    /// <inheritdoc />
    public partial class MultiVehicleRentalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaDon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoTenKhach = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailKhach = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SDTKhach = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    SoCCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoGPLX = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiaChiKhach = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThaiThanhToan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaThamChieuThanhToan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RentalOrders_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalOrderContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoHopDong = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NoiDungHopDong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaKyKhach = table.Column<bool>(type: "bit", nullable: false),
                    NgayKyKhach = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaKyChuXe = table.Column<bool>(type: "bit", nullable: false),
                    NgayKyChuXe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderContracts_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RentalOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenXe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BienSo = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    LoaiXe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TinhTrangLucDat = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NgayNhanXe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTraXe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonGiaNgay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoNgayThue = table.Column<int>(type: "int", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderItems_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalOrderItems_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RentalOrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaGiaoDich = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChuCongThanhToan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentalOrderPayments_RentalOrders_RentalOrderId",
                        column: x => x.RentalOrderId,
                        principalTable: "RentalOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderContracts_RentalOrderId",
                table: "RentalOrderContracts",
                column: "RentalOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderContracts_SoHopDong",
                table: "RentalOrderContracts",
                column: "SoHopDong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_RentalOrderId",
                table: "RentalOrderItems",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderItems_VehicleId_NgayNhanXe_NgayTraXe",
                table: "RentalOrderItems",
                columns: new[] { "VehicleId", "NgayNhanXe", "NgayTraXe" });

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPayments_MaGiaoDich",
                table: "RentalOrderPayments",
                column: "MaGiaoDich",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrderPayments_RentalOrderId",
                table: "RentalOrderPayments",
                column: "RentalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_CustomerId_CreatedAt",
                table: "RentalOrders",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_MaDon",
                table: "RentalOrders",
                column: "MaDon",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentalOrders_OwnerId_CreatedAt",
                table: "RentalOrders",
                columns: new[] { "OwnerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalOrderContracts");

            migrationBuilder.DropTable(
                name: "RentalOrderItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPayments");

            migrationBuilder.DropTable(
                name: "RentalOrders");
        }
    }
}
