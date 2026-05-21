using CallBell.Core.Entities;
using CallBell.Core.Models;
using CallBell.Data.Repositories;

namespace CallBell.Data.Services;

public sealed class CallBellRequestService
{
    private readonly RequestRepository _requestRepository;
    private readonly TriggerFileService _triggerFileService;

    public CallBellRequestService(RequestRepository requestRepository, TriggerFileService triggerFileService)
    {
        _requestRepository = requestRepository;
        _triggerFileService = triggerFileService;
    }

    public async Task<AssistanceRequest> CreateAsync(CreateAssistanceRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await _requestRepository.CreateAsync(command, cancellationToken);
        await _triggerFileService.WriteMarkerAsync("open", request.Id, request.SectorId, cancellationToken);
        return request;
    }

    public async Task<bool> CloseAsync(CloseAssistanceRequestCommand command, CancellationToken cancellationToken = default)
    {
        var closed = await _requestRepository.CloseAsync(command, cancellationToken);
        if (closed)
        {
            await _triggerFileService.WriteMarkerAsync("close", command.RequestId, 0, cancellationToken);
        }

        return closed;
    }
}
