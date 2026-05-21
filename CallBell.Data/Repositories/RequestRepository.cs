using CallBell.Core.Entities;
using CallBell.Core.Enums;
using CallBell.Core.Models;
using CallBell.Core.Validation;
using CallBell.Data.Db;
using Microsoft.Data.Sqlite;

namespace CallBell.Data.Repositories;

public sealed class RequestRepository
{
    private readonly SqliteConnectionFactory _factory;

    public RequestRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<AssistanceRequest> CreateAsync(CreateAssistanceRequestCommand command, CancellationToken cancellationToken = default)
    {
        FjCode.EnsureValid(command.RequestedByFjCode, "FJ do operador");

        await using var connection = _factory.CreateOpenConnection();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var snapshot = await LoadSnapshotAsync(connection, transaction, command, cancellationToken);
        var ticketNumber = $"CB-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        var requestedBy = FjCode.Normalize(command.RequestedByFjCode);
        var requestedAtUtc = DateTimeOffset.UtcNow;

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO AssistanceRequests (
                TicketNumber,
                SectorId,
                SectorNamePtSnapshot,
                SectorNameJpSnapshot,
                WorkAreaId,
                WorkAreaNamePtSnapshot,
                WorkAreaNameJpSnapshot,
                EquipmentId,
                EquipmentCodeSnapshot,
                EquipmentNamePtSnapshot,
                EquipmentNameJpSnapshot,
                MachineId,
                MachineCodeSnapshot,
                MachineNamePtSnapshot,
                MachineNameJpSnapshot,
                ReasonId,
                ReasonNamePtSnapshot,
                ReasonNameJpSnapshot,
                RequestedByFjCode,
                RequestedAtUtc,
                Status
            )
            VALUES (
                @TicketNumber,
                @SectorId,
                @SectorNamePtSnapshot,
                @SectorNameJpSnapshot,
                @WorkAreaId,
                @WorkAreaNamePtSnapshot,
                @WorkAreaNameJpSnapshot,
                @EquipmentId,
                @EquipmentCodeSnapshot,
                @EquipmentNamePtSnapshot,
                @EquipmentNameJpSnapshot,
                @MachineId,
                @MachineCodeSnapshot,
                @MachineNamePtSnapshot,
                @MachineNameJpSnapshot,
                @ReasonId,
                @ReasonNamePtSnapshot,
                @ReasonNameJpSnapshot,
                @RequestedByFjCode,
                @RequestedAtUtc,
                @Status
            );
            SELECT last_insert_rowid();
            """;
        insert.Parameters.AddWithValue("@TicketNumber", ticketNumber);
        insert.Parameters.AddWithValue("@SectorId", snapshot.SectorId);
        insert.Parameters.AddWithValue("@SectorNamePtSnapshot", snapshot.SectorNamePt);
        insert.Parameters.AddWithValue("@SectorNameJpSnapshot", snapshot.SectorNameJp);
        insert.Parameters.AddWithValue("@WorkAreaId", snapshot.WorkAreaId);
        insert.Parameters.AddWithValue("@WorkAreaNamePtSnapshot", snapshot.WorkAreaNamePt);
        insert.Parameters.AddWithValue("@WorkAreaNameJpSnapshot", snapshot.WorkAreaNameJp);
        insert.Parameters.AddWithValue("@EquipmentId", snapshot.EquipmentId);
        insert.Parameters.AddWithValue("@EquipmentCodeSnapshot", snapshot.EquipmentCode);
        insert.Parameters.AddWithValue("@EquipmentNamePtSnapshot", snapshot.EquipmentNamePt);
        insert.Parameters.AddWithValue("@EquipmentNameJpSnapshot", snapshot.EquipmentNameJp);
        insert.Parameters.AddWithValue("@MachineId", snapshot.MachineId is null ? DBNull.Value : snapshot.MachineId.Value);
        insert.Parameters.AddWithValue("@MachineCodeSnapshot", snapshot.MachineCode ?? (object)DBNull.Value);
        insert.Parameters.AddWithValue("@MachineNamePtSnapshot", snapshot.MachineNamePt ?? (object)DBNull.Value);
        insert.Parameters.AddWithValue("@MachineNameJpSnapshot", snapshot.MachineNameJp ?? (object)DBNull.Value);
        insert.Parameters.AddWithValue("@ReasonId", snapshot.ReasonId);
        insert.Parameters.AddWithValue("@ReasonNamePtSnapshot", snapshot.ReasonNamePt);
        insert.Parameters.AddWithValue("@ReasonNameJpSnapshot", snapshot.ReasonNameJp);
        insert.Parameters.AddWithValue("@RequestedByFjCode", requestedBy);
        insert.Parameters.AddWithValue("@RequestedAtUtc", requestedAtUtc.ToString("O"));
        insert.Parameters.AddWithValue("@Status", (int)RequestStatus.Open);

        var requestId = Convert.ToInt32(await insert.ExecuteScalarAsync(cancellationToken));
        await transaction.CommitAsync(cancellationToken);

        return new AssistanceRequest
        {
            Id = requestId,
            TicketNumber = ticketNumber,
            SectorId = snapshot.SectorId,
            SectorNamePt = snapshot.SectorNamePt,
            SectorNameJp = snapshot.SectorNameJp,
            WorkAreaId = snapshot.WorkAreaId,
            WorkAreaNamePt = snapshot.WorkAreaNamePt,
            WorkAreaNameJp = snapshot.WorkAreaNameJp,
            EquipmentId = snapshot.EquipmentId,
            EquipmentCode = snapshot.EquipmentCode,
            EquipmentNamePt = snapshot.EquipmentNamePt,
            EquipmentNameJp = snapshot.EquipmentNameJp,
            MachineId = snapshot.MachineId,
            MachineCode = snapshot.MachineCode,
            MachineNamePt = snapshot.MachineNamePt,
            MachineNameJp = snapshot.MachineNameJp,
            ReasonId = snapshot.ReasonId,
            ReasonNamePt = snapshot.ReasonNamePt,
            ReasonNameJp = snapshot.ReasonNameJp,
            RequestedByFjCode = requestedBy,
            RequestedAtUtc = requestedAtUtc,
            Status = RequestStatus.Open
        };
    }

    public async Task<bool> CloseAsync(CloseAssistanceRequestCommand command, CancellationToken cancellationToken = default)
    {
        FjCode.EnsureValid(command.ClosedByFjCode, "FJ do atendente");

        await using var connection = _factory.CreateOpenConnection();
        await using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE AssistanceRequests
            SET Status = @status,
                ClosedByFjCode = @closedBy,
                ClosedAtUtc = @closedAtUtc,
                ClosingNote = @note
            WHERE Id = @id
              AND Status = @openStatus;
            """;
        update.Parameters.AddWithValue("@status", (int)RequestStatus.Closed);
        update.Parameters.AddWithValue("@closedBy", FjCode.Normalize(command.ClosedByFjCode));
        update.Parameters.AddWithValue("@closedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        update.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(command.ClosingNote)
            ? DBNull.Value
            : command.ClosingNote.Trim());
        update.Parameters.AddWithValue("@id", command.RequestId);
        update.Parameters.AddWithValue("@openStatus", (int)RequestStatus.Open);

        return await update.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public Task<IReadOnlyList<AssistanceRequest>> GetOpenRequestsAsync(int? sectorId, CancellationToken cancellationToken = default)
    {
        return SearchInternalAsync(sectorId, RequestStatus.Open, null, null, cancellationToken);
    }

    public Task<IReadOnlyList<AssistanceRequest>> SearchAsync(RequestSearchFilter filter, CancellationToken cancellationToken = default)
    {
        return SearchInternalAsync(filter.SectorId, filter.Status, filter.FromUtc, filter.ToUtc, cancellationToken);
    }

    public async Task<MonitorBoardSnapshot> GetMonitorSnapshotAsync(string boardTitle, int? sectorId, int limit = 3, CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        var cards = new List<MonitorBoardCard>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT
                    Id,
                    TicketNumber,
                    SectorNamePtSnapshot,
                    SectorNameJpSnapshot,
                    WorkAreaNamePtSnapshot,
                    WorkAreaNameJpSnapshot,
                    EquipmentCodeSnapshot,
                    EquipmentNamePtSnapshot,
                    EquipmentNameJpSnapshot,
                    ReasonNamePtSnapshot,
                    ReasonNameJpSnapshot,
                    MachineCodeSnapshot,
                    MachineNamePtSnapshot,
                    MachineNameJpSnapshot,
                    RequestedByFjCode,
                    RequestedAtUtc
                FROM AssistanceRequests
                WHERE Status = @status
                  AND (@sectorId IS NULL OR SectorId = @sectorId)
                ORDER BY RequestedAtUtc ASC, Id ASC
                LIMIT @limit;
                """;
            command.Parameters.AddWithValue("@status", (int)RequestStatus.Open);
            command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);
            command.Parameters.AddWithValue("@limit", limit);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                cards.Add(new MonitorBoardCard
                {
                    RequestId = reader.GetInt32(0),
                    TicketNumber = reader.GetString(1),
                    SectorNamePt = reader.GetString(2),
                    SectorNameJp = reader.GetString(3),
                    WorkAreaNamePt = reader.GetString(4),
                    WorkAreaNameJp = reader.GetString(5),
                    EquipmentCode = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    EquipmentNamePt = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    EquipmentNameJp = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    ReasonNamePt = reader.GetString(9),
                    ReasonNameJp = reader.GetString(10),
                    MachineCode = reader.IsDBNull(11) ? null : reader.GetString(11),
                    MachineNamePt = reader.IsDBNull(12) ? null : reader.GetString(12),
                    MachineNameJp = reader.IsDBNull(13) ? null : reader.GetString(13),
                    RequestedByFjCode = reader.GetString(14),
                    RequestedAtUtc = DateTimeOffset.Parse(reader.GetString(15))
                });
            }
        }

        var totalOpenRequests = await ExecuteScalarIntAsync(connection,
            """
            SELECT COUNT(1)
            FROM AssistanceRequests
            WHERE Status = @status
              AND (@sectorId IS NULL OR SectorId = @sectorId);
            """,
            sectorId,
            cancellationToken);

        var latestOpenRequestId = await ExecuteScalarIntAsync(connection,
            """
            SELECT COALESCE(MAX(Id), 0)
            FROM AssistanceRequests
            WHERE Status = @status
              AND (@sectorId IS NULL OR SectorId = @sectorId);
            """,
            sectorId,
            cancellationToken);

        var sectorLabel = sectorId is null
            ? "Todos os setores"
            : cards.FirstOrDefault()?.SectorNamePt ?? await LoadSectorNameAsync(connection, sectorId.Value, cancellationToken);

        return new MonitorBoardSnapshot
        {
            BoardTitle = boardTitle,
            SectorLabel = sectorLabel,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TotalOpenRequests = totalOpenRequests,
            LatestOpenRequestId = latestOpenRequestId,
            Requests = cards
        };
    }

    private async Task<IReadOnlyList<AssistanceRequest>> SearchInternalAsync(
        int? sectorId,
        RequestStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                TicketNumber,
                SectorId,
                SectorNamePtSnapshot,
                SectorNameJpSnapshot,
                WorkAreaId,
                WorkAreaNamePtSnapshot,
                WorkAreaNameJpSnapshot,
                EquipmentId,
                EquipmentCodeSnapshot,
                EquipmentNamePtSnapshot,
                EquipmentNameJpSnapshot,
                MachineId,
                MachineCodeSnapshot,
                MachineNamePtSnapshot,
                MachineNameJpSnapshot,
                ReasonId,
                ReasonNamePtSnapshot,
                ReasonNameJpSnapshot,
                RequestedByFjCode,
                RequestedAtUtc,
                Status,
                ClosedByFjCode,
                ClosedAtUtc,
                ClosingNote
            FROM AssistanceRequests
            WHERE (@sectorId IS NULL OR SectorId = @sectorId)
              AND (@status IS NULL OR Status = @status)
              AND (@fromUtc IS NULL OR RequestedAtUtc >= @fromUtc)
              AND (@toUtc IS NULL OR RequestedAtUtc <= @toUtc)
            ORDER BY RequestedAtUtc DESC, Id DESC;
            """;
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);
        command.Parameters.AddWithValue("@status", status is null ? DBNull.Value : (int)status.Value);
        command.Parameters.AddWithValue("@fromUtc", fromUtc is null ? DBNull.Value : fromUtc.Value.ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc is null ? DBNull.Value : toUtc.Value.ToString("O"));

        var results = new List<AssistanceRequest>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AssistanceRequest
            {
                Id = reader.GetInt32(0),
                TicketNumber = reader.GetString(1),
                SectorId = reader.GetInt32(2),
                SectorNamePt = reader.GetString(3),
                SectorNameJp = reader.GetString(4),
                WorkAreaId = reader.GetInt32(5),
                WorkAreaNamePt = reader.GetString(6),
                WorkAreaNameJp = reader.GetString(7),
                EquipmentId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                EquipmentCode = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                EquipmentNamePt = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                EquipmentNameJp = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                MachineId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                MachineCode = reader.IsDBNull(13) ? null : reader.GetString(13),
                MachineNamePt = reader.IsDBNull(14) ? null : reader.GetString(14),
                MachineNameJp = reader.IsDBNull(15) ? null : reader.GetString(15),
                ReasonId = reader.GetInt32(16),
                ReasonNamePt = reader.GetString(17),
                ReasonNameJp = reader.GetString(18),
                RequestedByFjCode = reader.GetString(19),
                RequestedAtUtc = DateTimeOffset.Parse(reader.GetString(20)),
                Status = (RequestStatus)reader.GetInt32(21),
                ClosedByFjCode = reader.IsDBNull(22) ? null : reader.GetString(22),
                ClosedAtUtc = reader.IsDBNull(23) ? null : DateTimeOffset.Parse(reader.GetString(23)),
                ClosingNote = reader.IsDBNull(24) ? null : reader.GetString(24)
            });
        }

        return results;
    }

    private static async Task<string> LoadSectorNameAsync(SqliteConnection connection, int sectorId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(NamePt, Code) FROM Sectors WHERE Id = @id LIMIT 1;";
        command.Parameters.AddWithValue("@id", sectorId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToString(result) ?? $"Setor {sectorId}";
    }

    private static async Task<int> ExecuteScalarIntAsync(
        SqliteConnection connection,
        string sql,
        int? sectorId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@status", (int)RequestStatus.Open);
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task<RequestSnapshot> LoadSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CreateAssistanceRequestCommand command,
        CancellationToken cancellationToken)
    {
        await using var lookup = connection.CreateCommand();
        lookup.Transaction = transaction;
        lookup.CommandText = """
            SELECT
                s.Id,
                s.NamePt,
                s.NameJp,
                a.Id,
                a.NamePt,
                a.NameJp,
                e.Id,
                e.Code,
                e.NamePt,
                e.NameJp,
                r.Id,
                r.NamePt,
                r.NameJp,
                r.RequiresMachine,
                m.Id,
                m.Code,
                m.NamePt,
                m.NameJp
            FROM Sectors s
            INNER JOIN WorkAreas a ON a.Id = @workAreaId AND a.SectorId = s.Id AND a.IsActive = 1
            INNER JOIN Equipments e ON e.Id = @equipmentId AND e.SectorId = s.Id AND e.IsActive = 1
            INNER JOIN EquipmentReasonMappings erm ON erm.EquipmentId = e.Id
            INNER JOIN RequestReasons r ON r.Id = erm.ReasonId AND r.Id = @reasonId AND r.IsActive = 1
            LEFT JOIN Machines m ON m.Id = @machineId AND m.WorkAreaId = a.Id AND m.IsActive = 1
            WHERE s.Id = @sectorId
              AND s.IsActive = 1
            LIMIT 1;
            """;
        lookup.Parameters.AddWithValue("@sectorId", command.SectorId);
        lookup.Parameters.AddWithValue("@workAreaId", command.WorkAreaId);
        lookup.Parameters.AddWithValue("@equipmentId", command.EquipmentId);
        lookup.Parameters.AddWithValue("@reasonId", command.ReasonId);
        lookup.Parameters.AddWithValue("@machineId", command.MachineId is null ? DBNull.Value : command.MachineId.Value);

        await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Nao foi possivel localizar setor, area ou motivo para a solicitacao.");
        }

        var requiresMachine = reader.GetInt32(13) == 1;
        var machineId = reader.IsDBNull(14) ? (int?)null : reader.GetInt32(14);

        if (requiresMachine && machineId is null)
        {
            throw new InvalidOperationException("Este motivo exige selecao de maquina.");
        }

        return new RequestSnapshot
        {
            SectorId = reader.GetInt32(0),
            SectorNamePt = reader.GetString(1),
            SectorNameJp = reader.GetString(2),
            WorkAreaId = reader.GetInt32(3),
            WorkAreaNamePt = reader.GetString(4),
            WorkAreaNameJp = reader.GetString(5),
            EquipmentId = reader.GetInt32(6),
            EquipmentCode = reader.GetString(7),
            EquipmentNamePt = reader.GetString(8),
            EquipmentNameJp = reader.GetString(9),
            ReasonId = reader.GetInt32(10),
            ReasonNamePt = reader.GetString(11),
            ReasonNameJp = reader.GetString(12),
            MachineId = machineId,
            MachineCode = reader.IsDBNull(15) ? null : reader.GetString(15),
            MachineNamePt = reader.IsDBNull(16) ? null : reader.GetString(16),
            MachineNameJp = reader.IsDBNull(17) ? null : reader.GetString(17)
        };
    }

    private sealed class RequestSnapshot
    {
        public int SectorId { get; init; }
        public string SectorNamePt { get; init; } = string.Empty;
        public string SectorNameJp { get; init; } = string.Empty;
        public int WorkAreaId { get; init; }
        public string WorkAreaNamePt { get; init; } = string.Empty;
        public string WorkAreaNameJp { get; init; } = string.Empty;
        public int EquipmentId { get; init; }
        public string EquipmentCode { get; init; } = string.Empty;
        public string EquipmentNamePt { get; init; } = string.Empty;
        public string EquipmentNameJp { get; init; } = string.Empty;
        public int ReasonId { get; init; }
        public string ReasonNamePt { get; init; } = string.Empty;
        public string ReasonNameJp { get; init; } = string.Empty;
        public int? MachineId { get; init; }
        public string? MachineCode { get; init; }
        public string? MachineNamePt { get; init; }
        public string? MachineNameJp { get; init; }
    }
}
