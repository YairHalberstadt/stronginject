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
    /// <typeparam name="T"></typeparam>
    public sealed class Owned<T> : IDisposable
    {
        private readonly Action _dispose;
        private int _disposed = 0;

        public Owned(T value, Action dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public void Dispose()
        {
            var disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed == 0)
            {
                _dispose();
            }
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
    /// <typeparam name="T"></typeparam>
    public sealed class AsyncOwned<T> : IAsyncDisposable
    {
        private readonly Func<ValueTask> _dispose;
        private int _disposed = 0;

        public AsyncOwned(T value, Func<ValueTask> dispose)
        {
            Value = value;
            _dispose = dispose;
        }

        public T Value { get; }

        public ValueTask DisposeAsync()
        {
            var disposed = Interlocked.Exchange(ref _disposed, 1);
            if (disposed == 0)
            {
                return _dispose();
            }
            return default;
        }
    }
}
