using StrongInject.Internal;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use StrongInjectContainerExtensions instead", error: true)]
    public static class ContainerExtensions
    {
        public static TResult Run<T, TResult, TParam>(IContainer<T> container, Func<T, TParam, TResult> func, TParam param)
        {
            return StrongInjectContainerExtensions.Run(container, func, param);
        }

        public static TResult Run<T, TResult>(IContainer<T> container, Func<T, TResult> func)
        {
            return StrongInjectContainerExtensions.Run(container, func);
        }

        public static void Run<T>(IContainer<T> container, Action<T> action)
        {
            StrongInjectContainerExtensions.Run(container, action);
        }

        public static Owned<T> Resolve<T>(IContainer<T> container)
        {
            return StrongInjectContainerExtensions.Resolve(container);
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use StrongInjectContainerExtensions instead", error: true)]
    public static class AsyncContainerExtensions
    {
        public static ValueTask<TResult> RunAsync<T, TResult, TParam>(IAsyncContainer<T> container, Func<T, TParam, ValueTask<TResult>> func, TParam param)
        {
            return StrongInjectContainerExtensions.RunAsync(container, func, param);
        }

        public static ValueTask<TResult> RunAsync<T, TResult>(IAsyncContainer<T> container, Func<T, ValueTask<TResult>> func)
        {
            return StrongInjectContainerExtensions.RunAsync(container, func);
        }
        
        public static ValueTask<TResult> RunAsync<T, TResult>(IAsyncContainer<T> container, Func<T, Task<TResult>> func, DummyParameter? _ = null)
        {
            return StrongInjectContainerExtensions.RunAsync(container, func, _);
        }
        
        public static ValueTask<TResult> RunAsync<T, TResult>(IAsyncContainer<T> container, Func<T, TResult> func, DummyParameter? _ = null)
        {
            return StrongInjectContainerExtensions.RunAsync(container, func, _);
        }

        public static ValueTask<TResult> RunAsync<T, TResult>(IAsyncContainer<T> container, Func<T, TResult> func)
        {
            return StrongInjectContainerExtensions.RunAsync(container, func);
        }

        public static ValueTask RunAsync<T>(IAsyncContainer<T> container, Func<T, ValueTask> action)
        {
            return StrongInjectContainerExtensions.RunAsync(container, action);
        }
        
        public static ValueTask RunAsync<T>(IAsyncContainer<T> container, Func<T, Task> action, DummyParameter? _ = null)
        {
            return StrongInjectContainerExtensions.RunAsync(container, action, _);
        }
        
        public static ValueTask RunAsync<T>(IAsyncContainer<T> container, Action<T> action, DummyParameter? _ = null)
        {
            return StrongInjectContainerExtensions.RunAsync(container, action, _);
        }

        public static ValueTask RunAsync<T>(IAsyncContainer<T> container, Action<T> action)
        {
            return StrongInjectContainerExtensions.RunAsync(container, action);
        }

        public static ValueTask<AsyncOwned<T>> ResolveAsync<T>(IAsyncContainer<T> container)
        {
            return StrongInjectContainerExtensions.ResolveAsync(container);
        }
    }
}

