using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestInstancePerResolutionHasCorrectScope
    {
        public record A(B b, C c) { }
        public record B(D d) { }
        public record C(D d) { }
        public record D { }

        [Registration(typeof(D))]
        [Registration(typeof(C))]
        [Registration(typeof(B))]
        [Registration(typeof(A))]
        public partial class Container : IAsyncContainer<A>
        {
        }

        [Fact]
        public async Task Test()
        {
            var container = new Container();
            var a1 = await container.RunAsync(x => x);
            Assert.Same(a1.b.d, a1.c.d);
            var a2 = await container.RunAsync(x => x);
            Assert.Same(a2.b.d, a2.c.d);
            Assert.NotSame(a1, a2);
            Assert.NotSame(a1.b, a2.b);
            Assert.NotSame(a1.c, a2.c);
            Assert.NotSame(a1.b.d, a2.b.d);
        }
    }
}
