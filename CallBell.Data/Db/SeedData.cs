using Microsoft.Data.Sqlite;

namespace CallBell.Data.Db;

internal static class SeedData
{
    public static async Task EnsureSeededAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await SeedSectorsAsync(connection, transaction, cancellationToken);
        await SeedWorkAreasAsync(connection, transaction, cancellationToken);
        await SeedEquipmentsAsync(connection, transaction, cancellationToken);
        await SeedMachinesAsync(connection, transaction, cancellationToken);
        await SeedReasonsAsync(connection, transaction, cancellationToken);
        await SeedEquipmentReasonMappingsAsync(connection, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task SeedSectorsAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "Sectors", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction,
            "INSERT INTO Sectors (Code, NamePt, NameJp, SortOrder) VALUES ('SETOR1', 'Setor 1', '第1工程', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Sectors (Code, NamePt, NameJp, SortOrder) VALUES ('SETOR2', 'Setor 2', '第2工程', 2);");
    }

    private static async Task SeedWorkAreasAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "WorkAreas", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction,
            "INSERT INTO WorkAreas (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (1, 'AREA1', 'Area 1', 'エリア1', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO WorkAreas (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (1, 'AREA2', 'Area 2', 'エリア2', 2);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO WorkAreas (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (2, 'AREA3', 'Area 3', 'エリア3', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO WorkAreas (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (2, 'AREA4', 'Area 4', 'エリア4', 2);");
    }

    private static async Task SeedEquipmentsAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "Equipments", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction,
            "INSERT INTO Equipments (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (1, 'EQ-MAT', 'Abastecimento', '補給', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Equipments (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (1, 'EQ-SYS', 'Sistema', 'システム', 2);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Equipments (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (1, 'EQ-MAQ', 'Maquinas', '設備', 3);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Equipments (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (2, 'EQ-MAT', 'Abastecimento', '補給', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Equipments (SectorId, Code, NamePt, NameJp, SortOrder) VALUES (2, 'EQ-MAQ', 'Maquinas', '設備', 2);");
    }

    private static async Task SeedMachinesAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "Machines", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction,
            "INSERT INTO Machines (SectorId, WorkAreaId, Code, NamePt, NameJp, SortOrder) VALUES (1, 1, 'MC-101', 'Maquina 101', '設備101', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Machines (SectorId, WorkAreaId, Code, NamePt, NameJp, SortOrder) VALUES (1, 1, 'MC-102', 'Maquina 102', '設備102', 2);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Machines (SectorId, WorkAreaId, Code, NamePt, NameJp, SortOrder) VALUES (1, 2, 'MC-201', 'Maquina 201', '設備201', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Machines (SectorId, WorkAreaId, Code, NamePt, NameJp, SortOrder) VALUES (2, 3, 'MC-301', 'Maquina 301', '設備301', 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO Machines (SectorId, WorkAreaId, Code, NamePt, NameJp, SortOrder) VALUES (2, 4, 'MC-401', 'Maquina 401', '設備401', 1);");
    }

    private static async Task SeedReasonsAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "RequestReasons", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction,
            "INSERT INTO RequestReasons (Code, NamePt, NameJp, RequiresMachine, SortOrder) VALUES ('FALTA_PECA', 'Falta de peca', '部品不足', 0, 1);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO RequestReasons (Code, NamePt, NameJp, RequiresMachine, SortOrder) VALUES ('PROBLEMA_SISTEMA', 'Problema no sistema', 'システム異常', 0, 2);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO RequestReasons (Code, NamePt, NameJp, RequiresMachine, SortOrder) VALUES ('PROBLEMA_MAQUINA', 'Problema na maquina', '設備トラブル', 1, 3);");
        await ExecuteAsync(connection, transaction,
            "INSERT INTO RequestReasons (Code, NamePt, NameJp, RequiresMachine, SortOrder) VALUES ('AJUDA_LIDER', 'Chamar lider', 'リーダー呼出', 0, 4);");
    }

    private static async Task SeedEquipmentReasonMappingsAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        if (await HasRowsAsync(connection, transaction, "EquipmentReasonMappings", cancellationToken))
        {
            return;
        }

        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (1, 1);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (1, 4);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (2, 2);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (2, 4);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (3, 3);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (3, 4);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (4, 1);");
        await ExecuteAsync(connection, transaction, "INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId) VALUES (5, 3);");
    }

    private static async Task<bool> HasRowsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {tableName} LIMIT 1);";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
