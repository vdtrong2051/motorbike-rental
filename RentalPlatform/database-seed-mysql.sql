-- ============================================================
-- RENTALPLATFORM - DỮ LIỆU MẪU MYSQL 8.0+
-- Chạy file này bằng tài khoản root sau khi đã chạy migration.
-- File có thể chạy lại: các bản ghi mẫu dùng UUID cố định và
-- được cập nhật bằng ON DUPLICATE KEY UPDATE.
--
-- TÀI KHOẢN MẪU
-- admin@rentalplatform.vn     / Admin@123
-- owner1@rental.demo         / Owner@123
-- owner2@rental.demo         / Owner@123
-- customer1@rental.demo      / Customer@123
-- customer2@rental.demo      / Customer@123
-- ============================================================

CREATE DATABASE IF NOT EXISTS RentalPlatformDb
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE RentalPlatformDb;
SET NAMES utf8mb4;
START TRANSACTION;

-- ------------------------------------------------------------
-- 1. USERS
-- PasswordHash sử dụng ASP.NET Core PasswordHasher (Identity V3).
-- ------------------------------------------------------------
INSERT INTO `Users`
(`Id`, `HoTen`, `Email`, `SDT`, `PasswordHash`, `Role`, `CreatedAt`)
VALUES
('00000000-0000-0000-0000-000000000001',
 'Quản trị viên mẫu',
 'admin@rentalplatform.vn',
 '0900000001',
 'AQAAAAIAAYagAAAAEJmbj3JDPyFwWJt95tNlkptcKNFD8z8Ej/00VPVI0IGYxmkhdtmbDlBwj7n+GPPRbQ==',
 'Admin',
 UTC_TIMESTAMP()),
('10000000-0000-0000-0000-000000000101',
 'Nguyễn Minh Anh',
 'owner1@rental.demo',
 '0901000101',
 'AQAAAAIAAYagAAAAEDUSxGB8J177/tMw/A3xkRBctuEx1fhPSK01vD+JttvVagcq8tlDzKO35bwGASjAwg==',
 'Owner',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 180 DAY)),
('10000000-0000-0000-0000-000000000102',
 'Trần Hoàng Nam',
 'owner2@rental.demo',
 '0901000102',
 'AQAAAAIAAYagAAAAEDUSxGB8J177/tMw/A3xkRBctuEx1fhPSK01vD+JttvVagcq8tlDzKO35bwGASjAwg==',
 'Owner',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 160 DAY)),
('10000000-0000-0000-0000-000000000201',
 'Lê Thu Hà',
 'customer1@rental.demo',
 '0902000201',
 'AQAAAAIAAYagAAAAEOAlEtVfgE9ms/9LxJY6sK9t6yEo9o4aME+9vjR3tqxXNUxsDStq1sVZSmdTBd2rOQ==',
 'Customer',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 120 DAY)),
('10000000-0000-0000-0000-000000000202',
 'Phạm Quốc Bảo',
 'customer2@rental.demo',
 '0902000202',
 'AQAAAAIAAYagAAAAEOAlEtVfgE9ms/9LxJY6sK9t6yEo9o4aME+9vjR3tqxXNUxsDStq1sVZSmdTBd2rOQ==',
 'Customer',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 100 DAY))
ON DUPLICATE KEY UPDATE
    `HoTen` = VALUES(`HoTen`),
    `SDT` = VALUES(`SDT`),
    `PasswordHash` = VALUES(`PasswordHash`),
    `Role` = VALUES(`Role`);

-- ------------------------------------------------------------
-- 2. VEHICLES
-- ------------------------------------------------------------
INSERT INTO `Vehicles`
(`Id`, `OwnerId`, `TenXe`, `LoaiXe`, `BienSo`, `SoChoNgoi`,
 `HopSo`, `NhienLieu`, `Nam`, `KhuVuc`, `DiaChiCuThe`,
 `GiaThueMotNgay`, `MoTa`, `TrangThai`, `LyDoTuChoi`, `CreatedAt`)
