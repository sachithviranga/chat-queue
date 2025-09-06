using ChatQueue.Domain.Enums;

namespace ChatQueue.Domain.Utilities
{
    public static class ShiftHelper
    {
        public static ShiftType GetCurrentShift(DateTime current)
        {
            var time = TimeOnly.FromDateTime(current);

            if (time >= new TimeOnly(8, 0) && time < new TimeOnly(16, 0))
                return ShiftType.Morning;

            if (time >= new TimeOnly(16, 0) && time < new TimeOnly(23, 59))
                return ShiftType.Evening;

            return ShiftType.Night;
        }

        public static bool IsOfficeHours(DateTime current)
        {
            var shift = GetCurrentShift(current);
            return shift is ShiftType.Morning or ShiftType.Evening;
        }
    }
}
