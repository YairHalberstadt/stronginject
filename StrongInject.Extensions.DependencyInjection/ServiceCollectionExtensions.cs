using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.ComponentModel;

namespace StrongInject.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("Use " + nameof(AddTransientServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.AddTransientServiceUsingContainer<TContainer, TService>();
        }

        [Obsolete("Use " + nameof(AddTransientServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForTransientService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddTransientServiceUsingContainer(container);
        }

        [Obsolete("Use " + nameof(AddScopedServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.AddScopedServiceUsingContainer<TContainer, TService>();
        }

        [Obsolete("Use " + nameof(AddScopedServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForScopedService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScopedServiceUsingContainer(container);
        }

        [Obsolete("Use " + nameof(AddSingletonServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForSingletonService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.AddSingletonServiceUsingContainer<TContainer, TService>();
        }

        [Obsolete("Use " + nameof(AddSingletonServiceUsingContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddContainerForSingletonService<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddSingletonServiceUsingContainer(container);
        }

        [Obsolete("Use " + nameof(AddTransientServiceUsingScopedContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddScopedContainerForTransientService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.AddTransientServiceUsingScopedContainer<TContainer, TService>();
        }

        [Obsolete("Use " + nameof(AddScopedServiceUsingScopedContainer), false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddScopedContainerForScopedService<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.AddScopedServiceUsingScopedContainer<TContainer, TService>();
        }

        public static void AddTransientServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddTransientServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddTransient(x => container.Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScoped(x => container.Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddSingletonServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddSingleton(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddSingleton(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddSingletonServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddSingleton(x => container.Resolve());
            services.AddSingleton(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddTransientServiceUsingScopedContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddTransient(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        public static void AddScopedServiceUsingScopedContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.AddScoped(x => x.GetRequiredService<Owned<TService>>().Value);
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> transiently using <typeparamref name="TContainer"/>.
        /// </summary>
        public static void ReplaceWithTransientServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Transient));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> using <paramref name="container"/>.
        /// </summary>
        public static void ReplaceWithTransientServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddTransient(x => container.Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Transient));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> transiently using <typeparamref name="TContainer"/>.
        /// </summary>
        public static void ReplaceWithScopedServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Scoped));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> using <paramref name="container"/>.
        /// </summary>
        public static void ReplaceWithScopedServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddScoped(x => container.Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Scoped));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> transiently using <typeparamref name="TContainer"/>.
        /// </summary>
        public static void ReplaceWithSingletonServiceUsingContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddSingleton<TContainer, TContainer>();
            services.AddSingleton(x => x.GetRequiredService<TContainer>().Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Singleton));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> using <paramref name="container"/>.
        /// </summary>
        public static void ReplaceWithSingletonServiceUsingContainer<TService>(this IServiceCollection services, IContainer<TService> container) where TService : class
        {
            services.AddSingleton(x => container.Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Singleton));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> transiently using <typeparamref name="TContainer"/>.
        /// </summary>
        public static void ReplaceWithTransientServiceUsingScopedContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddTransient(x => x.GetRequiredService<TContainer>().Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Transient));
        }

        /// <summary>
        /// Removes the first service type in <paramref name="services"/> with the same service type as <typeparamref name="TService"/>
        /// and adds a <see cref="ServiceDescriptor"/> to the collection which resolves <typeparamref name="TService"/> transiently using <typeparamref name="TContainer"/>.
        /// </summary>
        public static void ReplaceWithScopedServiceUsingScopedContainer<TContainer, TService>(this IServiceCollection services) where TContainer : class, IContainer<TService> where TService : class
        {
            services.TryAddScoped<TContainer, TContainer>();
            services.AddScoped(x => x.GetRequiredService<TContainer>().Resolve());
            services.Replace(new ServiceDescriptor(typeof(TService), x => x.GetRequiredService<Owned<TService>>().Value, ServiceLifetime.Scoped));
        }
    }
}
