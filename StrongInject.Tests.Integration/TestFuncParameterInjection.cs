using System;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestFuncParameterInjection
    {
        public record A(B b, C c) { }
        public record B(C c) { }
        public class C { }

        [Registration(typeof(C))]
        [Registration(typeof(B))]
        [Registration(typeof(A))]
        public partial class Container : IContainer<Func<C, A>>
        {
        }

        [Fact]
        public void Test()
        {
            var container = new Container();
            var c1 = new C();
            var c2 = new C();
            container.Run(aF =>
            {
                var a1 = aF(c1);
                Assert.Same(c1, a1.c);
                Assert.Same(c1, a1.b.c);
                var a2 = aF(c2);
                Assert.Same(c2, a2.c);
                Assert.Same(c2, a2.b.c);
            });

        }
    }
}