VALUES
('11000000-0000-0000-0000-000000000001',
 '10000000-0000-0000-0000-000000000101',
 'Honda SH 160i 2025', 'Xe tay ga', '29A1-123.45', 2,
 'SoVoCap', 'Xang', 2025, 'Cầu Giấy, Hà Nội',
 'Đường Trần Thái Tông, Cầu Giấy',
 450000, 'Xe mới, phanh ABS, smart key, kèm hai mũ bảo hiểm.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 90 DAY)),
('11000000-0000-0000-0000-000000000002',
 '10000000-0000-0000-0000-000000000101',
 'Vespa Sprint 150', 'Xe tay ga', '29A1-234.56', 2,
 'SoVoCap', 'Xang', 2024, 'Ba Đình, Hà Nội',
 'Phố Đội Cấn, Ba Đình',
 400000, 'Kiểu dáng cổ điển, máy êm, đầy đủ giấy tờ.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 82 DAY)),
('11000000-0000-0000-0000-000000000003',
 '10000000-0000-0000-0000-000000000101',
 'Yamaha Grande Hybrid', 'Xe tay ga', '29B1-345.67', 2,
 'SoVoCap', 'Hybrid', 2024, 'Đống Đa, Hà Nội',
 'Phố Thái Hà, Đống Đa',
 350000, 'Tiết kiệm nhiên liệu, cốp rộng, phù hợp đi trong thành phố.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 70 DAY)),
('11000000-0000-0000-0000-000000000004',
 '10000000-0000-0000-0000-000000000102',
 'Mazda CX-5 Premium', 'Ô tô SUV', '30K-456.78', 5,
 'SoTuDong', 'Xang', 2024, 'Nam Từ Liêm, Hà Nội',
 'Khu đô thị Mỹ Đình, Nam Từ Liêm',
 1200000, 'SUV 5 chỗ, camera 360, bảo hiểm thân vỏ và định vị.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 65 DAY)),
('11000000-0000-0000-0000-000000000005',
 '10000000-0000-0000-0000-000000000102',
 'Toyota Vios G', 'Ô tô Sedan', '30K-567.89', 5,
 'SoTuDong', 'Xang', 2023, 'Thanh Xuân, Hà Nội',
 'Đường Nguyễn Trãi, Thanh Xuân',
 950000, 'Sedan tiết kiệm, camera lùi, điều hòa tự động.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 58 DAY)),
('11000000-0000-0000-0000-000000000006',
 '10000000-0000-0000-0000-000000000102',
 'VinFast Feliz S', 'Xe máy điện', '29MD1-678.90', 2,
 'SoVoCap', 'Dien', 2024, 'Hai Bà Trưng, Hà Nội',
 'Phố Bạch Mai, Hai Bà Trưng',
 380000, 'Xe điện pin khỏe, tặng kèm sạc và hai mũ bảo hiểm.',
 'SanSang', NULL, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 45 DAY))
ON DUPLICATE KEY UPDATE
    `OwnerId` = VALUES(`OwnerId`),
    `TenXe` = VALUES(`TenXe`),
    `LoaiXe` = VALUES(`LoaiXe`),
    `SoChoNgoi` = VALUES(`SoChoNgoi`),
    `HopSo` = VALUES(`HopSo`),
    `NhienLieu` = VALUES(`NhienLieu`),
    `Nam` = VALUES(`Nam`),
    `KhuVuc` = VALUES(`KhuVuc`),
    `DiaChiCuThe` = VALUES(`DiaChiCuThe`),
    `GiaThueMotNgay` = VALUES(`GiaThueMotNgay`),
    `MoTa` = VALUES(`MoTa`),
    `TrangThai` = VALUES(`TrangThai`),
    `LyDoTuChoi` = NULL;

INSERT INTO `VehicleImages`
(`Id`, `VehicleId`, `DuongDanAnh`, `LaAnhDaiDien`, `ThuTu`)
VALUES
('12000000-0000-0000-0000-000000000001', '11000000-0000-0000-0000-000000000001', '/image/no-image.png', 1, 0),
('12000000-0000-0000-0000-000000000002', '11000000-0000-0000-0000-000000000002', '/image/no-image.png', 1, 0),
('12000000-0000-0000-0000-000000000003', '11000000-0000-0000-0000-000000000003', '/image/no-image.png', 1, 0),
('12000000-0000-0000-0000-000000000004', '11000000-0000-0000-0000-000000000004', '/image/no-image.png', 1, 0),
('12000000-0000-0000-0000-000000000005', '11000000-0000-0000-0000-000000000005', '/image/no-image.png', 1, 0),
('12000000-0000-0000-0000-000000000006', '11000000-0000-0000-0000-000000000006', '/image/no-image.png', 1, 0)
ON DUPLICATE KEY UPDATE
    `VehicleId` = VALUES(`VehicleId`),
    `DuongDanAnh` = VALUES(`DuongDanAnh`),
    `LaAnhDaiDien` = VALUES(`LaAnhDaiDien`),
    `ThuTu` = VALUES(`ThuTu`);

-- ------------------------------------------------------------
-- 3. RENTAL ORDERS - luồng giỏ hàng nhiều xe
-- ------------------------------------------------------------
INSERT INTO `RentalOrders`
(`Id`, `MaDon`, `CustomerId`, `OwnerId`, `HoTenKhach`, `EmailKhach`,
 `SDTKhach`, `SoCCCD`, `SoGPLX`, `DiaChiKhach`, `GhiChu`,
 `TongTien`, `TrangThai`, `PhuongThucThanhToan`,
 `TrangThaiThanhToan`, `MaThamChieuThanhToan`,
 `CreatedAt`, `ConfirmedAt`, `PaidAt`, `UpdatedAt`)
VALUES
('20000000-0000-0000-0000-000000000001', 'RT-DEMO-001',
 '10000000-0000-0000-0000-000000000201',
 '10000000-0000-0000-0000-000000000101',
 'Lê Thu Hà', 'customer1@rental.demo', '0902000201',
 '001204000201', 'B2-20260001', 'Cầu Giấy, Hà Nội',
 'Đơn mẫu đã hoàn thành gồm hai xe máy.',
 2550000, 'HoanThanh', 'ChuyenKhoan', 'DaThanhToan', 'PAY-DEMO-001',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 38 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 37 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 36 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 32 DAY)),
('20000000-0000-0000-0000-000000000002', 'RT-DEMO-002',
 '10000000-0000-0000-0000-000000000202',
 '10000000-0000-0000-0000-000000000101',
 'Phạm Quốc Bảo', 'customer2@rental.demo', '0902000202',
 '001203000202', 'A1-20260002', 'Thanh Xuân, Hà Nội',
 'Đơn tương lai đang chờ chủ xe xác nhận.',
 1050000, 'ChoChuXeXacNhan', NULL, 'ChuaChon', NULL,
 UTC_TIMESTAMP(), NULL, NULL, UTC_TIMESTAMP()),
('20000000-0000-0000-0000-000000000003', 'RT-DEMO-003',
 '10000000-0000-0000-0000-000000000202',
 '10000000-0000-0000-0000-000000000102',
 'Phạm Quốc Bảo', 'customer2@rental.demo', '0902000202',
 '001203000202', 'B2-20260002', 'Thanh Xuân, Hà Nội',
 'Đã thanh toán, chờ chủ xe bàn giao.',
 2400000, 'DaThanhToan', 'ChuyenKhoan', 'DaThanhToan', 'PAY-DEMO-003',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 3 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY),
 UTC_TIMESTAMP()),
('20000000-0000-0000-0000-000000000004', 'RT-DEMO-004',
 '10000000-0000-0000-0000-000000000201',
 '10000000-0000-0000-0000-000000000102',
 'Lê Thu Hà', 'customer1@rental.demo', '0902000201',
 '001204000201', 'B2-20260001', 'Cầu Giấy, Hà Nội',
 'Đơn ô tô đã hoàn thành trong tháng trước.',
 2850000, 'HoanThanh', 'ThanhToanKhiNhanXe', 'DaThanhToan', 'PAY-DEMO-004',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 68 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 67 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 65 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 62 DAY))
ON DUPLICATE KEY UPDATE
    `CustomerId` = VALUES(`CustomerId`),
    `OwnerId` = VALUES(`OwnerId`),
    `HoTenKhach` = VALUES(`HoTenKhach`),
    `EmailKhach` = VALUES(`EmailKhach`),
    `SDTKhach` = VALUES(`SDTKhach`),
    `SoCCCD` = VALUES(`SoCCCD`),
    `SoGPLX` = VALUES(`SoGPLX`),
    `DiaChiKhach` = VALUES(`DiaChiKhach`),
    `GhiChu` = VALUES(`GhiChu`),
    `TongTien` = VALUES(`TongTien`),
    `TrangThai` = VALUES(`TrangThai`),
    `PhuongThucThanhToan` = VALUES(`PhuongThucThanhToan`),
    `TrangThaiThanhToan` = VALUES(`TrangThaiThanhToan`),
    `MaThamChieuThanhToan` = VALUES(`MaThamChieuThanhToan`),
    `ConfirmedAt` = VALUES(`ConfirmedAt`),
    `PaidAt` = VALUES(`PaidAt`),
    `UpdatedAt` = VALUES(`UpdatedAt`);

INSERT INTO `RentalOrderItems`
(`Id`, `RentalOrderId`, `VehicleId`, `TenXe`, `BienSo`, `LoaiXe`,
 `TinhTrangLucDat`, `NgayNhanXe`, `NgayTraXe`,
 `DonGiaNgay`, `SoNgayThue`, `ThanhTien`)
VALUES
('21000000-0000-0000-0000-000000000001',
 '20000000-0000-0000-0000-000000000001',
 '11000000-0000-0000-0000-000000000001',
 'Honda SH 160i 2025', '29A1-123.45', 'Xe tay ga',
 'Sẵn sàng; năm 2025; số vô cấp; xăng',
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 35 DAY) + INTERVAL 8 HOUR,
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 32 DAY) + INTERVAL 8 HOUR,
 450000, 3, 1350000),
