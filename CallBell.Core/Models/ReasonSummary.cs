namespace CallBell.Core.Models;

public sealed class ReasonSummary
{
    public string ReasonNamePt { get; set; } = string.Empty;
    public string ReasonNameJp { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public double AverageCloseMinutes { get; set; }
}
