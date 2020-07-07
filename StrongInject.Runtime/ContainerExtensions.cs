using System;
using System.Threading.Tasks;

namespace StrongInject.Runtime
{
    public static class ContainerExtensions
    {
        public static ValueTask<T> ResolveAsync<T>(this IContainer<T> container) => container.ResolveAsync();

        public static async ValueTask<TResult> RunAsync<T, TResult>(this IContainer<T> container, Func<T, ValueTask<TResult>> action)
        {
            var obj = await container.ResolveAsync<T>();
            return await action(obj);
        }
    }
}

