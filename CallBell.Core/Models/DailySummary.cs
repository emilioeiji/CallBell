namespace CallBell.Core.Models;

public sealed class DailySummary
{
    public string DayLabel { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int ClosedRequests { get; set; }
    public double AverageCloseMinutes { get; set; }
    public int DayShiftRequests { get; set; }
    public int NightShiftRequests { get; set; }
}
