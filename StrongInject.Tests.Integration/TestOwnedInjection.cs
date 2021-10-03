using System;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestOwnedInjection
    {
        public class DisposableDependency : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }

        public record DisposableDependencyWithDisposableDependency(DisposableDependency InnerDependency) : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }

        public record A(Func<Owned<DisposableDependencyWithDisposableDependency>> ResolveMiddleDependency);

        [Register(typeof(A))]
        [Register(typeof(DisposableDependencyWithDisposableDependency))]
        [Register(typeof(DisposableDependency))]
        public partial class ContainerWithTransientLeafDependency : IContainer<A>
        {
        }

        [Register(typeof(A))]
        [Register(typeof(DisposableDependencyWithDisposableDependency))]
        [Register(typeof(DisposableDependency), Scope.SingleInstance)]
        public partial class ContainerWithSingletonLeafDependency : IContainer<A>
        {
        }

        [Theory]
        [InlineData(new object[] { false })]
        [InlineData(new object[] { true })]
        public void DemonstrateUseCaseForFuncOfOwned(bool withSingletonLeafDependency)
        {
            using IContainer<A> container = withSingletonLeafDependency
                ? new ContainerWithSingletonLeafDependency()
                : new ContainerWithTransientLeafDependency();

            container.Run(c =>
            {
                // Outer class needs a new instance of the middle dependency.
                var ownedMiddleDependency1 = c.ResolveMiddleDependency();
                var innerDependency = ownedMiddleDependency1.Value.InnerDependency;

                // Outer class is finished with the middle dependency.
                // It can't dispose the inner dependency because it doesn't know about it, and the middle
                // dependency doesn't know the lifestyle of its inner dependency and so it also shouldn't dispose it.
                ownedMiddleDependency1.Dispose();
                Assert.True(ownedMiddleDependency1.Value.IsDisposed);

                // The lifestyle of the inner dependency can be configured either way and the outer and middle classes are agnostic about it.
                Assert.Equal(!withSingletonLeafDependency, innerDependency.IsDisposed);
            });
        }

        public record B(Func<Owned<DisposableDependency>> GetOwnedOfDisposableDependency) : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }

        public record C(B B);

        [Register(typeof(B)), Register(typeof(C))]
        [Register(typeof(DisposableDependency))]
        public partial class Container : IContainer<B>, IContainer<C>
        {
        }

        [Fact]
        public void OverlappingInstancesCanBeUsedWithFuncOfOwned()
        {
            using IContainer<B> container = new Container();
            container.Run(b =>
            {
                var firstResolution = b.GetOwnedOfDisposableDependency();
                var secondResolution = b.GetOwnedOfDisposableDependency();
                Assert.NotSame(firstResolution, secondResolution);

                firstResolution.Dispose();
                Assert.True(firstResolution.Value.IsDisposed);
                Assert.False(secondResolution.Value.IsDisposed);

                secondResolution.Dispose();
                Assert.True(secondResolution.Value.IsDisposed);
            });
        }

        [Fact]
        public void OwnedInstancesAreNeverImplicitlyReleased()
        {
            var owned = (Owned<DisposableDependency>?)null;
            var resolved = (C?)null;

            using (IContainer<C> container = new Container())
            {
                container.Run(c =>
                {
                    resolved = c;
                    owned = c.B.GetOwnedOfDisposableDependency();
                });
            }

            // The container itself is gone, so C and B are released.
            Assert.True(resolved!.B.IsDisposed);

            // But the owned instance is not released.
            Assert.False(owned!.Value.IsDisposed);

            // Until it is released manually.
            owned.Dispose();
            Assert.True(owned.Value.IsDisposed);
        }
    }
}
