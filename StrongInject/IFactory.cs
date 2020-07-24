using System.Threading.Tasks;

namespace StrongInject
{
    public interface IFactory<T>
    {
        T Create();
    }
    public interface IAsyncFactory<T>
    {
        ValueTask<T> CreateAsync();
    }
}
