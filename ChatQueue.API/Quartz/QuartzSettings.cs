namespace ChatQueue.API.Quartz
{
    public class QuartzJobSettings
    {
        public int? IntervalSeconds { get; set; }
        public int? IntervalMilliseconds { get; set; }
        public string? Cron { get; set; }
    }

    public class QuartzSettings
    {
        public QuartzJobSettings InactiveCleanup { get; set; } = new();
        public QuartzJobSettings Dispatch { get; set; } = new();
    }
}
