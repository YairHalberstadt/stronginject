using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Generator.Tests.Unit
{
    public partial class TestDisposalAfterUsageScope
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

        [Registration(typeof(D), Scope.InstancePerDependency)]
        [Registration(typeof(C), Scope.InstancePerResolution)]
        [Registration(typeof(B))]
        [Registration(typeof(A))]
        public partial class Container1 : IContainer<A>
        {
        }

        [Fact]
        public async Task TestCalledForInstancePerDependencyAndResolution()
        {
            var container = new Container1();
            var a = await container.RunAsync(x => x);
            Assert.True(a.Disposed);
            Assert.True(a.b.Disposed);
            Assert.True(a.c.AsyncDisposed);
            Assert.False(a.c.Disposed);
            Assert.True(a.b.d.AsyncDisposed);
            Assert.True(a.c.d.AsyncDisposed);
        }

        [Registration(typeof(D))]
        [Registration(typeof(C))]
        [Registration(typeof(B), Scope.SingleInstance)]
        [Registration(typeof(A))]
        public partial class Container2 : IContainer<A>
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
    }
}
