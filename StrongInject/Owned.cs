using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrongInject
{
    /// <summary>
    /// <para>
    /// A disposable wrapper for an instance of <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// Make sure to dispose this once you are done using <see cref="Value"/>. This will dispose <see cref="Value"/> and all dependencies of it.
    /// </para>
    /// <para>
    /// Do not dispose <see cref="Value"/> directly as that will not dispose its dependencies.
    /// </para>
    /// <para>
    /// Do not use <see cref="Value"/> after this is disposed.
    /// </para>
    /// </summary>
    public interface IOwned<out T> : IDisposable
    {
        T Value { get; }
    }

    /// <inheritdoc cref="IOwned{T}"/>
    public sealed class Owned<T> : IOwned<T>
    {
        private Action? _dispose;

        public Owned(T value, Action dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public void Dispose()
        {
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }

    /// <summary>
    /// <para>
    /// An async disposable wrapper for an instance of <typeparamref name="T"/>.
    /// </para>
    /// <para>
    /// Make sure to dispose this once you are done using <see cref="Value"/>. This will dispose <see cref="Value"/> and all dependencies of it.
    /// </para>
    /// <para>
    /// Do not dispose <see cref="Value"/> directly as that will not dispose its dependencies.
    /// </para>
    /// <para>
    /// Do not use <see cref="Value"/> after this is disposed.
    /// </para>
    /// </summary>
    public interface IAsyncOwned<out T> : IAsyncDisposable
    {
        T Value { get; }
    }

    /// <inheritdoc cref="IAsyncOwned{T}"/>
    public sealed class AsyncOwned<T> : IAsyncOwned<T>
    {
        private Func<ValueTask>? _dispose;

        public AsyncOwned(T value, Func<ValueTask> dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public ValueTask DisposeAsync()
        {
            return Interlocked.Exchange(ref _dispose, null)?.Invoke() ?? default;
        }
    }
}
