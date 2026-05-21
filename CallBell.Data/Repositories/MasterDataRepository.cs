using CallBell.Core.Entities;
using CallBell.Core.Models;
using CallBell.Data.Db;
using Microsoft.Data.Sqlite;

namespace CallBell.Data.Repositories;

public sealed class MasterDataRepository
{
    private readonly SqliteConnectionFactory _factory;

    public MasterDataRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<CatalogSnapshot> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var sectors = await GetSectorsAsync(cancellationToken);
        var areas = await GetWorkAreasAsync(null, cancellationToken);
        var equipments = await GetEquipmentsAsync(null, cancellationToken);
        var machines = await GetMachinesAsync(null, null, cancellationToken);
        var reasons = await GetReasonsAsync(cancellationToken);
        var equipmentReasonMappings = await GetEquipmentReasonMappingsAsync(cancellationToken);

        return new CatalogSnapshot
        {
            Sectors = sectors,
            WorkAreas = areas,
            Equipments = equipments,
            Machines = machines,
            Reasons = reasons,
            EquipmentReasonMappings = equipmentReasonMappings
        };
    }

    public async Task<IReadOnlyList<Sector>> GetSectorsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Code, NamePt, NameJp, IsActive, SortOrder
            FROM Sectors
            ORDER BY IsActive DESC, SortOrder, NamePt;
            """;

        var results = new List<Sector>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Sector
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                NamePt = reader.GetString(2),
                NameJp = reader.GetString(3),
                IsActive = reader.GetInt32(4) == 1,
                SortOrder = reader.GetInt32(5)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<WorkArea>> GetWorkAreasAsync(int? sectorId, CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SectorId, Code, NamePt, NameJp, IsActive, SortOrder
            FROM WorkAreas
            WHERE (@sectorId IS NULL OR SectorId = @sectorId)
            ORDER BY IsActive DESC, SectorId, SortOrder, NamePt;
            """;
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);

        var results = new List<WorkArea>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new WorkArea
            {
                Id = reader.GetInt32(0),
                SectorId = reader.GetInt32(1),
                Code = reader.GetString(2),
                NamePt = reader.GetString(3),
                NameJp = reader.GetString(4),
                IsActive = reader.GetInt32(5) == 1,
                SortOrder = reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<Machine>> GetMachinesAsync(int? sectorId, int? workAreaId, CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SectorId, WorkAreaId, Code, NamePt, NameJp, IsActive, SortOrder
            FROM Machines
            WHERE (@sectorId IS NULL OR SectorId = @sectorId)
              AND (@workAreaId IS NULL OR WorkAreaId = @workAreaId)
            ORDER BY IsActive DESC, SectorId, WorkAreaId, SortOrder, Code;
            """;
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);
        command.Parameters.AddWithValue("@workAreaId", workAreaId is null ? DBNull.Value : workAreaId.Value);

        var results = new List<Machine>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Machine
            {
                Id = reader.GetInt32(0),
                SectorId = reader.GetInt32(1),
                WorkAreaId = reader.GetInt32(2),
                Code = reader.GetString(3),
                NamePt = reader.GetString(4),
                NameJp = reader.GetString(5),
                IsActive = reader.GetInt32(6) == 1,
                SortOrder = reader.GetInt32(7)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<Equipment>> GetEquipmentsAsync(int? sectorId, CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SectorId, Code, NamePt, NameJp, IsActive, SortOrder
            FROM Equipments
            WHERE (@sectorId IS NULL OR SectorId = @sectorId)
            ORDER BY IsActive DESC, SectorId, SortOrder, Code;
            """;
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);

        var results = new List<Equipment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Equipment
            {
                Id = reader.GetInt32(0),
                SectorId = reader.GetInt32(1),
                Code = reader.GetString(2),
                NamePt = reader.GetString(3),
                NameJp = reader.GetString(4),
                IsActive = reader.GetInt32(5) == 1,
                SortOrder = reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<RequestReason>> GetReasonsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Code, NamePt, NameJp, RequiresMachine, IsActive, SortOrder
            FROM RequestReasons
            ORDER BY IsActive DESC, SortOrder, NamePt;
            """;

        var results = new List<RequestReason>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new RequestReason
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                NamePt = reader.GetString(2),
                NameJp = reader.GetString(3),
                RequiresMachine = reader.GetInt32(4) == 1,
                IsActive = reader.GetInt32(5) == 1,
                SortOrder = reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<EquipmentReasonMapping>> GetEquipmentReasonMappingsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EquipmentId, ReasonId
            FROM EquipmentReasonMappings
            ORDER BY EquipmentId, ReasonId;
            """;

        var results = new List<EquipmentReasonMapping>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new EquipmentReasonMapping
            {
                EquipmentId = reader.GetInt32(0),
                ReasonId = reader.GetInt32(1)
            });
        }

        return results;
    }

    public async Task SaveSectorsAsync(IEnumerable<Sector> sectors, CancellationToken cancellationToken = default)
    {
        await SaveAsync(
            sectors,
            """
            INSERT INTO Sectors (Id, Code, NamePt, NameJp, IsActive, SortOrder)
            VALUES (@Id, @Code, @NamePt, @NameJp, @IsActive, @SortOrder)
            ON CONFLICT(Id) DO UPDATE SET
                Code = excluded.Code,
                NamePt = excluded.NamePt,
                NameJp = excluded.NameJp,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder;
            """,
            static (command, item) =>
            {
                command.Parameters.AddWithValue("@Id", item.Id == 0 ? DBNull.Value : item.Id);
                command.Parameters.AddWithValue("@Code", item.Code);
                command.Parameters.AddWithValue("@NamePt", item.NamePt);
                command.Parameters.AddWithValue("@NameJp", item.NameJp);
                command.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            },
            cancellationToken);
    }

    public async Task SaveWorkAreasAsync(IEnumerable<WorkArea> areas, CancellationToken cancellationToken = default)
    {
        await SaveAsync(
            areas,
            """
            INSERT INTO WorkAreas (Id, SectorId, Code, NamePt, NameJp, IsActive, SortOrder)
            VALUES (@Id, @SectorId, @Code, @NamePt, @NameJp, @IsActive, @SortOrder)
            ON CONFLICT(Id) DO UPDATE SET
                SectorId = excluded.SectorId,
                Code = excluded.Code,
                NamePt = excluded.NamePt,
                NameJp = excluded.NameJp,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder;
            """,
            static (command, item) =>
            {
                command.Parameters.AddWithValue("@Id", item.Id == 0 ? DBNull.Value : item.Id);
                command.Parameters.AddWithValue("@SectorId", item.SectorId);
                command.Parameters.AddWithValue("@Code", item.Code);
                command.Parameters.AddWithValue("@NamePt", item.NamePt);
                command.Parameters.AddWithValue("@NameJp", item.NameJp);
                command.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            },
            cancellationToken);
    }

    public async Task SaveMachinesAsync(IEnumerable<Machine> machines, CancellationToken cancellationToken = default)
    {
        await SaveAsync(
            machines,
            """
            INSERT INTO Machines (Id, SectorId, WorkAreaId, Code, NamePt, NameJp, IsActive, SortOrder)
            VALUES (@Id, @SectorId, @WorkAreaId, @Code, @NamePt, @NameJp, @IsActive, @SortOrder)
            ON CONFLICT(Id) DO UPDATE SET
                SectorId = excluded.SectorId,
                WorkAreaId = excluded.WorkAreaId,
                Code = excluded.Code,
                NamePt = excluded.NamePt,
                NameJp = excluded.NameJp,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder;
            """,
            static (command, item) =>
            {
                command.Parameters.AddWithValue("@Id", item.Id == 0 ? DBNull.Value : item.Id);
                command.Parameters.AddWithValue("@SectorId", item.SectorId);
                command.Parameters.AddWithValue("@WorkAreaId", item.WorkAreaId);
                command.Parameters.AddWithValue("@Code", item.Code);
                command.Parameters.AddWithValue("@NamePt", item.NamePt);
                command.Parameters.AddWithValue("@NameJp", item.NameJp);
                command.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            },
            cancellationToken);
    }

    public async Task SaveEquipmentsAsync(IEnumerable<Equipment> equipments, CancellationToken cancellationToken = default)
    {
        await SaveAsync(
            equipments,
            """
            INSERT INTO Equipments (Id, SectorId, Code, NamePt, NameJp, IsActive, SortOrder)
            VALUES (@Id, @SectorId, @Code, @NamePt, @NameJp, @IsActive, @SortOrder)
            ON CONFLICT(Id) DO UPDATE SET
                SectorId = excluded.SectorId,
                Code = excluded.Code,
                NamePt = excluded.NamePt,
                NameJp = excluded.NameJp,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder;
            """,
            static (command, item) =>
            {
                command.Parameters.AddWithValue("@Id", item.Id == 0 ? DBNull.Value : item.Id);
                command.Parameters.AddWithValue("@SectorId", item.SectorId);
                command.Parameters.AddWithValue("@Code", item.Code);
                command.Parameters.AddWithValue("@NamePt", item.NamePt);
                command.Parameters.AddWithValue("@NameJp", item.NameJp);
                command.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            },
            cancellationToken);
    }

    public async Task SaveReasonsAsync(IEnumerable<RequestReason> reasons, CancellationToken cancellationToken = default)
    {
        await SaveAsync(
            reasons,
            """
            INSERT INTO RequestReasons (Id, Code, NamePt, NameJp, RequiresMachine, IsActive, SortOrder)
            VALUES (@Id, @Code, @NamePt, @NameJp, @RequiresMachine, @IsActive, @SortOrder)
            ON CONFLICT(Id) DO UPDATE SET
                Code = excluded.Code,
                NamePt = excluded.NamePt,
                NameJp = excluded.NameJp,
                RequiresMachine = excluded.RequiresMachine,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder;
            """,
            static (command, item) =>
            {
                command.Parameters.AddWithValue("@Id", item.Id == 0 ? DBNull.Value : item.Id);
                command.Parameters.AddWithValue("@Code", item.Code);
                command.Parameters.AddWithValue("@NamePt", item.NamePt);
                command.Parameters.AddWithValue("@NameJp", item.NameJp);
                command.Parameters.AddWithValue("@RequiresMachine", item.RequiresMachine ? 1 : 0);
                command.Parameters.AddWithValue("@IsActive", item.IsActive ? 1 : 0);
                command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            },
            cancellationToken);
    }

    public async Task ReplaceEquipmentReasonMappingsAsync(IEnumerable<EquipmentReasonMapping> mappings, CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM EquipmentReasonMappings;";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var mapping in mappings
                     .Where(x => x.EquipmentId > 0 && x.ReasonId > 0)
                     .GroupBy(x => new { x.EquipmentId, x.ReasonId })
                     .Select(x => x.First()))
        {
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO EquipmentReasonMappings (EquipmentId, ReasonId)
                VALUES (@EquipmentId, @ReasonId);
                """;
            insert.Parameters.AddWithValue("@EquipmentId", mapping.EquipmentId);
            insert.Parameters.AddWithValue("@ReasonId", mapping.ReasonId);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task SaveAsync<T>(
        IEnumerable<T> items,
        string sql,
        Action<SqliteCommand, T> parameterize,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var item in items)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            parameterize(command, item);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
