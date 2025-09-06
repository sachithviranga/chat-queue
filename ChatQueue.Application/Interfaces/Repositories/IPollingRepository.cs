namespace ChatQueue.Application.Interfaces.Repositories
{
    public interface IPollingRepository
    {
        void RegisterPoll(Guid sessionId, DateTime updateDate);

        void UpdatePoll(Guid sessionId, DateTime updateDate);
        DateTime? GetLasteUpdateDateTime(Guid sessionId);
        bool IsInactive(Guid sessionId , int Count);
    }
}
