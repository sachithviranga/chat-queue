using ChatQueue.Domain.Entities;

namespace ChatQueue.Application.Interfaces.Services
{
    public interface IChatDispatcher
    {
        Task<AssignedChatSession?> DispatchNextAsync(CancellationToken ct = default);

        Task<bool> ReleaseAsync(Guid sessionId, CancellationToken ct = default);
    }
}