('21000000-0000-0000-0000-000000000002',
 '20000000-0000-0000-0000-000000000001',
 '11000000-0000-0000-0000-000000000002',
 'Vespa Sprint 150', '29A1-234.56', 'Xe tay ga',
 'Sẵn sàng; năm 2024; số vô cấp; xăng',
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 35 DAY) + INTERVAL 8 HOUR,
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 32 DAY) + INTERVAL 8 HOUR,
 400000, 3, 1200000),
('21000000-0000-0000-0000-000000000003',
 '20000000-0000-0000-0000-000000000002',
 '11000000-0000-0000-0000-000000000003',
 'Yamaha Grande Hybrid', '29B1-345.67', 'Xe tay ga',
 'Sẵn sàng; năm 2024; số vô cấp; hybrid',
 DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 8 DAY) + INTERVAL 8 HOUR,
 DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 11 DAY) + INTERVAL 8 HOUR,
 350000, 3, 1050000),
('21000000-0000-0000-0000-000000000004',
 '20000000-0000-0000-0000-000000000003',
 '11000000-0000-0000-0000-000000000004',
 'Mazda CX-5 Premium', '30K-456.78', 'Ô tô SUV',
 'Sẵn sàng; năm 2024; số tự động; xăng',
 DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 3 DAY) + INTERVAL 8 HOUR,
 DATE_ADD(DATE(UTC_TIMESTAMP()), INTERVAL 5 DAY) + INTERVAL 8 HOUR,
 1200000, 2, 2400000),
