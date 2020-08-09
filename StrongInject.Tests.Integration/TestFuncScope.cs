using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestFuncScope
    {
        public record A(B b, C c, E e) { }
        public record B(D d) { }
        public record C(D d) { }
        public record D { }
        public record E { }

        [Registration(typeof(E), Scope.SingleInstance)]
        [Registration(typeof(D), Scope.InstancePerDependency)]
        [Registration(typeof(C))]
        [Registration(typeof(B))]
        [Registration(typeof(A))]
        public partial class Container : IContainer<Func<A>>
        {
        }

        [Fact]
        public void Test()
        {
            var container = new Container();
            var e1 = container.Run(aF =>
            {
                var a1 = aF();
                Assert.NotSame(a1.b.d, a1.c.d);
                var a2 = aF();
                Assert.NotSame(a2.b.d, a2.c.d);
                Assert.NotSame(a1, a2);
                Assert.NotSame(a1.b, a2.b);
                Assert.NotSame(a1.c, a2.c);
                Assert.NotSame(a1.b.d, a2.b.d);
                Assert.Same(a1.e, a2.e);
                return a1.e;
            });
            var e2 = container.Run(aF => aF().e);
            Assert.Same(e1, e2);

        }
    }
}
