using System;

namespace StrongInject.Samples.ConsoleApp
{
    public record User
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
    }
}
