using System.Threading.Tasks;

namespace StrongInject.Runtime
{
    public interface IContainer<T>
    {
        ValueTask<T> ResolveAsync();
    }
}
