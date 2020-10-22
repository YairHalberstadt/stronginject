﻿using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace StrongInject.Extensions.DependencyInjection.Tests
{
    public partial class ServiceCollectionExtensionTests
    {
        public class A : IDisposable { public bool Disposed; public void Dispose() => Disposed = true; }
        public record B(A A) : IDisposable { public bool Disposed; public void Dispose() => Disposed = true; }
        public record C(A A) : IDisposable { public bool Disposed; public void Dispose() => Disposed = true; }

        [Register(typeof(A), Scope.SingleInstance)]
        [Register(typeof(B))]
        [Register(typeof(C))]
        public partial class Container : IContainer<B>, IContainer<C> { }

        [Fact]
        public void TestGenericAddContainerForTransientService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForTransientService<Container, B>();
            serviceCollection.AddContainerForTransientService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.NotSame(b1, b2);
            Assert.NotSame(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.NotSame(b3, b4);
            Assert.NotSame(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestGenericAddContainerForScopedService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForScopedService<Container, B>();
            serviceCollection.AddContainerForScopedService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestGenericAddContainerForSingletonService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForSingletonService<Container, B>();
            serviceCollection.AddContainerForSingletonService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.Same(b1, b3);
            Assert.Same(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.False(b3.Disposed);
            Assert.False(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestAddScopedContainerForTransientService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScopedContainerForTransientService<Container, B>();
            serviceCollection.AddScopedContainerForTransientService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.NotSame(b1, b2);
            Assert.NotSame(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.NotSame(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.NotSame(b3, b4);
            Assert.NotSame(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.True(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestAddScopedContainerForScopedService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScopedContainerForScopedService<Container, B>();
            serviceCollection.AddScopedContainerForScopedService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.NotSame(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.True(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestInstanceAddContainerForTransientService()
        {
            var container = new Container();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForTransientService<B>(container);
            serviceCollection.AddContainerForTransientService<C>(container);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.NotSame(b1, b2);
            Assert.NotSame(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.NotSame(b3, b4);
            Assert.NotSame(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.False(b1.A.Disposed);

            container.Dispose();

            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestInstanceAddContainerForScopedService()
        {
            var container = new Container();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForScopedService<B>(container);
            serviceCollection.AddContainerForScopedService<C>(container);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.False(b1.A.Disposed);

            container.Dispose();

            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestInstanceAddContainerForSingletonService()
        {
            var container = new Container();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForSingletonService<B>(container);
            serviceCollection.AddContainerForSingletonService<C>(container);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.Same(b1, b3);
            Assert.Same(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.False(b3.Disposed);
            Assert.False(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.False(b1.A.Disposed);

            container.Dispose();

            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestMixedAddContainerForSingletonServiceAndTransientService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForSingletonService<Container, B>();
            serviceCollection.AddContainerForTransientService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.NotSame(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.Same(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.NotSame(c3, c4);

            scope.Dispose();

            Assert.False(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestMixedAddContainerForSingletonServiceAndScopedService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForSingletonService<Container, B>();
            serviceCollection.AddContainerForScopedService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.Same(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.Same(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.Same(c3, c4);

            scope.Dispose();

            Assert.False(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }

        [Fact]
        public void TestMixedAddContainerForScopedServiceAndTransientService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddContainerForScopedService<Container, B>();
            serviceCollection.AddContainerForTransientService<Container, C>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var b1 = serviceProvider.GetRequiredService<B>();
            var c1 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, c1.A);
            var b2 = serviceProvider.GetRequiredService<B>();
            var c2 = serviceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b2.A);
            Assert.Same(b1, b2);
            Assert.NotSame(c1, c2);

            var scope = serviceProvider.CreateScope();
            var b3 = scope.ServiceProvider.GetRequiredService<B>();
            var c3 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b1.A, b3.A);
            Assert.NotSame(b1, b3);
            Assert.NotSame(c1, c3);

            var b4 = scope.ServiceProvider.GetRequiredService<B>();
            var c4 = scope.ServiceProvider.GetRequiredService<C>();
            Assert.Same(b3.A, b4.A);
            Assert.Same(b3, b4);
            Assert.NotSame(c3, c4);

            scope.Dispose();

            Assert.True(b3.Disposed);
            Assert.True(c3.Disposed);
            Assert.False(b3.A.Disposed);

            Assert.False(b1.Disposed);
            Assert.False(c1.Disposed);
            Assert.False(b1.A.Disposed);

            ((IDisposable)serviceProvider).Dispose();

            Assert.True(b1.Disposed);
            Assert.True(c1.Disposed);
            Assert.True(b1.A.Disposed);
        }
    }
}
