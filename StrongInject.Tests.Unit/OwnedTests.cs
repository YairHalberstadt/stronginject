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

            // This should survive any refactoring of the types or APIs.
            // (E.g. Replacing IOwned with owned.Cast<object>() returning an Owned<object> wrapper would not be acceptable.)
            Assert.Same(narrowerType, widerType);
        }

        [Fact]
        public static void AsyncOwnedCanBeImplicitlyCastToWiderT()
        {
            var narrowerType = new AsyncOwned<string>("", dispose: () => ValueTask.CompletedTask);
            IAsyncOwned<object> widerType = narrowerType;

            // This should survive any refactoring of the types or APIs.
            // (E.g. Replacing IOwned with owned.Cast<object>() returning an Owned<object> wrapper would not be acceptable.)
            Assert.Same(narrowerType, widerType);
        }
    }
}
