using System.Threading.Tasks;

namespace StrongInject.Runtime
{
    public interface IFactory<T>
    {
        ValueTask<T> CreateAsync();
    }
}
