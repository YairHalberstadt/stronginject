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

        [Fact]
        public static void OwnedCanBeUsedWithNullDisposeAction()
        {
            var owned = new Owned<string>("", dispose: null);

            // If a check is ever added in the future to return null or throw if Value is accessed after disposal,
            // the null action should not cause Owned<T> to think that disposal has already happened.
            Assert.Equal("", owned.Value);

            owned.Dispose();
        }

        [Fact]
        public static void AsyncOwnedCanBeUsedWithNullDisposeAction()
        {
            var owned = new AsyncOwned<string>("", dispose: null);

            // If a check is ever added in the future to return null or throw if Value is accessed after disposal,
            // the null action should not cause Owned<T> to think that disposal has already happened.
            Assert.Equal("", owned.Value);

            // This should complete instantly since there is nothing to do.
            Assert.True(owned.DisposeAsync().IsCompletedSuccessfully);
        }
    }
}
