using ChatQueue.Domain.Interfaces;

namespace ChatQueue.Infrastructure.Services
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
