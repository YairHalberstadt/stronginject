using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Helpers
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Dispose<T>(T instance)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask DisposeAsync<T>(T instance)
        {
            if (instance is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return default;
        }
    }
}
