using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Domain.Entities;
using ChatQueue.Infrastructure.Data.SeedData;

namespace ChatQueue.Infrastructure.Data.Repositories
{
    public sealed class InMemoryTeamRepository : ITeamRepository
    {
        private static readonly IReadOnlyList<Team> _teams = TeamData.Teams;

        public Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(_teams);

        public Task<Team?> GetOverflowAsync(CancellationToken ct = default)
            => Task.FromResult(_teams.FirstOrDefault(t =>
                string.Equals(t.Name, "Overflow", StringComparison.OrdinalIgnoreCase)));
    }
}
