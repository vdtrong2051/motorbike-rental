-- A failing assertion raises SQLSTATE 45000 and makes batch mysql exit nonzero.
USE `RentalPlatformDemo`;
DROP PROCEDURE IF EXISTS `__AssertRentalDemo`;
DELIMITER $$
CREATE PROCEDURE `__AssertRentalDemo`()
BEGIN
    IF DATABASE() <> 'RentalPlatformDemo' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Unsafe demo database name';
    END IF;
    IF (SELECT COUNT(*) FROM `Users`) <> 6 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected 6 demo accounts';
    END IF;
    IF (SELECT COUNT(*) FROM `Users` WHERE `Role` = 'Admin' AND `Email` = 'admin@rentalplatform.vn') <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Default development admin missing';
    END IF;
    IF (SELECT COUNT(*) FROM `Users` WHERE `Role` = 'Owner') <> 2 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected 2 owners';
    END IF;
    IF (SELECT COUNT(*) FROM `Users` WHERE `Role` = 'Customer') <> 3 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected 3 customers';
    END IF;
    IF (SELECT COUNT(*) FROM `Vehicles`) <> 12 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected 12 vehicles';
    END IF;
    IF (SELECT COUNT(*) FROM `Vehicles` WHERE `TrangThai` = 'SanSang') <> 4
       OR (SELECT COUNT(*) FROM `Vehicles` WHERE `TrangThai` = 'ChoDuyet') <> 2
       OR (SELECT COUNT(*) FROM `Vehicles` WHERE `TrangThai` = 'DangThue') <> 2
       OR (SELECT COUNT(*) FROM `Vehicles` WHERE `TrangThai` = 'BaoTri') <> 2
       OR (SELECT COUNT(*) FROM `Vehicles` WHERE `TrangThai` = 'TuChoi') <> 2 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Vehicle status coverage incorrect';
    END IF;
    IF (SELECT COUNT(*) FROM `VehicleImages`) <> 12 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected one image per vehicle';
    END IF;
    IF (SELECT COUNT(*) FROM `RentalOrders`) <> 8
       OR (SELECT COUNT(*) FROM `RentalOrderItems`) <> 9
       OR (SELECT COUNT(*) FROM `RentalOrderPayments`) <> 5
       OR (SELECT COUNT(*) FROM `RentalOrderItemReviews`) <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Rental scenarios incomplete';
    END IF;
    IF (SELECT COUNT(DISTINCT `TrangThai`) FROM `RentalOrders`) <> 7 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Missing order status scenario';
    END IF;
    IF (SELECT COUNT(*) FROM `Vehicles` v LEFT JOIN `Users` u ON u.`Id`=v.`OwnerId`
        WHERE u.`Id` IS NULL OR u.`Role` <> 'Owner') <> 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Broken vehicle-owner relation';
    END IF;
    IF (SELECT COUNT(*) FROM `RentalOrders` o JOIN `RentalOrderItems` i ON i.`RentalOrderId`=o.`Id`
        JOIN `Vehicles` v ON v.`Id`=i.`VehicleId` WHERE v.`OwnerId`<>o.`OwnerId`) <> 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Order contains vehicle of another owner';
    END IF;
    IF (SELECT COUNT(*) FROM `RentalOrders` o WHERE o.`TrangThai`='DangThue') <> 2 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Expected 2 active rental orders';
    END IF;
END$$
DELIMITER ;
CALL `__AssertRentalDemo`();
DROP PROCEDURE `__AssertRentalDemo`;
SELECT 'PASS: 6 accounts, 12 vehicles, all 5 vehicle states, 8 orders, all 7 order states' AS `DemoSeedVerification`;
SELECT `Role`, COUNT(*) AS `Count` FROM `Users` GROUP BY `Role` ORDER BY `Role`;
SELECT `TrangThai`, COUNT(*) AS `Count` FROM `Vehicles` GROUP BY `TrangThai` ORDER BY `TrangThai`;
SELECT `TrangThai`, COUNT(*) AS `Count` FROM `RentalOrders` GROUP BY `TrangThai` ORDER BY `TrangThai`;
