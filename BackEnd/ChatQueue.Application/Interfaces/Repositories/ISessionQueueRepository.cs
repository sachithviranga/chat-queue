using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;

namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface ISessionQueueRepository
    {
        void Add(AssignedChatSession session);

        bool Inactive(Guid sessionId);

        int Count();

        IReadOnlyList<AssignedChatSession> Snapshot();
    }
}
