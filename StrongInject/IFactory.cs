using System.Threading.Tasks;

namespace StrongInject
{
    public interface IAsyncFactory<T>
    {
        ValueTask<T> CreateAsync();
    }
}
