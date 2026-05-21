using CallBell.Core.Enums;
using CallBell.Core.Models;
using CallBell.Data.Db;

namespace CallBell.Data.Repositories;

public sealed class ReportRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ReportRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<ReportSummary> GetSummaryAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? sectorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                COUNT(1) AS TotalRequests,
                SUM(CASE WHEN Status = @openStatus THEN 1 ELSE 0 END) AS OpenRequests,
                SUM(CASE WHEN Status = @closedStatus THEN 1 ELSE 0 END) AS ClosedRequests,
                COALESCE(AVG(
                    CASE
                        WHEN ClosedAtUtc IS NOT NULL
                        THEN (julianday(ClosedAtUtc) - julianday(RequestedAtUtc)) * 24 * 60
                    END
                ), 0) AS AverageCloseMinutes
            FROM AssistanceRequests
            WHERE RequestedAtUtc >= @fromUtc
              AND RequestedAtUtc <= @toUtc
              AND (@sectorId IS NULL OR SectorId = @sectorId);
            """;
        command.Parameters.AddWithValue("@openStatus", (int)RequestStatus.Open);
        command.Parameters.AddWithValue("@closedStatus", (int)RequestStatus.Closed);
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToString("O"));
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ReportSummary();
        }

        return new ReportSummary
        {
            TotalRequests = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            OpenRequests = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            ClosedRequests = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            AverageCloseMinutes = reader.IsDBNull(3) ? 0 : reader.GetDouble(3)
        };
    }

    public async Task<IReadOnlyList<ReasonSummary>> GetReasonBreakdownAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? sectorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ReasonNamePtSnapshot,
                ReasonNameJpSnapshot,
                COUNT(1) AS TotalRequests,
                COALESCE(AVG(
                    CASE
                        WHEN ClosedAtUtc IS NOT NULL
                        THEN (julianday(ClosedAtUtc) - julianday(RequestedAtUtc)) * 24 * 60
                    END
                ), 0) AS AverageCloseMinutes
            FROM AssistanceRequests
            WHERE RequestedAtUtc >= @fromUtc
              AND RequestedAtUtc <= @toUtc
              AND (@sectorId IS NULL OR SectorId = @sectorId)
            GROUP BY ReasonNamePtSnapshot, ReasonNameJpSnapshot
            ORDER BY TotalRequests DESC, ReasonNamePtSnapshot;
            """;
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToString("O"));
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);

        var results = new List<ReasonSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ReasonSummary
            {
                ReasonNamePt = reader.GetString(0),
                ReasonNameJp = reader.GetString(1),
                TotalRequests = reader.GetInt32(2),
                AverageCloseMinutes = reader.IsDBNull(3) ? 0 : reader.GetDouble(3)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<DailySummary>> GetDailySummaryAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int? sectorId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _factory.CreateOpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                substr(RequestedAtUtc, 1, 10) AS DayLabel,
                COUNT(1) AS TotalRequests,
                SUM(CASE WHEN Status = @closedStatus THEN 1 ELSE 0 END) AS ClosedRequests,
                COALESCE(AVG(
                    CASE
                        WHEN ClosedAtUtc IS NOT NULL
                        THEN (julianday(ClosedAtUtc) - julianday(RequestedAtUtc)) * 24 * 60
                    END
                ), 0) AS AverageCloseMinutes
            FROM AssistanceRequests
            WHERE RequestedAtUtc >= @fromUtc
              AND RequestedAtUtc <= @toUtc
              AND (@sectorId IS NULL OR SectorId = @sectorId)
            GROUP BY substr(RequestedAtUtc, 1, 10)
            ORDER BY DayLabel DESC;
            """;
        command.Parameters.AddWithValue("@closedStatus", (int)RequestStatus.Closed);
        command.Parameters.AddWithValue("@fromUtc", fromUtc.ToString("O"));
        command.Parameters.AddWithValue("@toUtc", toUtc.ToString("O"));
        command.Parameters.AddWithValue("@sectorId", sectorId is null ? DBNull.Value : sectorId.Value);

        var results = new List<DailySummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DailySummary
            {
                DayLabel = reader.GetString(0),
                TotalRequests = reader.GetInt32(1),
                ClosedRequests = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                AverageCloseMinutes = reader.IsDBNull(3) ? 0 : reader.GetDouble(3)
            });
        }

        return results;
    }
}
