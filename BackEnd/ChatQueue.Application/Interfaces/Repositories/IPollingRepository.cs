namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface IPollingRepository
    {
        Task RegisterPollAsync(Guid sessionId, DateTime updateDate, CancellationToken ct = default);
        Task UpdatePollAsync(Guid sessionId, DateTime updateDate, CancellationToken ct = default);
        Task<DateTime?> GetLasteUpdateDateTimeAsync(Guid sessionId, CancellationToken ct = default);
        Task<bool> IsInactiveAsync(Guid sessionId, int Count, CancellationToken ct = default);
    }
}
