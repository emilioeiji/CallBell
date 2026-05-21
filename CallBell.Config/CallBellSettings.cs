namespace CallBell.Config;

public sealed class CallBellSettings
{
    public required string DatabasePath { get; init; }
    public required string TriggerDirectory { get; init; }
    public required string ProfileDirectory { get; init; }
    public required string MonitorBoardTitle { get; init; }
    public required int MonitorRefreshSeconds { get; init; }
    public required bool PortableMode { get; init; }
}
