using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    public interface IContainer<T> : IDisposable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        TResult Run<TResult, TParam>(Func<T, TParam, TResult> func, TParam param);
        Owned<T> Resolve();
    }
    public interface IAsyncContainer<T> : IAsyncDisposable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ValueTask<TResult> RunAsync<TResult, TParam>(Func<T, TParam, ValueTask<TResult>> func, TParam param);
        ValueTask<AsyncOwned<T>> ResolveAsync();
    }
}
