namespace ChatQueue.Domain.Entities
{
    public sealed record PollSession(Guid Id, DateTime UpdateAt, int Count);
}
