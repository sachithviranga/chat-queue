using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;

namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface ISessionQueueRepository
    {
        Task AddAsync(AssignedChatSession session, CancellationToken ct = default);

        Task<bool> IsExistAsync(Guid sessionId, CancellationToken ct = default);

        Task<bool> InactiveAsync(Guid sessionId, CancellationToken ct = default);

        Task<int> CountAsync(CancellationToken ct = default);

        Task<IReadOnlyList<AssignedChatSession>> SnapshotAsync(CancellationToken ct = default);
    }
}
