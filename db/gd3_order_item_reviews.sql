-- GD3 additive schema. Run ONLY after taking and verifying a backup.
-- No Booking/Review/RentalOrder/Vehicle/User rows are modified.
-- For MySQL 8+, with the RentalPlatform baseline schema already installed.
CREATE TABLE IF NOT EXISTS `RentalOrderItemReviews` (
    `Id` char(36) NOT NULL,
    `RentalOrderItemId` char(36) NOT NULL,
    `VehicleId` char(36) NOT NULL,
    `CustomerId` char(36) NOT NULL,
    `SoSao` int NOT NULL,
    `NhanXet` varchar(1000) NULL,
    `IsVisible` tinyint(1) NOT NULL DEFAULT 1,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_RentalOrderItemReviews_RentalOrderItemId` (`RentalOrderItemId`),
    KEY `IX_RentalOrderItemReviews_VehicleId_IsVisible` (`VehicleId`, `IsVisible`),
    KEY `IX_RentalOrderItemReviews_CustomerId` (`CustomerId`),
    CONSTRAINT `FK_GD3_Reviews_Items` FOREIGN KEY (`RentalOrderItemId`) REFERENCES `RentalOrderItems` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_GD3_Reviews_Vehicles` FOREIGN KEY (`VehicleId`) REFERENCES `Vehicles` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_GD3_Reviews_Customers` FOREIGN KEY (`CustomerId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `CK_GD3_Reviews_Stars` CHECK (`SoSao` BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
