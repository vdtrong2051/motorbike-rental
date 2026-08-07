using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPlatform.MySqlMigrations
{
    /// <inheritdoc />
    public partial class InitialMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    HoTen = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    SDT = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false),
                    Role = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    MaDon = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    HoTenKhach = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EmailKhach = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    SDTKhach = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    SoCCCD = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    SoGPLX = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    DiaChiKhach = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    GhiChu = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "longtext", nullable: true),
                    TrangThaiThanhToan = table.Column<string>(type: "longtext", nullable: false),
                    MaThamChieuThanhToan = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OwnerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenXe = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LoaiXe = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    BienSo = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    SoChoNgoi = table.Column<int>(type: "int", nullable: false),
                    HopSo = table.Column<string>(type: "longtext", nullable: false),
                    NhienLieu = table.Column<string>(type: "longtext", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    KhuVuc = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DiaChiCuThe = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    GiaThueMotNgay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoTa = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    TrangThai = table.Column<string>(type: "varchar(255)", nullable: false),
                    LyDoTuChoi = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SoHopDong = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    NoiDungHopDong = table.Column<string>(type: "longtext", nullable: false),
                    DaKyKhach = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NgayKyKhach = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DaKyChuXe = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NgayKyChuXe = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    MaGiaoDich = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    PhuongThuc = table.Column<string>(type: "longtext", nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChuCongThanhToan = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    VehicleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: false),
                    GiaThueMotNgayLucDat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "varchar(255)", nullable: false),
                    GhiChu = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    LyDoHuy = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RentalOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RentalOrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    VehicleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TenXe = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    BienSo = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    LoaiXe = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TinhTrangLucDat = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    NgayNhanXe = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NgayTraXe = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VehicleImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    VehicleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    DuongDanAnh = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    LaAnhDaiDien = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleImages_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BookingId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SoHopDong = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    NoiDungHopDong = table.Column<string>(type: "longtext", nullable: false),
                    DaKyKhach = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NgayKyKhach = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DaKyChuXe = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NgayKyChuXe = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BookingId = table.Column<Guid>(type: "char(36)", nullable: false),
                    LoaiThanhToan = table.Column<string>(type: "longtext", nullable: false),
                    PhuongThuc = table.Column<string>(type: "longtext", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaGiaoDich = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BookingId = table.Column<Guid>(type: "char(36)", nullable: false),
                    VehicleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TrangThai",
                table: "Bookings",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleId_NgayBatDau_NgayKetThuc",
                table: "Bookings",
                columns: new[] { "VehicleId", "NgayBatDau", "NgayKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_BookingId",
                table: "Contracts",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_SoHopDong",
                table: "Contracts",
                column: "SoHopDong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MaGiaoDich",
                table: "Payments",
                column: "MaGiaoDich",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_VehicleId",
                table: "Reviews",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleImages_VehicleId",
                table: "VehicleImages",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_BienSo",
                table: "Vehicles",
                column: "BienSo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_GiaThueMotNgay",
                table: "Vehicles",
                column: "GiaThueMotNgay");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_KhuVuc",
                table: "Vehicles",
                column: "KhuVuc");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LoaiXe",
                table: "Vehicles",
                column: "LoaiXe");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId",
                table: "Vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TrangThai",
                table: "Vehicles",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RentalOrderContracts");

            migrationBuilder.DropTable(
                name: "RentalOrderItems");

            migrationBuilder.DropTable(
                name: "RentalOrderPayments");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "VehicleImages");

            migrationBuilder.DropTable(
                name: "RentalOrders");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
