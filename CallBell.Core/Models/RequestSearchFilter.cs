using CallBell.Core.Enums;

namespace CallBell.Core.Models;

public sealed class RequestSearchFilter
{
    public int? SectorId { get; set; }
    public RequestStatus? Status { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
}
