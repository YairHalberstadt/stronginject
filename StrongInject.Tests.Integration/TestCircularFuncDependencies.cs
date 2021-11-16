using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestCircularFuncDependencies
    {
        public record A(int I, Func<string, B> Func);

        public record B(string S, Func<int, A> Func);

        [Register<A>]
        [Register<B>]
        
        #pragma warning disable SI1107
        public partial class Container1 : IContainer<Func<int, A>>
        #pragma warning restore SI1107
        {
        }

        [Fact]
        public void TestCanResolveAndUseCircularFuncDependency()
        {
            new Container1().Run(x =>
            {
                var a1 = x(0);
                var b1 = a1.Func("b1");
                var a2 = b1.Func(1);
                var b2 = a2.Func("b2");
                Assert.Equal(0, a1.I);
                Assert.Equal("b1", b1.S);
                Assert.Equal(1, a2.I);
                Assert.Equal("b2", b2.S);
            });
        }
        
        public record C(int I, string S, Func<string, int, D> Func);

        public record D(int I, string S, Func<int, C> Func);

        [Register<C>]
        [Register<D>]
        
#pragma warning disable SI1107
        public partial class Container2 : IContainer<Func<int, C>>
#pragma warning restore SI1107
        {
            [Instance] private string defaultString = "";
        }
        
        [Register<C>]
        [Register<D>]
        public partial class Container3 : IContainer<Func<string, int, D>>
        {
        }
        
        [Fact]
        public void TestDoesNotAlwaysSeeUpdatedValueOfDelegateParam()
        {
            new Container2().Run(x =>
            {
                var c1 = x(0);
                var d1 = c1.Func("d1", 1);
                var c2 = d1.Func(2);
                var d2 = c2.Func("d2", 3);
                Assert.Equal(0, c1.I);
                Assert.Equal("", c1.S);
                Assert.Equal(1, d1.I);
                Assert.Equal("d1", d1.S);
                Assert.Equal(2, c2.I);
                Assert.Equal("", c2.S);
                Assert.Equal(3, d2.I);
                Assert.Equal("d2", d2.S);
            });
        }
        
        [Fact]
        public void TestAlwaysSeesUpdatedValueOfDelegateParamWhenNoWarning()
        {
            new Container3().Run(x =>
            {
                var d1 = x("d1", 0);
                var c1 = d1.Func(1);
                var d2 = c1.Func("d2", 2);
                var c2 = d2.Func(3);
                var d3 = c2.Func("d3", 4);
                Assert.Equal(0, d1.I);
                Assert.Equal("d1", d1.S);
                Assert.Equal(1, c1.I);
                Assert.Equal("d1", c1.S);
                Assert.Equal(2, d2.I);
                Assert.Equal("d2", d2.S);
                Assert.Equal(3, c2.I);
                Assert.Equal("d2", c2.S);
                Assert.Equal(4, d3.I);
                Assert.Equal("d3", d3.S);
            });
        }
    }
}
