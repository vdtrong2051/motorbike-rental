START TRANSACTION;
ALTER TABLE `Users` ADD `AnhCCCDMatSau` varchar(260) NULL;

ALTER TABLE `Users` ADD `AnhCCCDMatTruoc` varchar(260) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260810171403_AddIdentityDocumentImages', '10.0.9');

COMMIT;
