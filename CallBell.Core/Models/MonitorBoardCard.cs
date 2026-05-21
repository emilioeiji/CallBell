namespace CallBell.Core.Models;

public sealed class MonitorBoardCard
{
    public int RequestId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string SectorNamePt { get; set; } = string.Empty;
    public string SectorNameJp { get; set; } = string.Empty;
    public string WorkAreaNamePt { get; set; } = string.Empty;
    public string WorkAreaNameJp { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentNamePt { get; set; } = string.Empty;
    public string EquipmentNameJp { get; set; } = string.Empty;
    public string ReasonNamePt { get; set; } = string.Empty;
    public string ReasonNameJp { get; set; } = string.Empty;
    public string? MachineCode { get; set; }
    public string? MachineNamePt { get; set; }
    public string? MachineNameJp { get; set; }
    public string RequestedByFjCode { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
}
