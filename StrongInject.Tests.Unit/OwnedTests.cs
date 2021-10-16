using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Generator.Tests.Unit
{
    public static class OwnedTests
    {
        [Fact]
        public static void OwnedCanBeImplicitlyCastToWiderT()
        {
            var narrowerType = new Owned<string>("", dispose: () => { });
            IOwned<object> widerType = narrowerType;

            // Rather than testing that the behavior is the same, just show that they are in fact the same object reference.
            Assert.Same(narrowerType, widerType);
        }

        [Fact]
        public static void AsyncOwnedCanBeImplicitlyCastToWiderT()
        {
            var narrowerType = new AsyncOwned<string>("", dispose: () => ValueTask.CompletedTask);
            IAsyncOwned<object> widerType = narrowerType;

            // Rather than testing that the behavior is the same, just show that they are in fact the same object reference.
            Assert.Same(narrowerType, widerType);
        }
    }
}
