using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrongInject.Extensions.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static void AddContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : IContainer<TService> where TService : class
        {
            services.Replace(new ServiceDescriptor(typeof(TContainer), typeof(TContainer), ServiceLifetime.Singleton));
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForTransientService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddTransient(x => container.Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : IContainer<TService> where TService : class
        {
            services.Replace(new ServiceDescriptor(typeof(TContainer), typeof(TContainer), ServiceLifetime.Singleton));
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForScopedService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScoped(x => container.Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForSingletonService<TContainer, TService>(this IServiceCollection services) where TContainer : IContainer<TService> where TService : class
        {
            services.Replace(new ServiceDescriptor(typeof(TContainer), typeof(TContainer), ServiceLifetime.Singleton));
            services.AddSingleton(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddContainerForSingletonService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScoped(x => container.Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : IContainer<TService> where TService : class
        {
            services.Replace(new ServiceDescriptor(typeof(TContainer), typeof(TContainer), ServiceLifetime.Scoped));
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : IContainer<TService> where TService : class
        {
            services.Replace(new ServiceDescriptor(typeof(TContainer), typeof(TContainer), ServiceLifetime.Scoped));
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }
    }
}
