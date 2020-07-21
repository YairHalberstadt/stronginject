using System;
using System.Threading.Tasks;

namespace StrongInject
{
    public static class Helpers
    {
        public static ValueTask DisposeAsync<T>(T instance)
        {
#if !NETSTANDARD2_0
            if (instance is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }
#endif
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return default;
        }
    }
}
