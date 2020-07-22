using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestNullableConversion
    {
        public struct B { }
        public struct A
        {
            public B? B { get; }
            public A(B? b) => B = b;
        }


        [Registration(typeof(B), typeof(B?))]
        [Registration(typeof(A))]
        public partial class Container : IContainer<A>
        {
        }

        [Fact]
        public async Task Test()
        {
            await new Container().RunAsync(x => Assert.NotNull(x.B));
        }
    }
}