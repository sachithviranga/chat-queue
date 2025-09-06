using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Entities
{
    public record AssignedChatSession(
        Guid Id,
        DateTime CreatedAt,
        ChatSessionStatus Status,
        Guid? AssignedAgentId = null,
        DateTime? AssignedAt = null
    ) : ChatSession(Id, CreatedAt, Status);
}
