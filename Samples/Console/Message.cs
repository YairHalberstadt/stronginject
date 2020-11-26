namespace StrongInject.Samples.ConsoleApp
{
    public record Message
    {
        public string Content { get; init; } = null!;
        public User Recipient { get; init; } = null!;
    }
}
