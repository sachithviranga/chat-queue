using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;

namespace ChatQueue.Application.Services
{
    public sealed class ChatMaintenanceService : IChatMaintenanceService
    {
        private readonly ISessionQueueRepository _queue;
        private readonly IPollingRepository _polling;
        private readonly IAssignmentRepository _assignments;
        private readonly IDateTimeProvider _clock;
        private readonly ChatConfiguration _cfg;

        public ChatMaintenanceService(
            ISessionQueueRepository queue,
            IPollingRepository polling,
            IAssignmentRepository assignments,
            IDateTimeProvider clock,
            ChatConfiguration cfg) => (_queue, _polling, _assignments, _clock, _cfg) = (queue, polling, assignments, clock, cfg);
        public async Task CleanupInactiveAsync(CancellationToken ct = default)
        {
            var now = _clock.Now;
            var threshold = TimeSpan.FromSeconds(_cfg.InactiveAfterSeconds);

            foreach (var session in _queue.Snapshot().Where(a => a.Status != ChatSessionStatus.Inactive))
            {
                var last = _polling.GetLasteUpdateDateTime(session.Id) ?? session.AssignedAt;
                if (now - last >= threshold && _polling.IsInactive(session.Id , _cfg.InactiveAfterCount) && _queue.Inactive(session.Id))
                {
                     await _assignments.ReleaseAsync(session.Id);
                }
            }        
        }
    }
}
