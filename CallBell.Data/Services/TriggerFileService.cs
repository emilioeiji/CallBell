using CallBell.Config;

namespace CallBell.Data.Services;

public sealed class TriggerFileService
{
    private readonly CallBellSettings _settings;

    public TriggerFileService(CallBellSettings settings)
    {
        _settings = settings;
    }

    public Task WriteMarkerAsync(string eventType, int requestId, int sectorId, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_settings.TriggerDirectory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}_{eventType}_{sectorId}_{requestId}.trigger";
        var fullPath = Path.Combine(_settings.TriggerDirectory, fileName);
        return File.WriteAllTextAsync(fullPath, string.Empty, cancellationToken);
    }
}
