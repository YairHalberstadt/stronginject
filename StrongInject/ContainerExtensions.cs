using System;
using System.Threading.Tasks;

namespace StrongInject
{
    public static class ContainerExtensions
    {
        public static ValueTask<TResult> RunAsync<T, TResult, TParam>(this IContainer<T> container, Func<T, TParam, ValueTask<TResult>> func, TParam param)
        {
            return container.RunAsync(func, param);
        }

        public static ValueTask<TResult> RunAsync<T, TResult>(this IContainer<T> container, Func<T, ValueTask<TResult>> func)
        {
            return container.RunAsync((t, func) => func(t), func);
        }

        public static ValueTask<TResult> RunAsync<T, TResult>(this IContainer<T> container, Func<T, TResult> func)
        {
            return container.RunAsync((t, func) => new ValueTask<TResult>(func(t)), func);
        }

        public static ValueTask RunAsync<T>(this IContainer<T> container, Func<T, ValueTask> action)
        {
            return container.RunAsync(async (t, action) =>
            {
                await action(t);
                return default(object?);
            }, action).AsValueTask();
        }

        public static ValueTask RunAsync<T>(this IContainer<T> container, Action<T> action)
        {
            return container.RunAsync((t, action) =>
            {
                action(t);
                return new ValueTask<object?>(default(object));
            }, action).AsValueTask();
        }
    }
}

