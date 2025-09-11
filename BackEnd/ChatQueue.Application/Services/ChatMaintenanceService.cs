using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatQueue.Application.Services
{
    public sealed class ChatMaintenanceService : IChatMaintenanceService
    {
        private readonly ISessionQueueRepository _queue;
        private readonly IPollingRepository _polling;
        private readonly IAssignmentRepository _assignments;
        private readonly IDateTimeProvider _clock;
        private readonly ChatConfiguration _cfg;
        private readonly ILogger<ChatMaintenanceService> _logger;

        public ChatMaintenanceService(
            ISessionQueueRepository queue,
            IPollingRepository polling,
            IAssignmentRepository assignments,
            IDateTimeProvider clock,
            ChatConfiguration cfg,
            ILogger<ChatMaintenanceService> logger) =>
            (_queue, _polling, _assignments, _clock, _cfg, _logger) =
            (queue, polling, assignments, clock, cfg, logger);
        public async Task CleanupInactiveAsync(CancellationToken ct = default)
        {
            var now = _clock.Now;
            var threshold = TimeSpan.FromSeconds(_cfg.InactiveAfterSeconds);
            var maximumIdleTime = TimeSpan.FromSeconds(_cfg.MaxIdleSeconds);
            var sessions = await _queue.SnapshotAsync(ct);
            foreach (var session in sessions.Where(a => a.Status != ChatSessionStatus.Inactive))
            {
                var last = await _polling.GetLasteUpdateDateTimeAsync(session.Id, ct) ?? session.AssignedAt;
                if (now - last >= threshold && (await _polling.IsInactiveAsync(session.Id, _cfg.InactiveAfterCount, ct) || now - last > maximumIdleTime) && await _queue.InactiveAsync(session.Id , ct))
                {
                    _logger.LogInformation("Releasing inactive session {SessionId}. Last activity: {LastActivity}, Now: {Now}, Threshold: {Threshold}", session.Id, last, now, threshold);
                    await _assignments.ReleaseAsync(session.Id);
                }
            }
        }
    }
}
