using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using ChatQueue.Domain.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<ChatDispatcher> _logger;


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
            IDateTimeProvider clock,
            ILogger<ChatDispatcher> logger)
            => (_queue, _sessionQueueRepository, _teams, _assignments, _pollingRepository, _cfg, _clock, _logger) =
            (queue, sessionQueueRepository, teams, assignments , pollingRepository, cfg, clock, logger);

        public async Task<AssignedChatSession?> DispatchNextAsync(CancellationToken ct = default)
        {
            var head = _queue.Peek();
            if (head is null)
            {
                _logger.LogInformation("No chat session in queue to dispatch.");
                return null;
            }

            var now = _clock.Now;
            var currentShift = ShiftHelper.GetCurrentShift(now);

            _logger.LogInformation("Dispatching chat session {SessionId} created at {CreatedAt} during shift {Shift}.", head.Id, head.CreatedAt, currentShift);

            var allTeams = await _teams.GetAllAsync(ct);
            var mainAgents = allTeams
                .Where(t => t.AssignedShift == currentShift && !IsOverflow(t))
                .SelectMany(t => t.Agents)
                .ToList();

            _logger.LogInformation("Found {AgentCount} main agents for shift {Shift}.", mainAgents.Count, currentShift);

            var assignedAgentId = await TryAssignFromBucketsAsync(head.Id, mainAgents, ct);
            if (assignedAgentId is Guid a1)
            {
                _logger.LogInformation("Assigned session {SessionId} to agent {AgentId} from main teams.", head.Id, a1);
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
                    _logger.LogInformation("Attempting to assign session {SessionId} to overflow team.", head.Id);
                    assignedAgentId = await TryAssignFromBucketsAsync(head.Id, overflow.Agents, ct);
                    if (assignedAgentId is Guid a2)
                    {
                        _logger.LogInformation("Assigned session {SessionId} to agent {AgentId} from overflow team.", head.Id, a2);
                        _queue.TryRemove(head.Id);
                        var session = new AssignedChatSession(head.Id, head.CreatedAt, ChatSessionStatus.Assigned, a2, now);
                        _sessionQueueRepository.Add(session);
                        _pollingRepository.RegisterPoll(session.Id, now);
                        return session;
                    }
                    else
                    {
                        _logger.LogInformation("No available agents in overflow team for session {SessionId}.", head.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("No overflow team found during office hours.");
                }
            }
            else
            {
                _logger.LogInformation("Not office hours, overflow team not considered for session {SessionId}.", head.Id);
            }

            _logger.LogInformation("No agent could be assigned to session {SessionId}.", head.Id);
            return null;
        }


        public Task<bool> ReleaseAsync(Guid sessionId, CancellationToken ct = default)
            => _assignments.ReleaseAsync(sessionId, ct);

        private async Task<Guid?> TryAssignFromBucketsAsync(Guid sessionId, List<Agent> agents, CancellationToken ct)
        {
            foreach (var level in new[] { Seniority.Junior, Seniority.Mid, Seniority.Senior, Seniority.TeamLead })
            {
                var bucket = agents.Where(a => a.Level == level).ToList();
                if (bucket.Count == 0)
                {
                    _logger.LogInformation("No agents found for seniority level {Level}.", level);
                    continue;
                }

                var start = _rrIndex[level] % bucket.Count;

                for (int i = 0; i < bucket.Count; i++)
                {
                    var idx = (start + i) % bucket.Count;
                    var agent = bucket[idx];

                    var max = agent.GetMaxConcurrency(_cfg.AgentBaseConcurrency);
                    var load = await _assignments.GetLoadAsync(agent.Id, ct);
                    _logger.LogInformation("Checking agent {AgentId} (Level: {Level}) - Load: {Load}, Max: {Max}", agent.Id, level, load, max);
                    if (load >= max) continue;

                    if (await _assignments.TryAssignAsync(agent.Id, sessionId, ct))
                    {
                        _logger.LogInformation("Successfully assigned session {SessionId} to agent {AgentId} (Level: {Level}).", sessionId, agent.Id, level);
                        _rrIndex[level] = idx + 1;
                        return agent.Id;
                    }
                    else
                    {
                        _logger.LogInformation("Failed to assign session {SessionId} to agent {AgentId} (Level: {Level}).", sessionId, agent.Id, level);
                    }
                }
            }
            _logger.LogInformation("No available agent found for session {SessionId}.", sessionId);
            return null;
        }

        private static bool IsOverflow(Team t) =>
            string.Equals(t.Name, "Overflow", StringComparison.OrdinalIgnoreCase);
    }
}
