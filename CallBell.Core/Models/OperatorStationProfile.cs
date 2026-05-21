namespace CallBell.Core.Models;

public sealed class OperatorStationProfile
{
    public string RequestedByFjCode { get; set; } = string.Empty;
    public int SectorId { get; set; }
    public int WorkAreaId { get; set; }
}
