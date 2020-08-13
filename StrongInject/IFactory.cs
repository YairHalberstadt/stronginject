using System.Threading.Tasks;

namespace StrongInject
{
    public interface IFactory<T>
    {
        T Create();

        void Release(T instance)
#if !NETSTANDARD2_0 && !NET472
            => Helpers.Dispose(instance)
#endif
            ;
    }

    public interface IAsyncFactory<T>
    {
        ValueTask<T> CreateAsync();

        ValueTask ReleaseAsync(T instance)
#if !NETSTANDARD2_0 && !NET472
            => Helpers.DisposeAsync(instance)
#endif
            ;
    }
}
