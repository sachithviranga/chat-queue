using ChatQueue.Domain.Entities;

namespace ChatQueue.Application.Interfaces.Services
{
    public interface IChatService
    {
        Task<ChatSession> CreateChatAsync(CancellationToken ct = default);

        Task<bool> PollAsync(Guid sessionId, CancellationToken ct = default);
    }
}
