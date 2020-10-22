using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongInject.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForTransientService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddTransient(x => container.Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForScopedService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScoped(x => container.Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForSingletonService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddSingleton(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddSingleton(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForSingletonService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddSingleton(x => container.Resolve());
            services.AddSingleton(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }
    }
}
