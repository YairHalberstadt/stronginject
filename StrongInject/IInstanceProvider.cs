using System.Threading.Tasks;

namespace StrongInject
{
    public interface IInstanceProvider<T>
    {
        ValueTask<T> GetAsync();

        ValueTask ReleaseAsync(T instance)
#if !NETSTANDARD2_0 && !NET472
         => default
#endif
            ;
    }
}
