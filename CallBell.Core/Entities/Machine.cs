namespace CallBell.Core.Entities;

public sealed class Machine
{
    public int Id { get; set; }
    public int SectorId { get; set; }
    public int WorkAreaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NamePt { get; set; } = string.Empty;
    public string NameJp { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public override string ToString() => string.IsNullOrWhiteSpace(NamePt) ? Code : $"{Code} - {NamePt}";
}
