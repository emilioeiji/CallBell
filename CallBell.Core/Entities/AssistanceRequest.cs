using CallBell.Core.Enums;

namespace CallBell.Core.Entities;

public sealed class AssistanceRequest
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public int SectorId { get; set; }
    public string SectorNamePt { get; set; } = string.Empty;
    public string SectorNameJp { get; set; } = string.Empty;
    public int WorkAreaId { get; set; }
    public string WorkAreaNamePt { get; set; } = string.Empty;
    public string WorkAreaNameJp { get; set; } = string.Empty;
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentNamePt { get; set; } = string.Empty;
    public string EquipmentNameJp { get; set; } = string.Empty;
    public int? MachineId { get; set; }
    public string? MachineCode { get; set; }
    public string? MachineNamePt { get; set; }
    public string? MachineNameJp { get; set; }
    public int ReasonId { get; set; }
    public string ReasonNamePt { get; set; } = string.Empty;
    public string ReasonNameJp { get; set; } = string.Empty;
    public string RequestedByFjCode { get; set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Open;
    public string? ClosedByFjCode { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string? ClosingNote { get; set; }

    public double? ElapsedMinutes =>
        ClosedAtUtc is null ? null : (ClosedAtUtc.Value - RequestedAtUtc).TotalMinutes;
}
