namespace ChatQueue.Domain.Interfaces
{
    public interface IDateTimeProvider
    {

        DateTime Now { get; }
        DateTime UtcNow { get; }
    }

}
