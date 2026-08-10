START TRANSACTION;
ALTER TABLE `Vehicles` ADD `ApprovedAt` datetime(6) NULL;

ALTER TABLE `Vehicles` ADD `HangXe` varchar(100) NULL;

ALTER TABLE `Users` ADD `BiTamKhoa` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Users` ADD `GhiChuXacThuc` varchar(500) NULL;

ALTER TABLE `Users` ADD `GioiTinh` varchar(20) NULL;

ALTER TABLE `Users` ADD `GuiXacThucAt` datetime(6) NULL;

ALTER TABLE `Users` ADD `NgaySinh` date NULL;

ALTER TABLE `Users` ADD `OAuthProvider` varchar(30) NULL;

ALTER TABLE `Users` ADD `OAuthSubject` varchar(200) NULL;

ALTER TABLE `Users` ADD `SoLanViPham` int NOT NULL DEFAULT 0;

ALTER TABLE `Users` ADD `TamKhoaDen` datetime(6) NULL;

ALTER TABLE `Users` ADD `TrangThaiXacThuc` longtext NULL;

UPDATE `Users` SET `TrangThaiXacThuc` = 'ChuaGui' WHERE `TrangThaiXacThuc` IS NULL OR `TrangThaiXacThuc` = '';

ALTER TABLE `Users` MODIFY `TrangThaiXacThuc` longtext NOT NULL;

ALTER TABLE `Users` ADD `XacThucAt` datetime(6) NULL;

ALTER TABLE `RentalOrders` ADD `CancelledAt` datetime(6) NULL;

ALTER TABLE `RentalOrders` ADD `LyDoHuy` varchar(500) NULL;

ALTER TABLE `RentalOrders` ADD `PhiDichVu` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `RentalOrders` ADD `SoTienHoanDuKien` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `RentalOrders` ADD `TienDatCoc` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `RentalOrders` ADD `TienThue` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `RentalOrders` ADD `TyLeHoanTien` decimal(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `RentalOrderPayments` ADD `GhiChuDoiSoat` varchar(500) NULL;

ALTER TABLE `RentalOrderPayments` ADD `ReconciledAt` datetime(6) NULL;

CREATE TABLE `SystemPolicies` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TyLeDatCoc` decimal(5,2) NOT NULL,
    `TyLePhiDichVu` decimal(5,2) NOT NULL,
    `HoanTienTruoc72Gio` decimal(5,2) NOT NULL,
    `HoanTienTruoc24Gio` decimal(5,2) NOT NULL,
    `HoanTienTrong24Gio` decimal(5,2) NOT NULL,
    `PhatTraMuonMoiGio` decimal(18,2) NOT NULL,
    `QuyDinhViPham` varchar(4000) NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

UPDATE `RentalOrders` SET `TienThue` = `TongTien` WHERE `TienThue` = 0;

INSERT INTO `SystemPolicies` (`Id`, `TyLeDatCoc`, `TyLePhiDichVu`, `HoanTienTruoc72Gio`, `HoanTienTruoc24Gio`, `HoanTienTrong24Gio`, `PhatTraMuonMoiGio`, `QuyDinhViPham`, `UpdatedAt`)
VALUES (1, 20.0, 5.0, 100.0, 70.0, 30.0, 100000.0, 'Tuân thủ hợp đồng, giao nhận đúng hẹn và bảo quản phương tiện.', '2026-08-10 17:18:06.588469');
SELECT ROW_COUNT();


CREATE TABLE `ViolationReports` (
    `Id` char(36) NOT NULL,
    `ReporterId` char(36) NOT NULL,
    `ReportedUserId` char(36) NOT NULL,
    `RentalOrderId` char(36) NULL,
    `LoaiViPham` varchar(100) NOT NULL,
    `MoTa` varchar(2000) NOT NULL,
    `BangChungUrl` varchar(500) NULL,
    `TrangThai` varchar(255) NOT NULL,
    `HinhThucXuLy` longtext NOT NULL,
    `GhiChuAdmin` varchar(2000) NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `ResolvedAt` datetime(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ViolationReports_RentalOrders_RentalOrderId` FOREIGN KEY (`RentalOrderId`) REFERENCES `RentalOrders` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_ViolationReports_Users_ReportedUserId` FOREIGN KEY (`ReportedUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ViolationReports_Users_ReporterId` FOREIGN KEY (`ReporterId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX `IX_Users_OAuthProvider_OAuthSubject` ON `Users` (`OAuthProvider`, `OAuthSubject`);

CREATE INDEX `IX_ViolationReports_RentalOrderId` ON `ViolationReports` (`RentalOrderId`);

CREATE INDEX `IX_ViolationReports_ReportedUserId` ON `ViolationReports` (`ReportedUserId`);

CREATE INDEX `IX_ViolationReports_ReporterId` ON `ViolationReports` (`ReporterId`);

CREATE INDEX `IX_ViolationReports_TrangThai_CreatedAt` ON `ViolationReports` (`TrangThai`, `CreatedAt`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260810161953_AdvancedPlatformFeatures', '10.0.9');

COMMIT;
