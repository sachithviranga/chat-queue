using ChatQueue.Domain.Entities;

namespace ChatQueue.Domain.Interfaces
{
    public interface IChatQueueService
    {
        void Enqueue(ChatSession session);
        ChatSession? Peek();
        int Count { get; }
        bool TryRemove(Guid sessionId);
    }

}
