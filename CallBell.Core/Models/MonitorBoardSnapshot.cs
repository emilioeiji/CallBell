namespace CallBell.Core.Models;

public sealed class MonitorBoardSnapshot
{
    public string BoardTitle { get; set; } = string.Empty;
    public string SectorLabel { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public int TotalOpenRequests { get; set; }
    public int LatestOpenRequestId { get; set; }
    public IReadOnlyList<MonitorBoardCard> Requests { get; set; } = Array.Empty<MonitorBoardCard>();
}
