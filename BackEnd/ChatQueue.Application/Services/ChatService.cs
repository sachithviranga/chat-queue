using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using ChatQueue.Domain.Utilities;

namespace ChatQueue.Application.Services
{
    public sealed class ChatService : IChatService
    {
        private readonly IChatQueueService _queue;
        private readonly ISessionQueueRepository _sessionQueueRepository;
        private readonly ITeamRepository _teams;
        private readonly IPollingRepository _polling;
        private readonly IDateTimeProvider _clock;
        private readonly ChatConfiguration _cfg;

        private const string OverflowTeamName = "Overflow";

        public ChatService(
            IChatQueueService queue,
            ISessionQueueRepository sessionQueueRepository,
            ITeamRepository teams,
            IPollingRepository polling,
            IDateTimeProvider clock,
            ChatConfiguration cfg)
        {
            _queue = queue;
            _sessionQueueRepository = sessionQueueRepository;
            _teams = teams;
            _polling = polling;
            _clock = clock;
            _cfg = cfg;
        }

        public async Task<ChatSession> CreateChatAsync(CancellationToken ct = default)
        {
            var now = _clock.Now;
            var currentShift = ShiftHelper.GetCurrentShift(now);

            var teams = await _teams.GetAllAsync(ct);
            var eligibleTeams = teams.Where(t => t.AssignedShift == currentShift && !IsOverflow(t)).ToList();
            if (eligibleTeams.Any())
            {
                var allAgents = eligibleTeams.SelectMany(t => t.Agents).ToList();

                if (QueueIsFull(allAgents))
                {
                    if (ShiftHelper.IsOfficeHours(now))
                    {
                        var overflow = await _teams.GetOverflowAsync(ct);
                        if (overflow is null) return Refused(now);

                        allAgents.AddRange(overflow.Agents);

                        if (QueueIsFull(allAgents))
                            return Refused(now);
                    }
                    else
                    {
                        return Refused(now);
                    }
                }

                var session = new ChatSession(Guid.NewGuid(), now, ChatSessionStatus.Queued);
                _queue.Enqueue(session);

                return session;
            }

            return Refused(now);
        }

        public void Poll(Guid sessionId) => _polling.UpdatePoll(sessionId, _clock.Now);

        private (int capacity, int queueLimit) ComputeCapacityAndLimit(IEnumerable<Agent> agents)
        {
            var capacity = agents.Sum(a => a.GetMaxConcurrency(_cfg.AgentBaseConcurrency));
            var queueLimit = (int)(capacity * _cfg.QueueMultiplier);
            return (capacity, queueLimit);
        }

        private bool QueueIsFull(IEnumerable<Agent> agents)
        {
            var (_, queueLimit) = ComputeCapacityAndLimit(agents);
            if (queueLimit == 0) return false;
            return _queue.Count + _sessionQueueRepository.Count() >= queueLimit;
        }

        private static bool IsOverflow(Team t) =>
            string.Equals(t.Name, OverflowTeamName, StringComparison.OrdinalIgnoreCase);

        private static ChatSession Refused(DateTime now) =>
            new(Guid.NewGuid(), now, ChatSessionStatus.Refused);
    }
}
