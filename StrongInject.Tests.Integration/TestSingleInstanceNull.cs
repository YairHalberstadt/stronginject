using System;
using Xunit;

namespace StrongInject.Tests.Integration;

public partial class TestSingleInstanceNull
{
    public class A : IDisposable
    {
        public void Dispose()
        {
        }
    }
    
    public partial class Container : IContainer<A>
    {
        [Factory(Scope.SingleInstance)]
        public A? GetA()
        {
            Count++;
            return null;
        }

        public int Count { get; private set; }
    }

    [Fact]
    public void Test()
    {
        using var container = new Container();
        Assert.Equal(0, container.Count);
        Assert.Null(container.Run(x => x));
        Assert.Equal(1, container.Count);
        Assert.Null(container.Run(x => x));
        Assert.Equal(1, container.Count);
    }
}