('21000000-0000-0000-0000-000000000005',
 '20000000-0000-0000-0000-000000000004',
 '11000000-0000-0000-0000-000000000005',
 'Toyota Vios G', '30K-567.89', 'Ô tô Sedan',
 'Sẵn sàng; năm 2023; số tự động; xăng',
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 65 DAY) + INTERVAL 8 HOUR,
 DATE_SUB(DATE(UTC_TIMESTAMP()), INTERVAL 62 DAY) + INTERVAL 8 HOUR,
 950000, 3, 2850000)
ON DUPLICATE KEY UPDATE
    `RentalOrderId` = VALUES(`RentalOrderId`),
    `VehicleId` = VALUES(`VehicleId`),
    `TenXe` = VALUES(`TenXe`),
    `BienSo` = VALUES(`BienSo`),
    `LoaiXe` = VALUES(`LoaiXe`),
    `TinhTrangLucDat` = VALUES(`TinhTrangLucDat`),
    `NgayNhanXe` = VALUES(`NgayNhanXe`),
    `NgayTraXe` = VALUES(`NgayTraXe`),
    `DonGiaNgay` = VALUES(`DonGiaNgay`),
    `SoNgayThue` = VALUES(`SoNgayThue`),
    `ThanhTien` = VALUES(`ThanhTien`);

