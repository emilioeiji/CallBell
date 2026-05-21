namespace CallBell.Core.Models;

public sealed class ReportSummary
{
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int ClosedRequests { get; set; }
    public double AverageCloseMinutes { get; set; }
}
