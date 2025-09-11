using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public class InMemorySessionQueueRepository : ISessionQueueRepository
    {
        private readonly List<AssignedChatSession> _assignedQueue = [];

        public Task AddAsync(AssignedChatSession session, CancellationToken ct = default)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (session.Status != ChatSessionStatus.Assigned)
                throw new InvalidOperationException("Only Assigned sessions can be enqueued.");

            _assignedQueue.Add(session);

            return Task.CompletedTask;
        }

        public Task<bool> IsExistAsync(Guid sessionId, CancellationToken ct = default)
        {
            var isExist = _assignedQueue.Exists(a => a.Id == sessionId && a.Status == ChatSessionStatus.Assigned);
            return Task.FromResult(isExist);
        }

        public Task<int> CountAsync(CancellationToken ct = default)
        {
            int count = _assignedQueue.Count(a => a.Status != ChatSessionStatus.Inactive);
            return Task.FromResult(count);
        }

        public Task<bool> InactiveAsync(Guid sessionId, CancellationToken ct = default)
        {
            var isInactived = false;
            if (_assignedQueue.Exists(a => a.Id == sessionId))
            {
                for (int i = 0; i < _assignedQueue.Count; i++)
                {
                    if (_assignedQueue[i].Id == sessionId)
                    {
                        _assignedQueue[i] = _assignedQueue[i] with { Status = ChatSessionStatus.Inactive };
                        isInactived = true;
                        break;
                    }
                }
            }
            return Task.FromResult(isInactived);
        }

        public Task<IReadOnlyList<AssignedChatSession>> SnapshotAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<AssignedChatSession>>(_assignedQueue.AsReadOnly());
        }

    }
}
