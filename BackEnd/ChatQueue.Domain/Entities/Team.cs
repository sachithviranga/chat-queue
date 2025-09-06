using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Entities
{
    public sealed class Team(string name, List<Agent> agents, ShiftType shift)
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; init; } = name;
        public List<Agent> Agents { get; init; } = agents;
        public ShiftType AssignedShift { get; init; } = shift;

        public int GetCapacity(int baseConcurrency) =>
            Agents.Sum(agent => agent.GetMaxConcurrency(baseConcurrency));

        public int GetMaxQueueLength(int baseConcurrency, double multiplier) =>
            (int)(GetCapacity(baseConcurrency) * multiplier);
    }
}
