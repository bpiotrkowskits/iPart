BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE TABLE [Packages] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [CustomerRevision] nvarchar(50) NOT NULL,
        [CustomerId] int NULL,
        [Description] nvarchar(1000) NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [CreatedBy] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_Packages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Packages_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE TABLE [BomRevisions] (
        [Id] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [SourceFileName] nvarchar(260) NOT NULL,
        [SourceSheet] nvarchar(100) NULL,
        [ImportedAtUtc] datetimeoffset NOT NULL,
        [ImportedBy] nvarchar(256) NOT NULL,
        [Comment] nvarchar(1000) NULL,
        CONSTRAINT [PK_BomRevisions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BomRevisions_Number] CHECK ([Number] >= 1),
        CONSTRAINT [FK_BomRevisions_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [Packages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE TABLE [BomLines] (
        [Id] uniqueidentifier NOT NULL,
        [BomRevisionId] uniqueidentifier NOT NULL,
        [LineNumber] int NOT NULL,
        [Designators] nvarchar(max) NULL,
        [Quantity] int NULL,
        [PartTs] nvarchar(500) NULL,
        [Footprint] nvarchar(500) NULL,
        [Description] nvarchar(2000) NULL,
        [Value] nvarchar(500) NULL,
        [PartNumber] nvarchar(1000) NULL,
        [Notes] nvarchar(4000) NULL,
        [Confirmed] bit NOT NULL,
        CONSTRAINT [PK_BomLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BomLines_LineNumber] CHECK ([LineNumber] >= 1),
        CONSTRAINT [CK_BomLines_Quantity] CHECK ([Quantity] >= 0),
        CONSTRAINT [FK_BomLines_BomRevisions_BomRevisionId] FOREIGN KEY ([BomRevisionId]) REFERENCES [BomRevisions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE TABLE [BomLineDesignators] (
        [BomLineId] uniqueidentifier NOT NULL,
        [Designator] nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [BomRevisionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_BomLineDesignators] PRIMARY KEY ([BomLineId], [Designator]),
        CONSTRAINT [FK_BomLineDesignators_BomLines_BomLineId] FOREIGN KEY ([BomLineId]) REFERENCES [BomLines] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BomLineDesignators_BomRevisions_BomRevisionId] FOREIGN KEY ([BomRevisionId]) REFERENCES [BomRevisions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE TABLE [BomLineExtraValues] (
        [BomLineId] uniqueidentifier NOT NULL,
        [Ordinal] int NOT NULL,
        [Header] nvarchar(500) NULL,
        [Value] nvarchar(4000) NOT NULL,
        CONSTRAINT [PK_BomLineExtraValues] PRIMARY KEY ([BomLineId], [Ordinal]),
        CONSTRAINT [CK_BomLineExtraValues_Ordinal] CHECK ([Ordinal] >= 1),
        CONSTRAINT [FK_BomLineExtraValues_BomLines_BomLineId] FOREIGN KEY ([BomLineId]) REFERENCES [BomLines] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BomLineDesignators_BomRevisionId_Designator] ON [BomLineDesignators] ([BomRevisionId], [Designator]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BomLines_BomRevisionId_LineNumber] ON [BomLines] ([BomRevisionId], [LineNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BomRevisions_PackageId_Number] ON [BomRevisions] ([PackageId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE INDEX [IX_Packages_CustomerId] ON [Packages] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Packages_CodeRevisionCustomer] ON [Packages] ([Code], [CustomerRevision], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123163413_AddPackagesAndBom'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123163413_AddPackagesAndBom', N'10.0.15');
END;

COMMIT;
GO

