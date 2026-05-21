namespace CallBell.Core.Models;

public sealed class CreateAssistanceRequestCommand
{
    public string RequestedByFjCode { get; set; } = string.Empty;
    public int SectorId { get; set; }
    public int WorkAreaId { get; set; }
    public int EquipmentId { get; set; }
    public int ReasonId { get; set; }
    public int? MachineId { get; set; }
}
