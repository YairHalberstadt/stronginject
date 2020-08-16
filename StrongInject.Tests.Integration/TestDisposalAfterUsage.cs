using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestDisposalAfterUsage
    {
        public record A(B b, C c) : IDisposable
        {
            public bool Disposed { get; set; }
            public void Dispose()
            {
                Disposed = true;
            }
        }
        public record B(D d) : IDisposable
        {
            public bool Disposed { get; set; }
            public void Dispose()
            {
                Disposed = true;
            }
        }
        public record C(D d) : IDisposable, IAsyncDisposable
        {
            public bool Disposed { get; set; }
            public bool AsyncDisposed { get; set; }
            public void Dispose()
            {
                Disposed = true;
            }

            public ValueTask DisposeAsync()
            {
                AsyncDisposed = true;
                return default;
            }
        }
        public record D : IAsyncDisposable
        {
            public bool AsyncDisposed { get; set; }
            public ValueTask DisposeAsync()
            {
                AsyncDisposed = true;
                return default;
            }
        }

        [Register(typeof(D), Scope.InstancePerDependency)]
        [Register(typeof(C), Scope.InstancePerResolution)]
        [Register(typeof(B))]
        [Register(typeof(A))]
        public partial class Container1 : IAsyncContainer<A>, IAsyncContainer<Func<A>>
        {
        }

        [Fact]
        public async Task TestCalledForInstancePerDependencyAndResolution()
        {
            var container = new Container1();
            var a = await container.RunAsync<A, A>(x => x);
            Assert.True(a.Disposed);
            Assert.True(a.b.Disposed);
            Assert.True(a.c.AsyncDisposed);
            Assert.False(a.c.Disposed);
            Assert.True(a.b.d.AsyncDisposed);
            Assert.True(a.c.d.AsyncDisposed);
        }

        [Fact]
        public async Task TestCalledForInstancePerDependencyAndResolutionOnAllFuncs()
        {
            var container = new Container1();
            var (a1, a2) = await container.RunAsync<Func<A>, (A, A)>(aF => {
                var a1 = aF();
                var a2 = aF();
                Assert.False(a1.Disposed);
                Assert.False(a2.Disposed);
                return (a1, a2);
            });

            Assert.True(a1.Disposed);
            Assert.True(a1.b.Disposed);
            Assert.True(a1.c.AsyncDisposed);
            Assert.False(a1.c.Disposed);
            Assert.True(a1.b.d.AsyncDisposed);
            Assert.True(a1.c.d.AsyncDisposed);

            Assert.True(a2.Disposed);
            Assert.True(a2.b.Disposed);
            Assert.True(a2.c.AsyncDisposed);
            Assert.False(a2.c.Disposed);
            Assert.True(a2.b.d.AsyncDisposed);
            Assert.True(a2.c.d.AsyncDisposed);
        }

        [Register(typeof(D))]
        [Register(typeof(C))]
        [Register(typeof(B), Scope.SingleInstance)]
        [Register(typeof(A))]
        public partial class Container2 : IAsyncContainer<A>
        {
        }

        [Fact]
        public async Task TestNotCalledForSingleInstancyAndDependencies()
        {
            var container = new Container2();
            var a = await container.RunAsync(x => x);
            Assert.True(a.Disposed);
            Assert.False(a.b.Disposed);
            Assert.True(a.c.AsyncDisposed);
            Assert.False(a.c.Disposed);
            Assert.False(a.b.d.AsyncDisposed);
            Assert.True(a.c.d.AsyncDisposed);
        }

        [Fact]
        public async Task TestSingleInstancyAndDependenciesDisposedOnContainerDisposal()
        {
            var container = new Container2();
            var a = await container.RunAsync(x => x);
            Assert.True(a.Disposed);
            Assert.False(a.b.Disposed);
            Assert.True(a.c.AsyncDisposed);
            Assert.False(a.c.Disposed);
            Assert.False(a.b.d.AsyncDisposed);
            Assert.True(a.c.d.AsyncDisposed);

            await container.DisposeAsync();

            Assert.True(a.b.Disposed);
            Assert.True(a.b.d.AsyncDisposed);
        }
    }
}
