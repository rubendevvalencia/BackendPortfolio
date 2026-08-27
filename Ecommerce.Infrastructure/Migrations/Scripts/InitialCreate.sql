IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827154330_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [CompanyName] nvarchar(100) NOT NULL,
        [ContactName] nvarchar(50) NULL,
        [ContactTitle] nvarchar(50) NULL,
        [Address] nvarchar(200) NULL,
        [City] nvarchar(50) NULL,
        [Region] nvarchar(50) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Country] nvarchar(50) NULL,
        [Phone] nvarchar(20) NULL,
        [Fax] nvarchar(20) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [LastUpdatedAt] datetime2 NOT NULL,
        [LastUpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827154330_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827154330_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

