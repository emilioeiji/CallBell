namespace CallBell.Core.Models;

public sealed class CloseAssistanceRequestCommand
{
    public int RequestId { get; set; }
    public string ClosedByFjCode { get; set; } = string.Empty;
    public string? ClosingNote { get; set; }
}
