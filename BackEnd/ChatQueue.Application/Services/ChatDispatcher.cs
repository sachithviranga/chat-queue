using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using ChatQueue.Domain.Utilities;

namespace ChatQueue.Application.Services
{
    public sealed class ChatDispatcher : IChatDispatcher
    {
        private readonly IChatQueueService _queue;
        private readonly ISessionQueueRepository _sessionQueueRepository;
        private readonly ITeamRepository _teams;
        private readonly IAssignmentRepository _assignments;
        private readonly IPollingRepository _pollingRepository;
        private readonly ChatConfiguration _cfg;
        private readonly IDateTimeProvider _clock;


        private readonly Dictionary<Seniority, int> _rrIndex = new()
        {
            { Seniority.Junior, 0 },
            { Seniority.Mid, 0 },
            { Seniority.Senior, 0 },
            { Seniority.TeamLead, 0 }
        };

        public ChatDispatcher(
            IChatQueueService queue,
            ISessionQueueRepository sessionQueueRepository,
            ITeamRepository teams,
            IAssignmentRepository assignments,
            IPollingRepository pollingRepository,
            ChatConfiguration cfg,
            IDateTimeProvider clock)
            => (_queue, _sessionQueueRepository, _teams, _assignments, _pollingRepository, _cfg, _clock) =
            (queue, sessionQueueRepository, teams, assignments , pollingRepository, cfg, clock);

        public async Task<AssignedChatSession?> DispatchNextAsync(CancellationToken ct = default)
        {
            var head = _queue.Peek();
            if (head is null) return null;

            var now = _clock.Now;
            var currentShift = ShiftHelper.GetCurrentShift(now);

            var allTeams = await _teams.GetAllAsync(ct);
            var mainAgents = allTeams
                .Where(t => t.AssignedShift == currentShift && !IsOverflow(t))
                .SelectMany(t => t.Agents)
                .ToList();

            var assignedAgentId = await TryAssignFromBucketsAsync(head.Id, mainAgents, ct);
            if (assignedAgentId is Guid a1)
            {
                _queue.TryRemove(head.Id);
                var session = new AssignedChatSession(head.Id, head.CreatedAt, ChatSessionStatus.Assigned, a1, now);
                _sessionQueueRepository.Add(session);

                _pollingRepository.RegisterPoll(session.Id, now);

                return session;
            }

            if (ShiftHelper.IsOfficeHours(now))
            {
                var overflow = await _teams.GetOverflowAsync(ct);
                if (overflow is not null)
                {
                    assignedAgentId = await TryAssignFromBucketsAsync(head.Id, overflow.Agents, ct);
                    if (assignedAgentId is Guid a2)
                    {
                        _queue.TryRemove(head.Id);
                        var session = new AssignedChatSession(head.Id, head.CreatedAt, ChatSessionStatus.Assigned, a2, now);
                        _sessionQueueRepository.Add(session);
                        _pollingRepository.RegisterPoll(session.Id, now);
                        return session;
                    }
                }
            }

            return null;
        }


        public Task<bool> ReleaseAsync(Guid sessionId, CancellationToken ct = default)
            => _assignments.ReleaseAsync(sessionId, ct);

        private async Task<Guid?> TryAssignFromBucketsAsync(Guid sessionId, List<Agent> agents, CancellationToken ct)
        {
            foreach (var level in new[] { Seniority.Junior, Seniority.Mid, Seniority.Senior, Seniority.TeamLead })
            {
                var bucket = agents.Where(a => a.Level == level).ToList();
                if (bucket.Count == 0) continue;

                var start = _rrIndex[level] % bucket.Count;

                for (int i = 0; i < bucket.Count; i++)
                {
                    var idx = (start + i) % bucket.Count;
                    var agent = bucket[idx];

                    var max = agent.GetMaxConcurrency(_cfg.AgentBaseConcurrency);
                    var load = await _assignments.GetLoadAsync(agent.Id, ct);
                    if (load >= max) continue;

                    if (await _assignments.TryAssignAsync(agent.Id, sessionId, ct))
                    {
                        _rrIndex[level] = idx + 1;
                        return agent.Id;
                    }
                }
            }
            return null;
        }

        private static bool IsOverflow(Team t) =>
            string.Equals(t.Name, "Overflow", StringComparison.OrdinalIgnoreCase);
    }
}
