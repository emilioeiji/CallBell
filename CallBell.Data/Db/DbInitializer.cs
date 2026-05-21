namespace CallBell.Data.Db;

public static class DbInitializer
{
    public static async Task EnsureCreatedAsync(SqliteConnectionFactory factory, CancellationToken cancellationToken = default)
    {
        await using var connection = factory.CreateOpenConnection();

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = SchemaSql;
        await schemaCommand.ExecuteNonQueryAsync(cancellationToken);

        await EnsureColumnExistsAsync(connection, "AssistanceRequests", "EquipmentId", "INTEGER NULL", cancellationToken);
        await EnsureColumnExistsAsync(connection, "AssistanceRequests", "EquipmentCodeSnapshot", "TEXT NULL", cancellationToken);
        await EnsureColumnExistsAsync(connection, "AssistanceRequests", "EquipmentNamePtSnapshot", "TEXT NULL", cancellationToken);
        await EnsureColumnExistsAsync(connection, "AssistanceRequests", "EquipmentNameJpSnapshot", "TEXT NULL", cancellationToken);

        await SeedData.EnsureSeededAsync(connection, cancellationToken);
    }

    private static async Task EnsureColumnExistsAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        string tableName,
        string columnName,
        string columnSql,
        CancellationToken cancellationToken)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({tableName});";

        var exists = false;
        await using (var reader = await check.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
        {
            return;
        }

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnSql};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS Sectors (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL UNIQUE,
            NamePt TEXT NOT NULL,
            NameJp TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS WorkAreas (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SectorId INTEGER NOT NULL,
            Code TEXT NOT NULL,
            NamePt TEXT NOT NULL,
            NameJp TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (SectorId) REFERENCES Sectors(Id),
            UNIQUE (SectorId, Code)
        );

        CREATE TABLE IF NOT EXISTS Equipments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SectorId INTEGER NOT NULL,
            Code TEXT NOT NULL,
            NamePt TEXT NOT NULL,
            NameJp TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (SectorId) REFERENCES Sectors(Id),
            UNIQUE (SectorId, Code)
        );

        CREATE TABLE IF NOT EXISTS Machines (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SectorId INTEGER NOT NULL,
            WorkAreaId INTEGER NOT NULL,
            Code TEXT NOT NULL,
            NamePt TEXT NOT NULL,
            NameJp TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (SectorId) REFERENCES Sectors(Id),
            FOREIGN KEY (WorkAreaId) REFERENCES WorkAreas(Id),
            UNIQUE (WorkAreaId, Code)
        );

        CREATE TABLE IF NOT EXISTS RequestReasons (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Code TEXT NOT NULL UNIQUE,
            NamePt TEXT NOT NULL,
            NameJp TEXT NOT NULL,
            RequiresMachine INTEGER NOT NULL DEFAULT 0,
            IsActive INTEGER NOT NULL DEFAULT 1,
            SortOrder INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS EquipmentReasonMappings (
            EquipmentId INTEGER NOT NULL,
            ReasonId INTEGER NOT NULL,
            PRIMARY KEY (EquipmentId, ReasonId),
            FOREIGN KEY (EquipmentId) REFERENCES Equipments(Id),
            FOREIGN KEY (ReasonId) REFERENCES RequestReasons(Id)
        );

        CREATE TABLE IF NOT EXISTS AssistanceRequests (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TicketNumber TEXT NOT NULL UNIQUE,
            SectorId INTEGER NOT NULL,
            SectorNamePtSnapshot TEXT NOT NULL,
            SectorNameJpSnapshot TEXT NOT NULL,
            WorkAreaId INTEGER NOT NULL,
            WorkAreaNamePtSnapshot TEXT NOT NULL,
            WorkAreaNameJpSnapshot TEXT NOT NULL,
            EquipmentId INTEGER NULL,
            EquipmentCodeSnapshot TEXT NULL,
            EquipmentNamePtSnapshot TEXT NULL,
            EquipmentNameJpSnapshot TEXT NULL,
            MachineId INTEGER NULL,
            MachineCodeSnapshot TEXT NULL,
            MachineNamePtSnapshot TEXT NULL,
            MachineNameJpSnapshot TEXT NULL,
            ReasonId INTEGER NOT NULL,
            ReasonNamePtSnapshot TEXT NOT NULL,
            ReasonNameJpSnapshot TEXT NOT NULL,
            RequestedByFjCode TEXT NOT NULL,
            RequestedAtUtc TEXT NOT NULL,
            Status INTEGER NOT NULL,
            ClosedByFjCode TEXT NULL,
            ClosedAtUtc TEXT NULL,
            ClosingNote TEXT NULL,
            FOREIGN KEY (SectorId) REFERENCES Sectors(Id),
            FOREIGN KEY (WorkAreaId) REFERENCES WorkAreas(Id),
            FOREIGN KEY (EquipmentId) REFERENCES Equipments(Id),
            FOREIGN KEY (MachineId) REFERENCES Machines(Id),
            FOREIGN KEY (ReasonId) REFERENCES RequestReasons(Id)
        );

        CREATE INDEX IF NOT EXISTS IX_AssistanceRequests_Status_RequestedAt
            ON AssistanceRequests(Status, RequestedAtUtc);

        CREATE INDEX IF NOT EXISTS IX_AssistanceRequests_Sector_Status
            ON AssistanceRequests(SectorId, Status, RequestedAtUtc);
        """;
}
