namespace StrongInject.Samples.ConsoleApp
{
    public class Config
    {
        public string BootstrapServers { get; init; } = "";
        public string GroupId { get; init; } = "";
        public string ConsumedTopic { get; init; } = "";
        public string TargetTopicPrefix { get; init; } = "";
    }
}
