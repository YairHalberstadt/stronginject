using StrongInject.Modules;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongInject.Tests.Integration.Modules
{
    public partial class CollectionTests
    {
        [RegisterModule(typeof(CollectionsModule))]
        [Register(typeof(A), typeof(I))]
        [Register(typeof(B), typeof(I))]
        [Register(typeof(Collections))]
        public partial class Container : IContainer<Collections>
        {

        }

        public record Collections(
            IEnumerable<I> Enumerable1,
            IEnumerable<I> Enumerable2,
            IReadOnlyList<I> List1,
            IReadOnlyList<I> List2,
            IReadOnlyCollection<I> Collection1,
            IReadOnlyCollection<I> Collection2) { }

        public interface I { }
        public class A : I { }
        public class B : I { }

        [Fact]
        public void TestCanResolveCollections()
        {
            var container = new Container();
            using var scope = container.Resolve();
            var collections = scope.Value;
            Assert.Equal(2, collections.Enumerable1.Count());
            Assert.Equal(2, collections.Enumerable2.Count());
            Assert.Equal(2, collections.List1.Count);
            Assert.Equal(2, collections.List2.Count);
            Assert.Equal(2, collections.Collection1.Count);
            Assert.Equal(2, collections.Collection2.Count);
            Assert.NotSame(collections.Enumerable1, collections.Enumerable2);
            Assert.NotSame(collections.List1, collections.List2);
            Assert.NotSame(collections.Collection1, collections.Collection2);
        }
    }
}
