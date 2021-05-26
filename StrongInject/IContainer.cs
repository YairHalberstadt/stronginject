using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    /// <summary>
    /// Implement this interface to tell StrongInject to generate implementations for <see cref="Run{TResult, TParam}"/> and <see cref="Resolve"/>.
    /// You can implement this interface multiple times for different values of <typeparamref name="T"/>, and Single Instances will be shared.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IContainer<T> : IDisposable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        TResult Run<TResult, TParam>(Func<T, TParam, TResult> func, TParam param);
        Owned<T> Resolve();
    }

    /// Implement this interface to tell StrongInject to generate implementations for <see cref="RunAsync{TResult,TParam}"/> and <see cref="ResolveAsync"/>.
    /// You can implement this interface multiple times for different values of <typeparamref name="T"/>, and Single Instances will be shared.
    public interface IAsyncContainer<T> : IAsyncDisposable
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ValueTask<TResult> RunAsync<TResult, TParam>(Func<T, TParam, ValueTask<TResult>> func, TParam param);
        ValueTask<AsyncOwned<T>> ResolveAsync();
    }
}
