using StrongInject.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration.Modules
{
    public partial class LazyTests
    {
        [RegisterModule(typeof(LazyModule))]
        [Register(typeof(A))]
        public partial class Container : IContainer<Lazy<A>>
        {

        }

        public class A { }

        [Fact]
        public void TestCanResolveLazy()
        {
            var container = new Container();
            using var aScope1 = container.Resolve();
            using var aScope2 = container.Resolve();
            Assert.Same(aScope1.Value.Value, aScope1.Value.Value);
            Assert.NotSame(aScope1.Value.Value, aScope2.Value.Value);
        }
    }
}
