using ChatQueue.Domain.Entities;
using ChatQueue.Domain.Enums;

namespace ChatQueue.Infrastructure.Data.SeedData
{
    public static class TeamData
    {
        public static readonly IReadOnlyList<Team> Teams = new List<Team>
        {
            new Team("Team A",
            [
                new(Guid.NewGuid(), "Lead A", Seniority.TeamLead, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Mid A1", Seniority.Mid, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Mid A2", Seniority.Mid, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Junior A", Seniority.Junior, true, Guid.NewGuid()),
            ], ShiftType.Morning),

            new Team("Team B",
            [
                new(Guid.NewGuid(), "Senior B", Seniority.Senior, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Mid B", Seniority.Mid, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Junior B1", Seniority.Junior, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Junior B2", Seniority.Junior, true, Guid.NewGuid()),
            ], ShiftType.Evening),

            new Team("Team C",
            [
                new(Guid.NewGuid(), "Mid C1", Seniority.Mid, true, Guid.NewGuid()),
                new(Guid.NewGuid(), "Mid C2", Seniority.Mid, true, Guid.NewGuid()),
            ], ShiftType.Night),

            new Team("Overflow", Enumerable.Range(1, 2)
                .Select(i => new Agent(Guid.NewGuid(), $"Overflow {i}", Seniority.Junior, true, Guid.NewGuid()))
                .ToList(), ShiftType.Morning),
        };
    }
}
