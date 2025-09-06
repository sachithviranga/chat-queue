namespace ChatQueue.Application.Interfaces.Services
{
    public interface IChatMaintenanceService
    {
        Task CleanupInactiveAsync(CancellationToken ct = default);
    }
}
