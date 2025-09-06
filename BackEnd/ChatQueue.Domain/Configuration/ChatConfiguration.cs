namespace ChatQueue.Domain.Configuration
{
    public class ChatConfiguration
    {
        public double QueueMultiplier { get; set; } = 1.5;
        public int AgentBaseConcurrency { get; set; } = 10;
        public int InactiveAfterSeconds { get; set; } = 4;
        public int InactiveAfterCount { get; set; } = 3;

        public int MaxIdleSeconds {  get; set; } = 10;
    }
}
