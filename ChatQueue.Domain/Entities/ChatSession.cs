using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Entities
{
    public record ChatSession(Guid Id, DateTime CreatedAt, ChatSessionStatus Status);
}
