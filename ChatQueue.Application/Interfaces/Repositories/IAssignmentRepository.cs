namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface IAssignmentRepository
    {
        Task<bool> TryAssignAsync(Guid agentId, Guid sessionId, CancellationToken ct = default);

        Task<bool> ReleaseAsync(Guid sessionId, CancellationToken ct = default);

        Task<int> GetLoadAsync(Guid agentId, CancellationToken ct = default);

        Task<IReadOnlyCollection<Guid>> GetAssignedSessionsAsync(Guid agentId, CancellationToken ct = default);

        Task<Guid?> GetAssignedAgentAsync(Guid sessionId, CancellationToken ct = default);
    }
}
