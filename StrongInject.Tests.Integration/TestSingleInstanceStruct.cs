using System;
using Xunit;

namespace StrongInject.Tests.Integration;

public partial class TestSingleInstanceStruct
{
    public class IsDisposedHolder
    {
        public bool Disposed { get; private set; }
        public void Dispose()
        {
            Disposed = true;
        }
    }
    public struct A : IDisposable
    {
        public A()
        {
            _isDisposedHolder = new();
            Count++;
        }

        public static int Count { get; private set; }
        private readonly IsDisposedHolder _isDisposedHolder;
        public bool Disposed => _isDisposedHolder.Disposed;
        public void Dispose()
        {
            _isDisposedHolder.Dispose();
        }
    }

    public partial class ContainerA : IContainer<A>
    {
        [Factory(Scope.SingleInstance)] A GetA() => new();
    }

    [Fact]
    public void TestSingleInstanceFactoryMethod()
    {
        var container = new ContainerA();
        Assert.Equal(0, A.Count);
        var a1 = container.Run(x => x);
        Assert.Equal(1, A.Count);
        var a2 = container.Run(x => x);
        Assert.Equal(1, A.Count);
        Assert.False(a1.Disposed);
        Assert.False(a2.Disposed);
        container.Dispose();
        Assert.True(a1.Disposed);
        Assert.True(a2.Disposed);
    }
    
    public struct B : IDisposable
    {
        public B()
        {
            _isDisposedHolder = new();
            Count++;
        }

        public static int Count { get; private set; }
        private readonly IsDisposedHolder _isDisposedHolder;
        public bool Disposed => _isDisposedHolder.Disposed;
        public void Dispose()
        {
            _isDisposedHolder.Dispose();
        }
    }

    [Register<B>(Scope.SingleInstance)]
    public partial class ContainerB : IContainer<B>
    {
    }

    [Fact]
    public void TestSingleInstanceRegistration()
    {
        var container = new ContainerB();
        Assert.Equal(0, B.Count);
        var b1 = container.Run(x => x);
        Assert.Equal(1, B.Count);
        var b2 = container.Run(x => x);
        Assert.Equal(1, B.Count);
        Assert.False(b1.Disposed);
        Assert.False(b2.Disposed);
        container.Dispose();
        Assert.True(b1.Disposed);
        Assert.True(b2.Disposed);
    }
    
    public struct FactoryString : IFactory<string>, IDisposable
    {
        public FactoryString()
        {
            FactoryCreatedCount++;
        }

        public static int FactoryCreatedCount { get; private set; }
        public static int FactoryTargetCreatedCount { get; private set; }
        public static int DisposedCount { get; private set; }
        public void Dispose()
        {
            DisposedCount++;
        }

        public string Create()
        {
            FactoryTargetCreatedCount++;
            return "";
        }
    }

    [RegisterFactory(typeof(FactoryString), Scope.SingleInstance)]
    public partial class FactoryContainerString : IContainer<string>
    {
    }

    [Fact]
    public void TestSingleInstanceFactoryRegistration()
    {
        var container = new FactoryContainerString();
        Assert.Equal(0, FactoryString.FactoryCreatedCount);
        Assert.Equal(0, FactoryString.FactoryTargetCreatedCount);
        Assert.Equal(0, FactoryString.DisposedCount);
        container.Run(x => x);
        Assert.Equal(1, FactoryString.FactoryCreatedCount);
        Assert.Equal(1, FactoryString.FactoryTargetCreatedCount);
        container.Run(x => x);
        Assert.Equal(1, FactoryString.FactoryCreatedCount);
        Assert.Equal(2, FactoryString.FactoryTargetCreatedCount);
        Assert.Equal(0, FactoryString.DisposedCount);
        container.Dispose();
        Assert.Equal(1, FactoryString.DisposedCount);
    }
    
    public struct FactoryC : IFactory<C>, IDisposable
    {
        public FactoryC()
        {
            FactoryCreatedCount++;
        }

        public static int FactoryCreatedCount { get; private set; }
        public static int FactoryTargetCreatedCount { get; private set; }
        public static int DisposedCount { get; private set; }
        public void Dispose()
        {
            DisposedCount++;
        }

        public C Create()
        {
            FactoryTargetCreatedCount++;
            return new C();
        }
    }
    
    public struct C : IDisposable
    {
        public C()
        {
            _isDisposedHolder = new();
            Count++;
        }

        public static int Count { get; private set; }
        private readonly IsDisposedHolder _isDisposedHolder;
        public bool Disposed => _isDisposedHolder.Disposed;
        public void Dispose()
        {
            _isDisposedHolder.Dispose();
        }
    }

    [RegisterFactory(typeof(FactoryC), Scope.SingleInstance, Scope.SingleInstance)]
    public partial class FactoryContainerC : IContainer<C>
    {
    }

    [Fact]
    public void TestSingleInstanceFactoryTargetRegistration()
    {
        var container = new FactoryContainerC();
        Assert.Equal(0, C.Count);
        Assert.Equal(0, FactoryC.FactoryCreatedCount);
        Assert.Equal(0, FactoryC.FactoryTargetCreatedCount);
        Assert.Equal(0, FactoryC.DisposedCount);
        var c1 = container.Run(x => x);
        Assert.Equal(1, C.Count);
        Assert.Equal(1, FactoryC.FactoryCreatedCount);
        Assert.Equal(1, FactoryC.FactoryTargetCreatedCount);
        Assert.Equal(1, C.Count);
        var c2 = container.Run(x => x);
        Assert.Equal(1, C.Count);
        Assert.Equal(1, FactoryC.FactoryCreatedCount);
        Assert.Equal(1, FactoryC.FactoryTargetCreatedCount);
        Assert.Equal(0, FactoryC.DisposedCount);
        Assert.False(c1.Disposed);
        Assert.False(c2.Disposed);
        container.Dispose();
        Assert.True(c1.Disposed);
        Assert.True(c2.Disposed);
        Assert.Equal(1, FactoryC.DisposedCount);
    }
}