CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `Users` (
    `Id` char(36) NOT NULL,
    `HoTen` varchar(100) NOT NULL,
    `Email` varchar(150) NOT NULL,
    `SDT` varchar(15) NULL,
    `PasswordHash` longtext NOT NULL,
    `Role` longtext NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `RentalOrders` (
    `Id` char(36) NOT NULL,
    `MaDon` varchar(30) NOT NULL,
    `CustomerId` char(36) NOT NULL,
    `OwnerId` char(36) NOT NULL,
    `HoTenKhach` varchar(100) NOT NULL,
    `EmailKhach` varchar(150) NOT NULL,
    `SDTKhach` varchar(15) NOT NULL,
    `SoCCCD` varchar(20) NOT NULL,
    `SoGPLX` varchar(30) NOT NULL,
    `DiaChiKhach` varchar(300) NOT NULL,
    `GhiChu` varchar(500) NULL,
    `TongTien` decimal(18,2) NOT NULL,
    `TrangThai` longtext NOT NULL,
    `PhuongThucThanhToan` longtext NULL,
    `TrangThaiThanhToan` longtext NOT NULL,
    `MaThamChieuThanhToan` varchar(100) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ConfirmedAt` datetime(6) NULL,
    `PaidAt` datetime(6) NULL,
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RentalOrders_Users_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RentalOrders_Users_OwnerId` FOREIGN KEY (`OwnerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `Vehicles` (
    `Id` char(36) NOT NULL,
    `OwnerId` char(36) NOT NULL,
    `TenXe` varchar(100) NOT NULL,
    `LoaiXe` varchar(50) NOT NULL,
    `BienSo` varchar(15) NOT NULL,
    `SoChoNgoi` int NOT NULL,
    `HopSo` longtext NOT NULL,
    `NhienLieu` longtext NOT NULL,
    `Nam` int NOT NULL,
    `KhuVuc` varchar(100) NOT NULL,
    `DiaChiCuThe` varchar(250) NULL,
    `GiaThueMotNgay` decimal(18,2) NOT NULL,
    `MoTa` varchar(2000) NULL,
    `TrangThai` varchar(255) NOT NULL,
    `LyDoTuChoi` varchar(300) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Vehicles_Users_OwnerId` FOREIGN KEY (`OwnerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `RentalOrderContracts` (
    `Id` char(36) NOT NULL,
    `RentalOrderId` char(36) NOT NULL,
    `SoHopDong` varchar(30) NOT NULL,
    `NoiDungHopDong` longtext NOT NULL,
    `DaKyKhach` tinyint(1) NOT NULL,
    `NgayKyKhach` datetime(6) NULL,
    `DaKyChuXe` tinyint(1) NOT NULL,
    `NgayKyChuXe` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RentalOrderContracts_RentalOrders_RentalOrderId` FOREIGN KEY (`RentalOrderId`) REFERENCES `RentalOrders` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `RentalOrderPayments` (
    `Id` char(36) NOT NULL,
    `RentalOrderId` char(36) NOT NULL,
    `MaGiaoDich` varchar(60) NOT NULL,
    `PhuongThuc` longtext NOT NULL,
    `TrangThai` longtext NOT NULL,
    `SoTien` decimal(18,2) NOT NULL,
    `GhiChuCongThanhToan` varchar(200) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `PaidAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RentalOrderPayments_RentalOrders_RentalOrderId` FOREIGN KEY (`RentalOrderId`) REFERENCES `RentalOrders` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Bookings` (
    `Id` char(36) NOT NULL,
    `VehicleId` char(36) NOT NULL,
    `CustomerId` char(36) NOT NULL,
    `NgayBatDau` date NOT NULL,
    `NgayKetThuc` date NOT NULL,
    `GiaThueMotNgayLucDat` decimal(18,2) NOT NULL,
    `TongTien` decimal(18,2) NOT NULL,
    `TrangThai` varchar(255) NOT NULL,
    `GhiChu` varchar(500) NULL,
    `LyDoHuy` varchar(300) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Bookings_Users_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Bookings_Vehicles_VehicleId` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `RentalOrderItems` (
    `Id` char(36) NOT NULL,
    `RentalOrderId` char(36) NOT NULL,
    `VehicleId` char(36) NOT NULL,
    `TenXe` varchar(100) NOT NULL,
    `BienSo` varchar(15) NOT NULL,
    `LoaiXe` varchar(50) NOT NULL,
    `TinhTrangLucDat` varchar(200) NOT NULL,
    `NgayNhanXe` datetime(6) NOT NULL,
    `NgayTraXe` datetime(6) NOT NULL,
    `DonGiaNgay` decimal(18,2) NOT NULL,
    `SoNgayThue` int NOT NULL,
    `ThanhTien` decimal(18,2) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_RentalOrderItems_RentalOrders_RentalOrderId` FOREIGN KEY (`RentalOrderId`) REFERENCES `RentalOrders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RentalOrderItems_Vehicles_VehicleId` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE RESTRICT
);

CREATE TABLE `VehicleImages` (
    `Id` char(36) NOT NULL,
    `VehicleId` char(36) NOT NULL,
    `DuongDanAnh` varchar(2000) NOT NULL,
    `LaAnhDaiDien` tinyint(1) NOT NULL,
    `ThuTu` int NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_VehicleImages_Vehicles_VehicleId` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Contracts` (
    `Id` char(36) NOT NULL,
    `BookingId` char(36) NOT NULL,
    `SoHopDong` varchar(30) NOT NULL,
    `NoiDungHopDong` longtext NOT NULL,
    `DaKyKhach` tinyint(1) NOT NULL,
    `NgayKyKhach` datetime(6) NULL,
    `DaKyChuXe` tinyint(1) NOT NULL,
    `NgayKyChuXe` datetime(6) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Contracts_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Payments` (
    `Id` char(36) NOT NULL,
    `BookingId` char(36) NOT NULL,
    `LoaiThanhToan` longtext NOT NULL,
    `PhuongThuc` longtext NOT NULL,
    `SoTien` decimal(18,2) NOT NULL,
    `MaGiaoDich` varchar(50) NOT NULL,
    `TrangThai` longtext NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Payments_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE CASCADE
);

CREATE TABLE `Reviews` (
    `Id` char(36) NOT NULL,
    `BookingId` char(36) NOT NULL,
    `VehicleId` char(36) NOT NULL,
    `CustomerId` char(36) NOT NULL,
    `SoSao` int NOT NULL,
    `NhanXet` varchar(1000) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Reviews_Bookings_BookingId` FOREIGN KEY (`BookingId`) REFERENCES `Bookings` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Reviews_Users_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reviews_Vehicles_VehicleId` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_Bookings_CustomerId` ON `Bookings` (`CustomerId`);

CREATE INDEX `IX_Bookings_TrangThai` ON `Bookings` (`TrangThai`);

CREATE INDEX `IX_Bookings_VehicleId_NgayBatDau_NgayKetThuc` ON `Bookings` (`VehicleId`, `NgayBatDau`, `NgayKetThuc`);

CREATE UNIQUE INDEX `IX_Contracts_BookingId` ON `Contracts` (`BookingId`);

CREATE UNIQUE INDEX `IX_Contracts_SoHopDong` ON `Contracts` (`SoHopDong`);

CREATE INDEX `IX_Payments_BookingId` ON `Payments` (`BookingId`);

CREATE UNIQUE INDEX `IX_Payments_MaGiaoDich` ON `Payments` (`MaGiaoDich`);

CREATE UNIQUE INDEX `IX_RentalOrderContracts_RentalOrderId` ON `RentalOrderContracts` (`RentalOrderId`);

CREATE UNIQUE INDEX `IX_RentalOrderContracts_SoHopDong` ON `RentalOrderContracts` (`SoHopDong`);

CREATE INDEX `IX_RentalOrderItems_RentalOrderId` ON `RentalOrderItems` (`RentalOrderId`);

CREATE INDEX `IX_RentalOrderItems_VehicleId_NgayNhanXe_NgayTraXe` ON `RentalOrderItems` (`VehicleId`, `NgayNhanXe`, `NgayTraXe`);

CREATE UNIQUE INDEX `IX_RentalOrderPayments_MaGiaoDich` ON `RentalOrderPayments` (`MaGiaoDich`);

CREATE INDEX `IX_RentalOrderPayments_RentalOrderId` ON `RentalOrderPayments` (`RentalOrderId`);

CREATE INDEX `IX_RentalOrders_CustomerId_CreatedAt` ON `RentalOrders` (`CustomerId`, `CreatedAt`);

CREATE UNIQUE INDEX `IX_RentalOrders_MaDon` ON `RentalOrders` (`MaDon`);

CREATE INDEX `IX_RentalOrders_OwnerId_CreatedAt` ON `RentalOrders` (`OwnerId`, `CreatedAt`);

CREATE UNIQUE INDEX `IX_Reviews_BookingId` ON `Reviews` (`BookingId`);

CREATE INDEX `IX_Reviews_CustomerId` ON `Reviews` (`CustomerId`);

CREATE INDEX `IX_Reviews_VehicleId` ON `Reviews` (`VehicleId`);

CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

CREATE INDEX `IX_VehicleImages_VehicleId` ON `VehicleImages` (`VehicleId`);

CREATE UNIQUE INDEX `IX_Vehicles_BienSo` ON `Vehicles` (`BienSo`);

CREATE INDEX `IX_Vehicles_GiaThueMotNgay` ON `Vehicles` (`GiaThueMotNgay`);

CREATE INDEX `IX_Vehicles_KhuVuc` ON `Vehicles` (`KhuVuc`);

CREATE INDEX `IX_Vehicles_LoaiXe` ON `Vehicles` (`LoaiXe`);

CREATE INDEX `IX_Vehicles_OwnerId` ON `Vehicles` (`OwnerId`);

CREATE INDEX `IX_Vehicles_TrangThai` ON `Vehicles` (`TrangThai`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260807143030_InitialMySql', '10.0.9');

COMMIT;
