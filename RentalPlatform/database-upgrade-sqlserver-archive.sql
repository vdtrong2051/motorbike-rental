BEGIN TRANSACTION;
CREATE TABLE [RentalOrders] (
    [Id] uniqueidentifier NOT NULL,
    [MaDon] nvarchar(30) NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [OwnerId] uniqueidentifier NOT NULL,
    [HoTenKhach] nvarchar(100) NOT NULL,
    [EmailKhach] nvarchar(150) NOT NULL,
    [SDTKhach] nvarchar(15) NOT NULL,
    [SoCCCD] nvarchar(20) NOT NULL,
    [SoGPLX] nvarchar(30) NOT NULL,
    [DiaChiKhach] nvarchar(300) NOT NULL,
    [GhiChu] nvarchar(500) NULL,
    [TongTien] decimal(18,2) NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    [PhuongThucThanhToan] nvarchar(max) NULL,
    [TrangThaiThanhToan] nvarchar(max) NOT NULL,
    [MaThamChieuThanhToan] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ConfirmedAt] datetime2 NULL,
    [PaidAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_RentalOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RentalOrders_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RentalOrders_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RentalOrderContracts] (
    [Id] uniqueidentifier NOT NULL,
    [RentalOrderId] uniqueidentifier NOT NULL,
    [SoHopDong] nvarchar(30) NOT NULL,
    [NoiDungHopDong] nvarchar(max) NOT NULL,
    [DaKyKhach] bit NOT NULL,
    [NgayKyKhach] datetime2 NULL,
    [DaKyChuXe] bit NOT NULL,
    [NgayKyChuXe] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RentalOrderContracts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RentalOrderContracts_RentalOrders_RentalOrderId] FOREIGN KEY ([RentalOrderId]) REFERENCES [RentalOrders] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RentalOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [RentalOrderId] uniqueidentifier NOT NULL,
    [VehicleId] uniqueidentifier NOT NULL,
    [TenXe] nvarchar(100) NOT NULL,
    [BienSo] nvarchar(15) NOT NULL,
    [LoaiXe] nvarchar(50) NOT NULL,
    [TinhTrangLucDat] nvarchar(200) NOT NULL,
    [NgayNhanXe] datetime2 NOT NULL,
    [NgayTraXe] datetime2 NOT NULL,
    [DonGiaNgay] decimal(18,2) NOT NULL,
    [SoNgayThue] int NOT NULL,
    [ThanhTien] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_RentalOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RentalOrderItems_RentalOrders_RentalOrderId] FOREIGN KEY ([RentalOrderId]) REFERENCES [RentalOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RentalOrderItems_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [RentalOrderPayments] (
    [Id] uniqueidentifier NOT NULL,
    [RentalOrderId] uniqueidentifier NOT NULL,
    [MaGiaoDich] nvarchar(60) NOT NULL,
    [PhuongThuc] nvarchar(max) NOT NULL,
    [TrangThai] nvarchar(max) NOT NULL,
    [SoTien] decimal(18,2) NOT NULL,
    [GhiChuCongThanhToan] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PaidAt] datetime2 NULL,
    CONSTRAINT [PK_RentalOrderPayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RentalOrderPayments_RentalOrders_RentalOrderId] FOREIGN KEY ([RentalOrderId]) REFERENCES [RentalOrders] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RentalOrderContracts_RentalOrderId] ON [RentalOrderContracts] ([RentalOrderId]);

CREATE UNIQUE INDEX [IX_RentalOrderContracts_SoHopDong] ON [RentalOrderContracts] ([SoHopDong]);

CREATE INDEX [IX_RentalOrderItems_RentalOrderId] ON [RentalOrderItems] ([RentalOrderId]);

CREATE INDEX [IX_RentalOrderItems_VehicleId_NgayNhanXe_NgayTraXe] ON [RentalOrderItems] ([VehicleId], [NgayNhanXe], [NgayTraXe]);

CREATE UNIQUE INDEX [IX_RentalOrderPayments_MaGiaoDich] ON [RentalOrderPayments] ([MaGiaoDich]);

CREATE INDEX [IX_RentalOrderPayments_RentalOrderId] ON [RentalOrderPayments] ([RentalOrderId]);

CREATE INDEX [IX_RentalOrders_CustomerId_CreatedAt] ON [RentalOrders] ([CustomerId], [CreatedAt]);

CREATE UNIQUE INDEX [IX_RentalOrders_MaDon] ON [RentalOrders] ([MaDon]);

CREATE INDEX [IX_RentalOrders_OwnerId_CreatedAt] ON [RentalOrders] ([OwnerId], [CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260807094358_MultiVehicleRentalFlow', N'10.0.9');

COMMIT;
GO
