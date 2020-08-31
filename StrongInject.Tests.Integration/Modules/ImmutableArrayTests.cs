using StrongInject.Modules;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit;

namespace StrongInject.Tests.Integration.Modules
{
    public partial class ImmutableArrayTests
    {
        [RegisterModule(typeof(SafeImmutableArrayModule))]
        [RegisterModule(typeof(StandardModule))]
        [Register(typeof(A), typeof(I))]
        [Register(typeof(B), typeof(I))]
        public partial class SafeContainer : IContainer<(ImmutableArray<I> first, ImmutableArray<I> second)>
        {

        }

        [RegisterModule(typeof(UnsafeImmutableArrayModule))]
        [RegisterModule(typeof(StandardModule))]
        [Register(typeof(A), typeof(I))]
        [Register(typeof(B), typeof(I))]
        public partial class UnsafeContainer : IContainer<(ImmutableArray<I> first, ImmutableArray<I> second)>
        {

        }

        public interface I { }
        public class A : I { }
        public class B : I { }

        [Fact]
        public void TestCanResolveSafe()
        {
            var container = new SafeContainer();
            using var aScope1 = container.Resolve();
            var (first, second) = aScope1.Value;
            Assert.Equal(2, first.Length);
            Assert.Equal(2, second.Length);
            Assert.NotSame(Unsafe.As<ImmutableArray<I>, I[]>(ref first), Unsafe.As<ImmutableArray<I>, I[]>(ref second));
        }

        [Fact]
        public void TestCanResolveUnsafe()
        {
            var container = new UnsafeContainer();
            using var aScope1 = container.Resolve();
            var (first, second) = aScope1.Value;
            Assert.Equal(2, first.Length);
            Assert.Equal(2, second.Length);
            Assert.NotSame(Unsafe.As<ImmutableArray<I>, I[]>(ref first), Unsafe.As<ImmutableArray<I>, I[]>(ref second));
        }
    }
}
