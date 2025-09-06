using ChatQueue.Domain.Entities;

namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface ITeamRepository
    {
        Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default);

        Task<Team?> GetOverflowAsync(CancellationToken ct = default);
    }
}