INSERT INTO `RentalOrderContracts`
(`Id`, `RentalOrderId`, `SoHopDong`, `NoiDungHopDong`,
 `DaKyKhach`, `NgayKyKhach`, `DaKyChuXe`, `NgayKyChuXe`, `CreatedAt`)
VALUES
('22000000-0000-0000-0000-000000000001',
 '20000000-0000-0000-0000-000000000001', 'HD-DEMO-001',
 'HỢP ĐỒNG MẪU: Thuê Honda SH 160i và Vespa Sprint. Hai bên đã hoàn thành nghĩa vụ.',
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 37 DAY),
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 37 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 38 DAY)),
('22000000-0000-0000-0000-000000000002',
 '20000000-0000-0000-0000-000000000002', 'HD-DEMO-002',
 'HỢP ĐỒNG MẪU: Thuê Yamaha Grande Hybrid. Đơn đang chờ chủ xe xác nhận.',
 0, NULL, 0, NULL, UTC_TIMESTAMP()),
('22000000-0000-0000-0000-000000000003',
 '20000000-0000-0000-0000-000000000003', 'HD-DEMO-003',
 'HỢP ĐỒNG MẪU: Thuê Mazda CX-5 Premium. Đã thanh toán và chờ bàn giao.',
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 DAY),
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 2 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 3 DAY)),
('22000000-0000-0000-0000-000000000004',
 '20000000-0000-0000-0000-000000000004', 'HD-DEMO-004',
 'HỢP ĐỒNG MẪU: Thuê Toyota Vios G. Đơn đã hoàn thành.',
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 67 DAY),
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 67 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 68 DAY))
ON DUPLICATE KEY UPDATE
    `RentalOrderId` = VALUES(`RentalOrderId`),
    `SoHopDong` = VALUES(`SoHopDong`),
    `NoiDungHopDong` = VALUES(`NoiDungHopDong`),
    `DaKyKhach` = VALUES(`DaKyKhach`),
    `NgayKyKhach` = VALUES(`NgayKyKhach`),
    `DaKyChuXe` = VALUES(`DaKyChuXe`),
    `NgayKyChuXe` = VALUES(`NgayKyChuXe`);

INSERT INTO `RentalOrderPayments`
(`Id`, `RentalOrderId`, `MaGiaoDich`, `PhuongThuc`,
 `TrangThai`, `SoTien`, `GhiChuCongThanhToan`, `CreatedAt`, `PaidAt`)
VALUES
('23000000-0000-0000-0000-000000000001',
 '20000000-0000-0000-0000-000000000001', 'PAY-DEMO-001',
 'ChuyenKhoan', 'DaThanhToan', 2550000,
 'Giao dịch mẫu đã xác nhận.',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 36 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 36 DAY)),
('23000000-0000-0000-0000-000000000003',
 '20000000-0000-0000-0000-000000000003', 'PAY-DEMO-003',
 'ChuyenKhoan', 'DaThanhToan', 2400000,
 'Giao dịch mẫu đã xác nhận qua QR.',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 1 DAY)),
