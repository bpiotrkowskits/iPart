BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [dataPart].[Components] GROUP BY [Mpn], [ManufacturerId] HAVING COUNT(*) > 1)
        THROW 50001, N'Etap 1: w [dataPart].[Components] ten sam MPN i producent występuje kilka razy (różni Klienci). Scal te rekordy przed migracją.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [dataPart].[Components] DROP CONSTRAINT [FK_Components_Customers_CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP INDEX [IX_Customers_Name] ON [dataPart].[Customers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP INDEX [IX_Components_CustomerId] ON [dataPart].[Components];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP INDEX [UX_Components_BusinessKey] ON [dataPart].[Components];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dataPart].[Components]') AND [c].[name] = N'CustomerId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [dataPart].[Components] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [dataPart].[Components] DROP COLUMN [CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF SCHEMA_ID(N'components') IS NULL EXEC(N'CREATE SCHEMA [components];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF SCHEMA_ID(N'documentation') IS NULL EXEC(N'CREATE SCHEMA [documentation];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF SCHEMA_ID(N'shared') IS NULL EXEC(N'CREATE SCHEMA [shared];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[Subcategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ResistorParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ProductStatuses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [documentation] TRANSFER [dataPart].[Packages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[MountingMethods];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[Manufacturers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[LedParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[InductorParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [shared] TRANSFER [dataPart].[Customers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[CrystalParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ConnectorParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[Components];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ComponentQualifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ComponentPads];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[ComponentLinks];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[Categories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[CasePatterns];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[CapacitorParameters];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [documentation] TRANSFER [dataPart].[BomRevisions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [documentation] TRANSFER [dataPart].[BomLines];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [documentation] TRANSFER [dataPart].[BomLineExtraValues];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [documentation] TRANSFER [dataPart].[BomLineDesignators];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER SCHEMA [components] TRANSFER [dataPart].[AecQualifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[Packages] DROP CONSTRAINT [FK_Packages_Customers_CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP INDEX [UX_Packages_CodeRevisionCustomer] ON [documentation].[Packages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP INDEX [IX_Packages_CustomerId] ON [documentation].[Packages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [shared].[Customers] ADD [NewId] uniqueidentifier NOT NULL CONSTRAINT [DF_Customers_NewId] DEFAULT NEWID();
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[Packages] ADD [NewCustomerId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE p SET p.[NewCustomerId] = c.[NewId]
    FROM [documentation].[Packages] p JOIN [shared].[Customers] c ON c.[Id] = p.[CustomerId];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[Packages] DROP COLUMN [CustomerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC sp_rename N'[documentation].[Packages].[NewCustomerId]', N'CustomerId', N'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [shared].[Customers] DROP CONSTRAINT [PK_Customers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [shared].[Customers] DROP CONSTRAINT [DF_Customers_NewId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [shared].[Customers] DROP COLUMN [Id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC sp_rename N'[shared].[Customers].[NewId]', N'Id', N'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [shared].[Customers] ADD CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_Packages_CustomerId] ON [documentation].[Packages] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Packages_CodeRevisionCustomer] ON [documentation].[Packages] ([Code], [CustomerRevision], [CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[Packages] ADD CONSTRAINT [FK_Packages_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [shared].[Customers] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [components].[MountingMethods] ADD [Code] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [SourceFileId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [components].[ChangeSets] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Author] nvarchar(256) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [SubjectType] nvarchar(100) NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Operation] nvarchar(50) NOT NULL,
        [Comment] nvarchar(4000) NULL,
        CONSTRAINT [PK_ChangeSets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [documentation].[ChangeSets] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Author] nvarchar(256) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [SubjectType] nvarchar(100) NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Operation] nvarchar(50) NOT NULL,
        [Comment] nvarchar(4000) NULL,
        CONSTRAINT [PK_ChangeSets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[ChangeSets] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Author] nvarchar(256) NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [SubjectType] nvarchar(100) NOT NULL,
        [SubjectId] uniqueidentifier NOT NULL,
        [Operation] nvarchar(50) NOT NULL,
        [Comment] nvarchar(4000) NULL,
        CONSTRAINT [PK_ChangeSets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [documentation].[Files] (
        [Id] uniqueidentifier NOT NULL,
        [FileName] nvarchar(260) NOT NULL,
        [ContentType] nvarchar(200) NULL,
        [SizeBytes] bigint NOT NULL,
        [Sha256] char(64) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [UploadedByUserId] uniqueidentifier NULL,
        [UploadedBy] nvarchar(256) NOT NULL,
        [UploadedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Files] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Files_Size] CHECK ([SizeBytes] >= 0 AND [SizeBytes] = DATALENGTH([Content]))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[Groups] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [AccountName] nvarchar(256) NOT NULL,
        [DisplayName] nvarchar(256) NULL,
        [Active] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [components].[FieldChanges] (
        [Id] bigint NOT NULL IDENTITY,
        [ChangeSetId] uniqueidentifier NOT NULL,
        [ObjectType] nvarchar(100) NOT NULL,
        [ObjectId] uniqueidentifier NOT NULL,
        [Field] nvarchar(512) NOT NULL,
        [PreviousValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_FieldChanges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FieldChanges_ChangeSets_ChangeSetId] FOREIGN KEY ([ChangeSetId]) REFERENCES [components].[ChangeSets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [documentation].[FieldChanges] (
        [Id] bigint NOT NULL IDENTITY,
        [ChangeSetId] uniqueidentifier NOT NULL,
        [ObjectType] nvarchar(100) NOT NULL,
        [ObjectId] uniqueidentifier NOT NULL,
        [Field] nvarchar(512) NOT NULL,
        [PreviousValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_FieldChanges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FieldChanges_ChangeSets_ChangeSetId] FOREIGN KEY ([ChangeSetId]) REFERENCES [documentation].[ChangeSets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[FieldChanges] (
        [Id] bigint NOT NULL IDENTITY,
        [ChangeSetId] uniqueidentifier NOT NULL,
        [ObjectType] nvarchar(100) NOT NULL,
        [ObjectId] uniqueidentifier NOT NULL,
        [Field] nvarchar(512) NOT NULL,
        [PreviousValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        CONSTRAINT [PK_FieldChanges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FieldChanges_ChangeSets_ChangeSetId] FOREIGN KEY ([ChangeSetId]) REFERENCES [shared].[ChangeSets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[GroupMembers] (
        [GroupId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [IsAdministrator] bit NOT NULL,
        CONSTRAINT [PK_GroupMembers] PRIMARY KEY ([GroupId], [UserId]),
        CONSTRAINT [FK_GroupMembers_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [shared].[Groups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_GroupMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [shared].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE TABLE [shared].[UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleCode] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleCode]),
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [shared].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[shared].[Groups]'))
        SET IDENTITY_INSERT [shared].[Groups] ON;
    EXEC(N'INSERT INTO [shared].[Groups] ([Id], [Code], [Name])
    VALUES (''6f1c0b2e-0001-4d6a-9a51-6b1f0c2a0001'', N''OBSLUGA_PROJEKTU'', N''Obsługa Projektu''),
    (''6f1c0b2e-0002-4d6a-9a51-6b1f0c2a0002'', N''TECHNOLOGIA_SMT'', N''Technologia SMT''),
    (''6f1c0b2e-0003-4d6a-9a51-6b1f0c2a0003'', N''ZAKUPY'', N''Zakupy''),
    (''6f1c0b2e-0004-4d6a-9a51-6b1f0c2a0004'', N''MAGAZYN'', N''Magazyn''),
    (''6f1c0b2e-0005-4d6a-9a51-6b1f0c2a0005'', N''PRODUKCJA'', N''Produkcja''),
    (''6f1c0b2e-0006-4d6a-9a51-6b1f0c2a0006'', N''OPIEKUNOWIE_KLIENTA'', N''Opiekunowie klienta'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[shared].[Groups]'))
        SET IDENTITY_INSERT [shared].[Groups] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''SMT''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''PIN_IN_PASTE''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''THT''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''PRESS_FIT''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''PLUGS''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    EXEC(N'UPDATE [components].[MountingMethods] SET [Code] = N''MECH''
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[components].[MountingMethods]'))
        SET IDENTITY_INSERT [components].[MountingMethods] ON;
    EXEC(N'INSERT INTO [components].[MountingMethods] ([Id], [Code], [Name])
    VALUES (7, N''PCB'', N''PCB''),
    (8, N''DNP'', N''DNP'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Name') AND [object_id] = OBJECT_ID(N'[components].[MountingMethods]'))
        SET IDENTITY_INSERT [components].[MountingMethods] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    UPDATE [components].[MountingMethods] SET [Code] = CONCAT(N'M', [Id]) WHERE [Code] = N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MountingMethods_Code] ON [components].[MountingMethods] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_Name] ON [shared].[Customers] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Components_BusinessKey] ON [components].[Components] ([Mpn], [ManufacturerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_BomRevisions_SourceFileId] ON [documentation].[BomRevisions] ([SourceFileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc] ON [components].[ChangeSets] ([SubjectType], [SubjectId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc] ON [documentation].[ChangeSets] ([SubjectType], [SubjectId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_ChangeSets_SubjectType_SubjectId_OccurredAtUtc] ON [shared].[ChangeSets] ([SubjectType], [SubjectId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ChangeSetId] ON [components].[FieldChanges] ([ChangeSetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ObjectType_ObjectId] ON [components].[FieldChanges] ([ObjectType], [ObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ChangeSetId] ON [documentation].[FieldChanges] ([ChangeSetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ObjectType_ObjectId] ON [documentation].[FieldChanges] ([ObjectType], [ObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ChangeSetId] ON [shared].[FieldChanges] ([ChangeSetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_FieldChanges_ObjectType_ObjectId] ON [shared].[FieldChanges] ([ObjectType], [ObjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_Files_Sha256] ON [documentation].[Files] ([Sha256]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE INDEX [IX_GroupMembers_UserId] ON [shared].[GroupMembers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Groups_Code] ON [shared].[Groups] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_AccountName] ON [shared].[Users] ([AccountName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD CONSTRAINT [FK_BomRevisions_Files_SourceFileId] FOREIGN KEY ([SourceFileId]) REFERENCES [documentation].[Files] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    INSERT INTO [components].[ChangeSets] ([Id], [UserId], [Author], [OccurredAtUtc], [SubjectType], [SubjectId], [Operation], [Comment])
    SELECT h.[OperationId], NULL, MIN(h.[Author]), MIN(h.[OccurredAtUtc]), N'Component', MIN(h.[ComponentId]),
           CASE MIN(h.[Operation]) WHEN 1 THEN N'Created' ELSE N'Updated' END, NULL
    FROM [dataPart].[ComponentHistory] h
    GROUP BY h.[OperationId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    INSERT INTO [components].[FieldChanges] ([ChangeSetId], [ObjectType], [ObjectId], [Field], [PreviousValue], [NewValue])
    SELECT h.[OperationId], N'Component', h.[ComponentId], h.[FieldPath], h.[PreviousValue], h.[NewValue]
    FROM [dataPart].[ComponentHistory] h
    ORDER BY h.[Id];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    DROP TABLE [dataPart].[ComponentHistory];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.objects o JOIN sys.schemas s ON s.schema_id = o.schema_id WHERE s.name = N'dataPart')
        DROP SCHEMA [dataPart];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126121809_Etap1Fundament'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126121809_Etap1Fundament', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    DROP INDEX [IX_Users_AccountName] ON [shared].[Users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[shared].[Users]') AND [c].[name] = N'Active');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [shared].[Users] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [shared].[Users] DROP COLUMN [Active];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    EXEC sp_rename N'[shared].[Users].[CreatedAtUtc]', N'LastLoginAtUtc', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    EXEC sp_rename N'[shared].[Users].[AccountName]', N'Login', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    ALTER TABLE [shared].[Users] ADD [DepartmentId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    ALTER TABLE [shared].[Users] ADD [Email] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    ALTER TABLE [shared].[Users] ADD [FirstLoginAtUtc] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    CREATE INDEX [IX_Users_Login] ON [shared].[Users] ([Login]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126123355_Etap1KontaNexo'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126123355_Etap1KontaNexo', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126130119_Etap1UprawnieniaKonta'
)
BEGIN
    DROP TABLE [shared].[UserRoles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126130119_Etap1UprawnieniaKonta'
)
BEGIN
    CREATE TABLE [shared].[UserAccess] (
        [UserId] uniqueidentifier NOT NULL,
        [PermissionsJson] nvarchar(max) NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [ChangedByUserId] uniqueidentifier NULL,
        [ChangedBy] nvarchar(256) NOT NULL,
        [ChangedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_UserAccess] PRIMARY KEY ([UserId]),
        CONSTRAINT [CK_UserAccess_Json] CHECK (ISJSON([PermissionsJson]) = 1),
        CONSTRAINT [FK_UserAccess_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [shared].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126130119_Etap1UprawnieniaKonta'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126130119_Etap1UprawnieniaKonta', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    ALTER TABLE [shared].[GroupMembers] DROP CONSTRAINT [FK_GroupMembers_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    ALTER TABLE [shared].[UserAccess] DROP CONSTRAINT [FK_UserAccess_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    DROP TABLE [shared].[Users];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    DROP INDEX [IX_GroupMembers_UserId] ON [shared].[GroupMembers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    IF SCHEMA_ID(N'produman') IS NULL EXEC(N'CREATE SCHEMA [produman];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    IF SCHEMA_ID(N'employees') IS NULL EXEC(N'CREATE SCHEMA [employees];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    CREATE TABLE [produman].[users] (
        [UserId] uniqueidentifier NOT NULL CONSTRAINT [PK_produman_users] PRIMARY KEY,
        [Login] nvarchar(256) NULL,
        [FullName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [Department] int NULL,
        [IsActive] bit NOT NULL,
        [Deleted] bit NOT NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    CREATE TABLE [employees].[Departments] (
        [Id] int NOT NULL CONSTRAINT [PK_employees_Departments] PRIMARY KEY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    INSERT INTO [employees].[Departments] ([Id], [Code], [Name], [IsActive], [SortOrder]) VALUES
    (100100, N'MG', N'Magazyn', 1, 10), (100101, N'SMT', N'SMT', 1, 20), (100102, N'THT', N'THT', 1, 30),
    (100103, N'KJ', N'Kontrola jakości', 1, 40), (100104, N'TSL', N'TS Labs', 1, 50), (100105, N'SZ', N'Szablony', 1, 60),
    (100106, N'RD', N'R&D', 1, 70), (100107, N'IT', N'IT', 1, 80), (100108, N'ZAK', N'Zakupy', 1, 90),
    (100109, N'OP', N'Opiekunowie', 1, 100), (100111, N'WDR', N'Wdrożenia', 0, 110), (100113, N'DOK', N'Dokumentacja', 1, 120),
    (200001, N'JAK', N'Jakość', 1, 130), (200002, N'TECH', N'Dział techniczny', 1, 140), (200003, N'ZAR', N'Zarząd', 1, 150),
    (200004, N'ADM', N'Administracja', 1, 160), (200005, N'PROD', N'Kierownictwo produkcji', 1, 170);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144152_PilotKopieProduMana'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126144152_PilotKopieProduMana', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ConfirmationBasis] varchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ConfirmationNote] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ConfirmedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ConfirmedBy] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ConfirmedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    EXEC(N'ALTER TABLE [documentation].[BomLines] ADD CONSTRAINT [CK_BomLines_ConfirmationBasis] CHECK ([Confirmed] = 0 OR [ConfirmationBasis] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261126144938_PilotPotwierdzenieLinii'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261126144938_PilotPotwierdzenieLinii', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [PnpApprovedAtUtc] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [PnpApprovedBy] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [PnpApprovedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [PnpFileId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [PnpFileName] nvarchar(260) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [ComparisonDecision] varchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [CustomerAnswer] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [DifferenceQuestion] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [MountingCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE TABLE [documentation].[BomVersions] (
        [Id] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [BomRevisionId] uniqueidentifier NOT NULL,
        [Kind] varchar(20) NOT NULL,
        [OrderNumber] nvarchar(20) NULL,
        [Comment] nvarchar(4000) NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedBy] nvarchar(256) NOT NULL,
        [ApprovedAtUtc] datetimeoffset NOT NULL,
        [LinesJson] nvarchar(max) NOT NULL,
        [LineCount] int NOT NULL,
        [UnconfirmedCount] int NOT NULL,
        CONSTRAINT [PK_BomVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_BomVersions_Kind] CHECK ([Kind] IN ('BAZOWA', 'DLA_ZK') AND ([Kind] = 'BAZOWA' OR [OrderNumber] IS NOT NULL)),
        CONSTRAINT [FK_BomVersions_BomRevisions_BomRevisionId] FOREIGN KEY ([BomRevisionId]) REFERENCES [documentation].[BomRevisions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BomVersions_Packages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [documentation].[Packages] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE TABLE [documentation].[LineMpns] (
        [Id] uniqueidentifier NOT NULL,
        [BomLineId] uniqueidentifier NOT NULL,
        [Mpn] nvarchar(200) NOT NULL,
        [Forbidden] bit NOT NULL,
        [Note] nvarchar(1000) NOT NULL,
        [Ordinal] int NOT NULL,
        [AddedByUserId] uniqueidentifier NULL,
        [AddedBy] nvarchar(256) NOT NULL,
        [AddedAtUtc] datetimeoffset NOT NULL,
        CONSTRAINT [PK_LineMpns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LineMpns_BomLines_BomLineId] FOREIGN KEY ([BomLineId]) REFERENCES [documentation].[BomLines] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE TABLE [documentation].[PnpRows] (
        [Id] uniqueidentifier NOT NULL,
        [BomRevisionId] uniqueidentifier NOT NULL,
        [SourceRow] int NOT NULL,
        [Designator] nvarchar(100) NOT NULL,
        [X] nvarchar(50) NULL,
        [Y] nvarchar(50) NULL,
        [Rotation] nvarchar(50) NULL,
        [Side] nvarchar(50) NULL,
        [Footprint] nvarchar(500) NULL,
        [Value] nvarchar(500) NULL,
        CONSTRAINT [PK_PnpRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PnpRows_BomRevisions_BomRevisionId] FOREIGN KEY ([BomRevisionId]) REFERENCES [documentation].[BomRevisions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE INDEX [IX_BomRevisions_PnpFileId] ON [documentation].[BomRevisions] ([PnpFileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE INDEX [IX_BomVersions_BomRevisionId] ON [documentation].[BomVersions] ([BomRevisionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BomVersions_PackageId_Number] ON [documentation].[BomVersions] ([PackageId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LineMpns_BomLineId_Mpn_Forbidden] ON [documentation].[LineMpns] ([BomLineId], [Mpn], [Forbidden]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PnpRows_BomRevisionId_Designator] ON [documentation].[PnpRows] ([BomRevisionId], [Designator]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD CONSTRAINT [FK_BomRevisions_Files_PnpFileId] FOREIGN KEY ([PnpFileId]) REFERENCES [documentation].[Files] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127084012_PilotProcesBom'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127084012_PilotProcesBom', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127091654_PilotWspolrzedneMm'
)
BEGIN
    ALTER TABLE [documentation].[PnpRows] ADD [XMm] decimal(12,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127091654_PilotWspolrzedneMm'
)
BEGIN
    ALTER TABLE [documentation].[PnpRows] ADD [YMm] decimal(12,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127091654_PilotWspolrzedneMm'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127091654_PilotWspolrzedneMm', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127095120_PilotRozstrzygniecieLinii'
)
BEGIN
    ALTER TABLE [documentation].[PnpRows] ADD [Description] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127095120_PilotRozstrzygniecieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [TsDescription] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127095120_PilotRozstrzygniecieLinii'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [TsFootprint] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127095120_PilotRozstrzygniecieLinii'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127095120_PilotRozstrzygniecieLinii', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127135206_PilotComparator'
)
BEGIN
    ALTER TABLE [documentation].[BomRevisions] ADD [CompareFields] varchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127135206_PilotComparator'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [SplitFromLineNumber] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127135206_PilotComparator'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [TsValue] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127135206_PilotComparator'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127135206_PilotComparator', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127151113_PilotPartNumber'
)
BEGIN
    ALTER TABLE [documentation].[PnpRows] ADD [PartNumber] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127151113_PilotPartNumber'
)
BEGIN
    ALTER TABLE [documentation].[BomLines] ADD [TsPartNumber] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261127151113_PilotPartNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261127151113_PilotPartNumber', N'10.0.15');
END;

COMMIT;
GO

