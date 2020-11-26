using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public interface IProducer<TKey, TValue>
    {
        public Task Produce(TKey key, TValue value);
    }
}