('23000000-0000-0000-0000-000000000004',
 '20000000-0000-0000-0000-000000000004', 'PAY-DEMO-004',
 'ThanhToanKhiNhanXe', 'DaThanhToan', 2850000,
 'Chủ xe xác nhận đã thu tiền mặt.',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 65 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 65 DAY))
ON DUPLICATE KEY UPDATE
    `RentalOrderId` = VALUES(`RentalOrderId`),
    `MaGiaoDich` = VALUES(`MaGiaoDich`),
    `PhuongThuc` = VALUES(`PhuongThuc`),
    `TrangThai` = VALUES(`TrangThai`),
    `SoTien` = VALUES(`SoTien`),
    `GhiChuCongThanhToan` = VALUES(`GhiChuCongThanhToan`),
    `PaidAt` = VALUES(`PaidAt`);

-- ------------------------------------------------------------
-- 4. LEGACY BOOKINGS / PAYMENTS / REVIEWS
-- Dùng để dashboard Admin và phần đánh giá cũ cũng có dữ liệu.
-- ------------------------------------------------------------
INSERT INTO `Bookings`
(`Id`, `VehicleId`, `CustomerId`, `NgayBatDau`, `NgayKetThuc`,
 `GiaThueMotNgayLucDat`, `TongTien`, `TrangThai`,
 `GhiChu`, `LyDoHuy`, `CreatedAt`, `UpdatedAt`)
VALUES
('30000000-0000-0000-0000-000000000001',
 '11000000-0000-0000-0000-000000000006',
 '10000000-0000-0000-0000-000000000201',
 DATE_SUB(CURDATE(), INTERVAL 102 DAY),
 DATE_SUB(CURDATE(), INTERVAL 100 DAY),
 380000, 760000, 'HoanThanh',
 'Booking mẫu xe điện đã hoàn thành.', NULL,
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 105 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 100 DAY)),
('30000000-0000-0000-0000-000000000002',
 '11000000-0000-0000-0000-000000000004',
 '10000000-0000-0000-0000-000000000202',
 DATE_SUB(CURDATE(), INTERVAL 122 DAY),
 DATE_SUB(CURDATE(), INTERVAL 120 DAY),
 1200000, 2400000, 'HoanThanh',
 'Booking mẫu ô tô đã hoàn thành.', NULL,
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 125 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 120 DAY))
ON DUPLICATE KEY UPDATE
    `VehicleId` = VALUES(`VehicleId`),
    `CustomerId` = VALUES(`CustomerId`),
    `NgayBatDau` = VALUES(`NgayBatDau`),
    `NgayKetThuc` = VALUES(`NgayKetThuc`),
    `GiaThueMotNgayLucDat` = VALUES(`GiaThueMotNgayLucDat`),
    `TongTien` = VALUES(`TongTien`),
    `TrangThai` = VALUES(`TrangThai`),
    `GhiChu` = VALUES(`GhiChu`),
    `LyDoHuy` = NULL,
    `UpdatedAt` = VALUES(`UpdatedAt`);

INSERT INTO `Contracts`
(`Id`, `BookingId`, `SoHopDong`, `NoiDungHopDong`,
 `DaKyKhach`, `NgayKyKhach`, `DaKyChuXe`, `NgayKyChuXe`, `CreatedAt`)
VALUES
('31000000-0000-0000-0000-000000000001',
 '30000000-0000-0000-0000-000000000001', 'HD-LEGACY-001',
 'Hợp đồng mẫu booking cũ cho xe VinFast Feliz S.',
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 104 DAY),
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 104 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 105 DAY)),
('31000000-0000-0000-0000-000000000002',
 '30000000-0000-0000-0000-000000000002', 'HD-LEGACY-002',
 'Hợp đồng mẫu booking cũ cho xe Mazda CX-5 Premium.',
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 124 DAY),
 1, DATE_SUB(UTC_TIMESTAMP(), INTERVAL 124 DAY),
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 125 DAY))
ON DUPLICATE KEY UPDATE
    `BookingId` = VALUES(`BookingId`),
    `NoiDungHopDong` = VALUES(`NoiDungHopDong`),
    `DaKyKhach` = 1,
    `DaKyChuXe` = 1;

