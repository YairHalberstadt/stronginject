using System.Collections.Generic;

namespace StrongInject.Samples.ConsoleApp
{
    public interface IConsumer<TKey, TValue>
    {
        IAsyncEnumerable<ICommitable<TKey, TValue>> Consume();
    }
}
