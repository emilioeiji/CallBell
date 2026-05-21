namespace CallBell.Core.Entities;

public sealed class RequestReason
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NamePt { get; set; } = string.Empty;
    public string NameJp { get; set; } = string.Empty;
    public bool RequiresMachine { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public override string ToString() => string.IsNullOrWhiteSpace(NamePt) ? Code : NamePt;
}
