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
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [AecQualifications] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_AecQualifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [CasePatterns] (
        [Id] int NOT NULL IDENTITY,
        [Pattern] nvarchar(500) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Notes] nvarchar(max) NULL,
        CONSTRAINT [PK_CasePatterns] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [Code] nvarchar(500) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [Manufacturers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Manufacturers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [MountingMethods] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_MountingMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ProductStatuses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_ProductStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [Subcategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(500) NOT NULL,
        [CategoryId] int NOT NULL,
        CONSTRAINT [PK_Subcategories] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Subcategories_Id_CategoryId] UNIQUE ([Id], [CategoryId]),
        CONSTRAINT [FK_Subcategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [Components] (
        [Id] uniqueidentifier NOT NULL,
        [Mpn] nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [ManufacturerId] int NULL,
        [CustomerId] int NULL,
        [NamePl] nvarchar(500) NULL,
        [NameEng] nvarchar(500) NULL,
        [DistributorName] nvarchar(500) NULL,
        [CustomerComponentName] nvarchar(500) NULL,
        [CategoryId] int NULL,
        [SubcategoryId] int NULL,
        [MountingMethodId] int NULL,
        [CaseName] nvarchar(500) NULL,
        [CasePatternId] int NULL,
        [Footprint] nvarchar(500) NULL,
        [Msl] nvarchar(500) NULL,
        [Rohs] bit NULL,
        [ProductStatusId] int NULL,
        [MinimumOperatingTemperatureC] decimal(9,3) NULL,
        [MaximumOperatingTemperatureC] decimal(9,3) NULL,
        [Notes] nvarchar(max) NULL,
        [Critical] bit NULL,
        [AdditionalDescription] nvarchar(max) NULL,
        [LabelDescription] nvarchar(500) NULL,
        CONSTRAINT [PK_Components] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Components_MpnNormalized] CHECK (DATALENGTH([Mpn]) > 0 AND UNICODE(LEFT([Mpn], 1)) NOT IN (9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288) AND UNICODE(RIGHT([Mpn], 1)) NOT IN (9,10,11,12,13,32,133,160,5760,8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,8232,8233,8239,8287,12288)),
        CONSTRAINT [CK_Components_SubcategoryRequiresCategory] CHECK ([SubcategoryId] IS NULL OR [CategoryId] IS NOT NULL),
        CONSTRAINT [CK_Components_TemperatureOrder] CHECK ([MinimumOperatingTemperatureC] IS NULL OR [MaximumOperatingTemperatureC] IS NULL OR [MinimumOperatingTemperatureC] <= [MaximumOperatingTemperatureC]),
        CONSTRAINT [FK_Components_CasePatterns_CasePatternId] FOREIGN KEY ([CasePatternId]) REFERENCES [CasePatterns] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_Manufacturers_ManufacturerId] FOREIGN KEY ([ManufacturerId]) REFERENCES [Manufacturers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_MountingMethods_MountingMethodId] FOREIGN KEY ([MountingMethodId]) REFERENCES [MountingMethods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_ProductStatuses_ProductStatusId] FOREIGN KEY ([ProductStatusId]) REFERENCES [ProductStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Components_Subcategories_SubcategoryId_CategoryId] FOREIGN KEY ([SubcategoryId], [CategoryId]) REFERENCES [Subcategories] ([Id], [CategoryId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [CapacitorParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [CapacitancePf] decimal(28,9) NULL,
        [RatedVoltageV] decimal(18,6) NULL,
        [TolerancePercent] decimal(18,6) NULL,
        [AbsoluteTolerancePf] decimal(28,9) NULL,
        [Dielectric] nvarchar(500) NULL,
        [EsrMilliOhms] decimal(28,9) NULL,
        [LowEsr] bit NULL,
        [LifetimeHours] int NULL,
        CONSTRAINT [PK_CapacitorParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_Capacitor_Lifetime] CHECK ([LifetimeHours] >= 0),
        CONSTRAINT [CK_Capacitor_ToleranceAlternative] CHECK ([TolerancePercent] IS NULL OR [AbsoluteTolerancePf] IS NULL),
        CONSTRAINT [CK_CapacitorParameters_AbsoluteTolerancePf] CHECK ([AbsoluteTolerancePf] >= 0),
        CONSTRAINT [CK_CapacitorParameters_CapacitancePf] CHECK ([CapacitancePf] >= 0),
        CONSTRAINT [CK_CapacitorParameters_EsrMilliOhms] CHECK ([EsrMilliOhms] >= 0),
        CONSTRAINT [CK_CapacitorParameters_RatedVoltageV] CHECK ([RatedVoltageV] >= 0),
        CONSTRAINT [CK_CapacitorParameters_TolerancePercent] CHECK ([TolerancePercent] >= 0),
        CONSTRAINT [FK_CapacitorParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ComponentHistory] (
        [Id] bigint NOT NULL IDENTITY,
        [OperationId] uniqueidentifier NOT NULL,
        [ComponentId] uniqueidentifier NOT NULL,
        [Author] nvarchar(256) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [Operation] int NOT NULL,
        [FieldPath] nvarchar(512) NOT NULL,
        [PreviousValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_ComponentHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ComponentHistory_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ComponentLinks] (
        [Id] uniqueidentifier NOT NULL,
        [ComponentId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [Url] nvarchar(2048) NOT NULL,
        [Description] nvarchar(500) NULL,
        CONSTRAINT [PK_ComponentLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ComponentLinks_Kind] CHECK ([Kind] IN (1, 2)),
        CONSTRAINT [FK_ComponentLinks_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ComponentPads] (
        [ComponentId] uniqueidentifier NOT NULL,
        [RC] int NULL,
        [TT] int NULL,
        [DL] int NULL,
        [QL] int NULL,
        [BGA] int NULL,
        [TP] int NULL,
        [THT] int NULL,
        [PreTHT] int NULL,
        [PressFIT] int NULL,
        [MECH] int NULL,
        [BTC] bit NULL,
        [NS] bit NULL,
        CONSTRAINT [PK_ComponentPads] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_Pads_BGA] CHECK ([BGA] = 0 OR [BGA] >= 3),
        CONSTRAINT [CK_Pads_DL] CHECK ([DL] = 0 OR [DL] >= 3),
        CONSTRAINT [CK_Pads_MECH] CHECK ([MECH] >= 0),
        CONSTRAINT [CK_Pads_PressFIT] CHECK ([PressFIT] >= 0),
        CONSTRAINT [CK_Pads_PreTHT] CHECK ([PreTHT] >= 0),
        CONSTRAINT [CK_Pads_QL] CHECK ([QL] = 0 OR [QL] >= 4),
        CONSTRAINT [CK_Pads_RC] CHECK ([RC] IN (0, 2)),
        CONSTRAINT [CK_Pads_THT] CHECK ([THT] >= 0),
        CONSTRAINT [CK_Pads_TP] CHECK ([TP] >= 0),
        CONSTRAINT [CK_Pads_TT] CHECK ([TT] IN (0, 2)),
        CONSTRAINT [FK_ComponentPads_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ComponentQualifications] (
        [ComponentId] uniqueidentifier NOT NULL,
        [QualificationId] int NOT NULL,
        CONSTRAINT [PK_ComponentQualifications] PRIMARY KEY ([ComponentId], [QualificationId]),
        CONSTRAINT [FK_ComponentQualifications_AecQualifications_QualificationId] FOREIGN KEY ([QualificationId]) REFERENCES [AecQualifications] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ComponentQualifications_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ConnectorParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [Type] nvarchar(500) NULL,
        [PositionCount] int NULL,
        [PitchMm] decimal(18,9) NULL,
        [RowCount] int NULL,
        [CompatiblePlug] nvarchar(500) NULL,
        CONSTRAINT [PK_ConnectorParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_Connector_PositionCount] CHECK ([PositionCount] >= 0),
        CONSTRAINT [CK_Connector_RowCount] CHECK ([RowCount] >= 0),
        CONSTRAINT [CK_ConnectorParameters_PitchMm] CHECK ([PitchMm] >= 0),
        CONSTRAINT [FK_ConnectorParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [CrystalParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [FrequencyHz] decimal(28,6) NULL,
        [LoadCapacitancePf] decimal(28,9) NULL,
        CONSTRAINT [PK_CrystalParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_CrystalParameters_FrequencyHz] CHECK ([FrequencyHz] >= 0),
        CONSTRAINT [CK_CrystalParameters_LoadCapacitancePf] CHECK ([LoadCapacitancePf] >= 0),
        CONSTRAINT [FK_CrystalParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [InductorParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [InductanceMicroHenries] decimal(28,9) NULL,
        [TolerancePercent] decimal(18,6) NULL,
        [RatedCurrentA] decimal(18,9) NULL,
        [DcrOhms] decimal(28,12) NULL,
        [Core] nvarchar(500) NULL,
        [Type] nvarchar(500) NULL,
        CONSTRAINT [PK_InductorParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_InductorParameters_DcrOhms] CHECK ([DcrOhms] >= 0),
        CONSTRAINT [CK_InductorParameters_InductanceMicroHenries] CHECK ([InductanceMicroHenries] >= 0),
        CONSTRAINT [CK_InductorParameters_RatedCurrentA] CHECK ([RatedCurrentA] >= 0),
        CONSTRAINT [CK_InductorParameters_TolerancePercent] CHECK ([TolerancePercent] >= 0),
        CONSTRAINT [FK_InductorParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [LedParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [Color] nvarchar(500) NULL,
        [LuminousIntensityCd] decimal(28,9) NULL,
        [LuminousFluxLm] decimal(28,9) NULL,
        [ForwardCurrentA] decimal(18,9) NULL,
        [ForwardVoltageV] decimal(18,6) NULL,
        [DominantWavelengthNm] decimal(18,6) NULL,
        CONSTRAINT [PK_LedParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_LedParameters_DominantWavelengthNm] CHECK ([DominantWavelengthNm] >= 0),
        CONSTRAINT [CK_LedParameters_ForwardCurrentA] CHECK ([ForwardCurrentA] >= 0),
        CONSTRAINT [CK_LedParameters_ForwardVoltageV] CHECK ([ForwardVoltageV] >= 0),
        CONSTRAINT [CK_LedParameters_LuminousFluxLm] CHECK ([LuminousFluxLm] >= 0),
        CONSTRAINT [CK_LedParameters_LuminousIntensityCd] CHECK ([LuminousIntensityCd] >= 0),
        CONSTRAINT [FK_LedParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE TABLE [ResistorParameters] (
        [ComponentId] uniqueidentifier NOT NULL,
        [ResistanceOhms] decimal(28,12) NULL,
        [TolerancePercent] decimal(18,6) NULL,
        [PowerWatts] decimal(18,9) NULL,
        [TemperatureCoefficientPpmPerC] decimal(18,6) NULL,
        CONSTRAINT [PK_ResistorParameters] PRIMARY KEY ([ComponentId]),
        CONSTRAINT [CK_ResistorParameters_PowerWatts] CHECK ([PowerWatts] >= 0),
        CONSTRAINT [CK_ResistorParameters_ResistanceOhms] CHECK ([ResistanceOhms] >= 0),
        CONSTRAINT [CK_ResistorParameters_TolerancePercent] CHECK ([TolerancePercent] >= 0),
        CONSTRAINT [FK_ResistorParameters_Components_ComponentId] FOREIGN KEY ([ComponentId]) REFERENCES [Components] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[AecQualifications]'))
        SET IDENTITY_INSERT [AecQualifications] ON;
    EXEC(N'INSERT INTO [AecQualifications] ([Id], [Name])
    VALUES (1, N''AEC-Q200''),
    (2, N''AEC-Q101''),
    (3, N''AEC-Q100'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[AecQualifications]'))
        SET IDENTITY_INSERT [AecQualifications] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[MountingMethods]'))
        SET IDENTITY_INSERT [MountingMethods] ON;
    EXEC(N'INSERT INTO [MountingMethods] ([Id], [Name])
    VALUES (1, N''SMT''),
    (2, N''Pin-In-Paste''),
    (3, N''THT''),
    (4, N''Press-Fit''),
    (5, N''Plugs''),
    (6, N''Mech'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[MountingMethods]'))
        SET IDENTITY_INSERT [MountingMethods] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[ProductStatuses]'))
        SET IDENTITY_INSERT [ProductStatuses] ON;
    EXEC(N'INSERT INTO [ProductStatuses] ([Id], [Name])
    VALUES (1, N''Last Time Buy''),
    (2, N''Not For New Design''),
    (3, N''Obsolete''),
    (4, N''End of Life'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Name') AND [object_id] = OBJECT_ID(N'[ProductStatuses]'))
        SET IDENTITY_INSERT [ProductStatuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_AecQualifications_Name] ON [AecQualifications] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_CapacitorParameters_CapacitancePf] ON [CapacitorParameters] ([CapacitancePf]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_CapacitorParameters_RatedVoltageV] ON [CapacitorParameters] ([RatedVoltageV]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ComponentHistory_ComponentId_OccurredAtUtc] ON [ComponentHistory] ([ComponentId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ComponentHistory_OperationId] ON [ComponentHistory] ([OperationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ComponentLinks_ComponentId] ON [ComponentLinks] ([ComponentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ComponentQualifications_QualificationId] ON [ComponentQualifications] ([QualificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_CasePatternId] ON [Components] ([CasePatternId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_CategoryId] ON [Components] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_CustomerId] ON [Components] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_ManufacturerId] ON [Components] ([ManufacturerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_MountingMethodId] ON [Components] ([MountingMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_ProductStatusId] ON [Components] ([ProductStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Components_SubcategoryId_CategoryId] ON [Components] ([SubcategoryId], [CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Components_BusinessKey] ON [Components] ([Mpn], [ManufacturerId], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ConnectorParameters_PitchMm] ON [ConnectorParameters] ([PitchMm]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ConnectorParameters_PositionCount] ON [ConnectorParameters] ([PositionCount]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_CrystalParameters_FrequencyHz] ON [CrystalParameters] ([FrequencyHz]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Customers_Name] ON [Customers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_InductorParameters_InductanceMicroHenries] ON [InductorParameters] ([InductanceMicroHenries]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_InductorParameters_RatedCurrentA] ON [InductorParameters] ([RatedCurrentA]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_LedParameters_Color] ON [LedParameters] ([Color]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Manufacturers_Name] ON [Manufacturers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_MountingMethods_Name] ON [MountingMethods] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ProductStatuses_Name] ON [ProductStatuses] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ResistorParameters_PowerWatts] ON [ResistorParameters] ([PowerWatts]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ResistorParameters_ResistanceOhms] ON [ResistorParameters] ([ResistanceOhms]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_ResistorParameters_TolerancePercent] ON [ResistorParameters] ([TolerancePercent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Subcategories_CategoryId] ON [Subcategories] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    CREATE INDEX [IX_Subcategories_Name] ON [Subcategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125347_InitialComponentModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113125347_InitialComponentModel', N'10.0.15');
END;

COMMIT;
GO

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

