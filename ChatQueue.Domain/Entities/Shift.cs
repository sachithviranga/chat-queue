using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Entities
{
    public sealed class Shift(ShiftType type, TimeOnly startTime, TimeOnly endTime)
    {
        public ShiftType Type { get; } = type;
        public TimeOnly StartTime { get; } = startTime;
        public TimeOnly EndTime { get; } = endTime;

        public bool IsActive(DateTime current)
        {
            var now = TimeOnly.FromDateTime(current);
            return StartTime <= EndTime
                ? now >= StartTime && now < EndTime
                : now >= StartTime || now < EndTime;
        }
    }
}
