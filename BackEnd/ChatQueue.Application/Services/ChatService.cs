﻿using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Exceptions;
using ChatQueue.Domain.Interfaces;
using ChatQueue.Domain.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<ChatService> _logger;

        private const string OverflowTeamName = "Overflow";

        public ChatService(
            IChatQueueService queue,
            ISessionQueueRepository sessionQueueRepository,
            ITeamRepository teams,
            IPollingRepository polling,
            IDateTimeProvider clock,
            ChatConfiguration cfg,
            ILogger<ChatService> logger)
        {
            _queue = queue;
            _sessionQueueRepository = sessionQueueRepository;
            _teams = teams;
            _polling = polling;
            _clock = clock;
            _cfg = cfg;
            _logger = logger;
        }

        public async Task<ChatSession> CreateChatAsync(CancellationToken ct = default)
        {
            var now = _clock.Now;
            var currentShift = ShiftHelper.GetCurrentShift(now);

            _logger.LogInformation("Attempting to create chat at {Time} during {Shift} shift.", now, currentShift);

            var teams = await _teams.GetAllAsync(ct);
            var eligibleTeams = teams
                .Where(t => t.AssignedShift == currentShift && !IsOverflow(t))
                .ToList();

            _logger.LogInformation("Found {TeamCount} eligible teams for shift {Shift}.", eligibleTeams.Count, currentShift);

            if (!eligibleTeams.Any())
            {
                _logger.LogInformation("No eligible teams found. Refusing chat.");
                return Refused(now);
            }

            var allAgents = eligibleTeams.SelectMany(t => t.Agents).ToList();

            if (await QueueIsFull(allAgents))
            {
                _logger.LogInformation("Queue is full for eligible teams.");

                if (ShiftHelper.IsOfficeHours(now))
                {
                    var overflow = await _teams.GetOverflowAsync(ct);
                    if (overflow == null || await QueueIsFull(allAgents.Concat(overflow.Agents).ToList()))
                    {
                        _logger.LogInformation("Overflow team not found or also full. Refusing chat.");
                        throw new QueueFullException("Chat queue is full including overflow.");
                    }

                    allAgents.AddRange(overflow.Agents);
                    _logger.LogInformation("Using overflow agents. New total agent count: {Count}", allAgents.Count);
                }
                else
                {
                    _logger.LogInformation("Not office hours and queue is full. Refusing chat.");
                    throw new QueueFullException("Chat queue is full.");
                }
            }

            var session = new ChatSession(Guid.NewGuid(), now, ChatSessionStatus.Queued);

            _queue.Enqueue(session);

            _logger.LogInformation("Session created and queued. sessiod id {Id}",session.Id);

            return session;
        }


        public async Task<bool> PollAsync(Guid sessionId, CancellationToken ct = default)
        {
            if (!await _sessionQueueRepository.IsExistAsync(sessionId))
                return false;

            await _polling.UpdatePollAsync(sessionId, _clock.Now, ct);
            return true;
        }

        private (int capacity, int queueLimit) ComputeCapacityAndLimit(IEnumerable<Agent> agents)
        {
            var capacity = agents.Sum(a => a.GetMaxConcurrency(_cfg.AgentBaseConcurrency));
            var queueLimit = (int)(capacity * _cfg.QueueMultiplier);
            return (capacity, queueLimit);
        }

        private async Task<bool> QueueIsFull(IEnumerable<Agent> agents, CancellationToken ct = default)
        {
            var (_, queueLimit) = ComputeCapacityAndLimit(agents);
            if (queueLimit == 0) return false;
            return _queue.Count + await _sessionQueueRepository.CountAsync(ct) >= queueLimit;
        }

        private static bool IsOverflow(Team t) =>
            string.Equals(t.Name, OverflowTeamName, StringComparison.OrdinalIgnoreCase);

        private static ChatSession Refused(DateTime now) =>
            new(Guid.NewGuid(), now, ChatSessionStatus.Refused);
    }
}
