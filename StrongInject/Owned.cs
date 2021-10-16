using System;
using System.Threading;
using System.Threading.Tasks;

namespace StrongInject
{
    /// <summary>
    /// A disposable wrapper for an instance of <typeparamref name="T"/>.
    /// 
    /// Make sure to dispose this once you are done using <see cref="Value"/>. This will dispose <see cref="Value"/> and all dependencies of it.
    /// 
    /// Do not dispose <see cref="Value"/> directly as that will not dispose its dependencies.
    /// 
    /// Do not use <see cref="Value"/> after this is disposed.
    /// </summary>
    public sealed class Owned<T> : IDisposable
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
    /// An async disposable wrapper for an instance of <typeparamref name="T"/>.
    /// 
    /// Make sure to dispose this once you are done using <see cref="Value"/>. This will dispose <see cref="Value"/> and all dependencies of it.
    /// 
    /// Do not dispose <see cref="Value"/> directly as that will not dispose its dependencies.
    /// 
    /// Do not use <see cref="Value"/> after this is disposed.
    /// </summary>
    public sealed class AsyncOwned<T> : IAsyncDisposable
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