INSERT INTO `Payments`
(`Id`, `BookingId`, `LoaiThanhToan`, `PhuongThuc`,
 `SoTien`, `MaGiaoDich`, `TrangThai`, `CreatedAt`)
VALUES
('32000000-0000-0000-0000-000000000001',
 '30000000-0000-0000-0000-000000000001',
 'DatCoc', 'ChuyenKhoanNganHang', 228000, 'GD-DEMO-001-A',
 'ThanhCong', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 104 DAY)),
('32000000-0000-0000-0000-000000000002',
 '30000000-0000-0000-0000-000000000001',
 'ThanhToanConLai', 'TienMat', 532000, 'GD-DEMO-001-B',
 'ThanhCong', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 102 DAY)),
('32000000-0000-0000-0000-000000000003',
 '30000000-0000-0000-0000-000000000002',
 'DatCoc', 'ChuyenKhoanNganHang', 720000, 'GD-DEMO-002-A',
 'ThanhCong', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 124 DAY)),
('32000000-0000-0000-0000-000000000004',
 '30000000-0000-0000-0000-000000000002',
 'ThanhToanConLai', 'ChuyenKhoanNganHang', 1680000, 'GD-DEMO-002-B',
 'ThanhCong', DATE_SUB(UTC_TIMESTAMP(), INTERVAL 122 DAY))
ON DUPLICATE KEY UPDATE
    `BookingId` = VALUES(`BookingId`),
    `LoaiThanhToan` = VALUES(`LoaiThanhToan`),
    `PhuongThuc` = VALUES(`PhuongThuc`),
    `SoTien` = VALUES(`SoTien`),
    `TrangThai` = VALUES(`TrangThai`);

INSERT INTO `Reviews`
(`Id`, `BookingId`, `VehicleId`, `CustomerId`,
 `SoSao`, `NhanXet`, `CreatedAt`)
VALUES
('33000000-0000-0000-0000-000000000001',
 '30000000-0000-0000-0000-000000000001',
 '11000000-0000-0000-0000-000000000006',
 '10000000-0000-0000-0000-000000000201',
 5, 'Xe sạch, pin khỏe và chủ xe hỗ trợ rất nhanh.',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 99 DAY)),
('33000000-0000-0000-0000-000000000002',
 '30000000-0000-0000-0000-000000000002',
 '11000000-0000-0000-0000-000000000004',
 '10000000-0000-0000-0000-000000000202',
 4, 'Xe vận hành tốt, thủ tục nhận xe rõ ràng.',
 DATE_SUB(UTC_TIMESTAMP(), INTERVAL 119 DAY))
ON DUPLICATE KEY UPDATE
    `BookingId` = VALUES(`BookingId`),
    `VehicleId` = VALUES(`VehicleId`),
    `CustomerId` = VALUES(`CustomerId`),
    `SoSao` = VALUES(`SoSao`),
    `NhanXet` = VALUES(`NhanXet`);

COMMIT;

-- ------------------------------------------------------------
-- 5. KIỂM TRA NHANH SAU KHI SEED
-- ------------------------------------------------------------
SELECT 'Users' AS `Bang`, COUNT(*) AS `SoLuong` FROM `Users`
UNION ALL
SELECT 'Vehicles', COUNT(*) FROM `Vehicles`
UNION ALL
SELECT 'RentalOrders', COUNT(*) FROM `RentalOrders`
UNION ALL
SELECT 'RentalOrderItems', COUNT(*) FROM `RentalOrderItems`
UNION ALL
SELECT 'RentalOrderPayments', COUNT(*) FROM `RentalOrderPayments`
UNION ALL
SELECT 'Bookings', COUNT(*) FROM `Bookings`
UNION ALL
SELECT 'Reviews', COUNT(*) FROM `Reviews`;
