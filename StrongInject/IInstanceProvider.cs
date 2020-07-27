using System.Threading.Tasks;

namespace StrongInject
{
    public interface IInstanceProvider<T>
    {
        T Get();

        void Release(T instance)
#if !NETSTANDARD2_0 && !NET472
        {}
#else
            ;
#endif
    }
    public interface IAsyncInstanceProvider<T>
    {
        ValueTask<T> GetAsync();

        ValueTask ReleaseAsync(T instance)
#if !NETSTANDARD2_0 && !NET472
            => default
#endif
            ;
    }
}
