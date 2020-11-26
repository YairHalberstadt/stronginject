using StrongInject.Internal;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    public static class ContainerExtensions
    {
        public static TResult Run<T, TResult, TParam>(this IContainer<T> container, Func<T, TParam, TResult> func, TParam param)
        {
            return container.Run(func, param);
        }

        public static TResult Run<T, TResult>(this IContainer<T> container, Func<T, TResult> func)
        {
            return container.Run(static (t, func) => func(t), func);
        }

        public static void Run<T>(this IContainer<T> container, Action<T> action)
        {
            container.Run(static (t, action) =>
            {
                action(t);
                return default(object);
            }, action);
        }

        public static Owned<T> Resolve<T>(this IContainer<T> container) => container.Resolve();
    }

    public static class AsyncContainerExtensions
    {
        public static ValueTask<TResult> RunAsync<T, TResult, TParam>(this IAsyncContainer<T> container, Func<T, TParam, ValueTask<TResult>> func, TParam param)
        {
            return container.RunAsync(func, param);
        }

        public static ValueTask<TResult> RunAsync<T, TResult>(this IAsyncContainer<T> container, Func<T, ValueTask<TResult>> func)
        {
            return container.RunAsync(static (t, func) => func(t), func);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="container"></param>
        /// <param name="func"></param>
        /// <param name="_">Ignore this parameter. Used to prefer overload <see cref="RunAsync{T, TResult}(IAsyncContainer{T}, Func{T, ValueTask{TResult}})"/> to this overload.</param>
        /// <returns></returns>
        public static ValueTask<TResult> RunAsync<T, TResult>(this IAsyncContainer<T> container, Func<T, Task<TResult>> func, DummyParameter? _ = null)
        {
            return container.RunAsync(static (t, func) => new ValueTask<TResult>(func(t)), func);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="container"></param>
        /// <param name="func"></param>
        /// <param name="_">Ignore this parameter. Used to prefer overload <see cref="RunAsync{T, TResult}(IAsyncContainer{T}, Func{T, Task{TResult}}, DummyParameter?)"/> to this overload.</param>
        /// <returns></returns>
        public static ValueTask<TResult> RunAsync<T, TResult>(this IAsyncContainer<T> container, Func<T, TResult> func, DummyParameter? _ = null)
        {
            return container.RunAsync(static (t, func) => new ValueTask<TResult>(func(t)), func);
        }

        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask<TResult> RunAsync<T, TResult>(IAsyncContainer<T> container, Func<T, TResult> func) => container.RunAsync(func);

        public static ValueTask RunAsync<T>(this IAsyncContainer<T> container, Func<T, ValueTask> action)
        {
            return container.RunAsync(static async (t, action) =>
            {
                await action(t);
                return default(object?);
            }, action).AsValueTask();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="action"></param>
        /// <param name="_">Ignore this parameter. Used to prefer overload <see cref="RunAsync{T}(IAsyncContainer{T}, Func{T, ValueTask})"/> to this overload.</param>
        /// <returns></returns>
        public static ValueTask RunAsync<T>(this IAsyncContainer<T> container, Func<T, Task> action, DummyParameter? _ = null)
        {
            return container.RunAsync(static async (t, action) =>
            {
                await action(t);
                return default(object?);
            }, action).AsValueTask();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <param name="action"></param>
        /// <param name="_">Ignore this parameter. Used to prefer overload <see cref="RunAsync{T}(IAsyncContainer{T}, Func{T, Task}, DummyParameter?)"/> to this overload.</param>
        /// <returns></returns>
        public static ValueTask RunAsync<T>(this IAsyncContainer<T> container, Action<T> action, DummyParameter? _ = null)
        {
            return container.RunAsync(static (t, action) =>
            {
                action(t);
                return new ValueTask<object?>(default(object));
            }, action).AsValueTask();
        }

        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask RunAsync<T>(IAsyncContainer<T> container, Action<T> action) => container.RunAsync(action);

        public static ValueTask<AsyncOwned<T>> ResolveAsync<T>(this IAsyncContainer<T> container) => container.ResolveAsync();
    }
}

