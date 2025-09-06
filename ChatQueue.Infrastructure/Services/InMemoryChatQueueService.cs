using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;

namespace ChatQueue.Infrastructure.Services
{
    public sealed class InMemoryChatQueueService : IChatQueueService
    {
        private readonly Queue<ChatSession> _queue = new();
        private readonly HashSet<Guid> _index = new();
        private readonly object _lock = new();

        public int Count { get { lock (_lock) return _queue.Count; } }

        public void Enqueue(ChatSession session)
        {
            if (session is null) throw new ArgumentNullException(nameof(session));
            if (session.Status != ChatSessionStatus.Queued)
                throw new InvalidOperationException("Only Queued sessions can be enqueued.");

            lock (_lock)
            {
                if (_index.Contains(session.Id)) return;
                _queue.Enqueue(session);
                _index.Add(session.Id);
            }
        }

        public ChatSession? Peek()
        {
            lock (_lock)
            {
                foreach (var s in _queue)
                    if (s.Status == ChatSessionStatus.Queued) return s;
                return null;
            }
        }

        public bool TryRemove(Guid sessionId)
        {
            lock (_lock)
            {
                if (!_index.Contains(sessionId)) return false;

                var tmp = new Queue<ChatSession>(_queue.Count);
                var removed = false;

                while (_queue.Count > 0)
                {
                    var s = _queue.Dequeue();
                    if (!removed && s.Id == sessionId)
                    {
                        removed = true;
                        continue;
                    }
                    tmp.Enqueue(s);
                }
                while (tmp.Count > 0) _queue.Enqueue(tmp.Dequeue());
                return removed;
            }
        }
    }
}
