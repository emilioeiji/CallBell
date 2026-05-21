using CallBell.Core.Entities;

namespace CallBell.Core.Models;

public sealed class CatalogSnapshot
{
    public IReadOnlyList<Sector> Sectors { get; init; } = Array.Empty<Sector>();
    public IReadOnlyList<WorkArea> WorkAreas { get; init; } = Array.Empty<WorkArea>();
    public IReadOnlyList<Equipment> Equipments { get; init; } = Array.Empty<Equipment>();
    public IReadOnlyList<Machine> Machines { get; init; } = Array.Empty<Machine>();
    public IReadOnlyList<RequestReason> Reasons { get; init; } = Array.Empty<RequestReason>();
    public IReadOnlyList<EquipmentReasonMapping> EquipmentReasonMappings { get; init; } = Array.Empty<EquipmentReasonMapping>();
}
