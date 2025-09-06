using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Entities
{
    public sealed record Agent(Guid Id, string Name, Seniority Level, bool IsAvailable, Guid TeamId)
    {
        public double Efficiency => Level switch
        {
            Seniority.Junior => 0.4,
            Seniority.Mid => 0.6,
            Seniority.Senior => 0.8,
            Seniority.TeamLead => 0.5,
            _ => 0.0
        };

        public int GetMaxConcurrency(int baseConcurrency) =>
            (int)(baseConcurrency * Efficiency);
    }
}
