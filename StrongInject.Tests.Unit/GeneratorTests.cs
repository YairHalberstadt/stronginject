using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public class GeneratorTests : TestBase
    {
        public GeneratorTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void InstancePerResolutionDependencies()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, C c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C {}
public class D 
{
    public D(C c){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerResolutionDependenciesWithCasts()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), typeof(C), typeof(IC))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, IC c){}
}
public class B 
{
    public B(IC c, D d){}
}
public class C : IC {}
public class D 
{
    public D(C c){}
}
public interface IC {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
    }

        [Fact]
        public void InstancePerResolutionDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(B b, C c){}

    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
public class B 
{
    public B(C c, D d){}
}
public class C : IRequiresAsyncInitialization { public ValueTask InitializeAsync()  => new ValueTask();  }
public class D : E
{
    public D(C c){}
}

public class E : IRequiresAsyncInitialization
{
    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_2).InitializeAsync();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_2).InitializeAsync();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerResolutionDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(A))]
[FactoryRegistration(typeof(B))]
[FactoryRegistration(typeof(C))]
[FactoryRegistration(typeof(D))]
[Registration(typeof(C))]
public partial class Container : IAsyncContainer<AFactoryTarget>
{
}

public class A : IAsyncFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c){}
    ValueTask<AFactoryTarget> IAsyncFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IAsyncFactory<BFactoryTarget>
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IAsyncFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
}
public class BFactoryTarget {}
public class C : IAsyncFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IAsyncFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IAsyncFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IAsyncFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Registration(typeof(C))
                new DiagnosticResult("SI1001", @"Registration(typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.RunAsync<TResult, TParam>(global::System.Func<global::AFactoryTarget, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _4 = new global::C();
        var _7 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _6 = new global::D(_7);
        var _5 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).CreateAsync();
        var _3 = new global::B((global::C)_4, _5);
        var _2 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).CreateAsync();
        var _1 = new global::A(_2, _7);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        TResult result;
        try
        {
            result = await func((global::AFactoryTarget)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).ReleaseAsync(_2);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).ReleaseAsync(_5);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_7);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _4 = new global::C();
        var _7 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _6 = new global::D(_7);
        var _5 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).CreateAsync();
        var _3 = new global::B((global::C)_4, _5);
        var _2 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).CreateAsync();
        var _1 = new global::A(_2, _7);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).ReleaseAsync(_2);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).ReleaseAsync(_5);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_7);
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependencies()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), Scope.InstancePerDependency)]
[Registration(typeof(B))]
[Registration(typeof(C), Scope.InstancePerDependency)]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, C c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C {}
public class D 
{
    public D(C c){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _4 = new global::C();
        var _3 = new global::D((global::C)_4);
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _5 = new global::C();
        var _0 = new global::A((global::B)_1, (global::C)_5);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _4 = new global::C();
        var _3 = new global::D((global::C)_4);
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _5 = new global::C();
        var _0 = new global::A((global::B)_1, (global::C)_5);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithCasts()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), Scope.InstancePerDependency, typeof(C), typeof(IC))]
[Registration(typeof(D), Scope.InstancePerDependency)]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, IC c){}
}
public class B 
{
    public B(IC c, D d){}
}
public class C : IC {}
public class D 
{
    public D(C c){}
}
public interface IC {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _4 = new global::C();
        var _3 = new global::D((global::C)_4);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _5 = new global::C();
        var _0 = new global::A((global::B)_1, (global::IC)_5);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _4 = new global::C();
        var _3 = new global::D((global::C)_4);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _5 = new global::C();
        var _0 = new global::A((global::B)_1, (global::IC)_5);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A), Scope.InstancePerDependency)]
[Registration(typeof(B), Scope.InstancePerDependency)]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(B b, C c, B b1){}

    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
public class B 
{
    public B(C c, D d){}
}
public class C : IRequiresAsyncInitialization { public ValueTask InitializeAsync()  => new ValueTask();  }
public class D : E
{
    public D(C c){}
}

public class E : IRequiresAsyncInitialization
{
    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_2).InitializeAsync();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _4 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2, (global::B)_4);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_2).InitializeAsync();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _4 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2, (global::B)_4);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(A))]
[FactoryRegistration(typeof(B), Scope.InstancePerDependency)]
[FactoryRegistration(typeof(C), Scope.InstancePerResolution, Scope.InstancePerDependency)]
[FactoryRegistration(typeof(D), Scope.InstancePerDependency, Scope.InstancePerDependency)]
[Registration(typeof(C))]
public partial class Container : IAsyncContainer<AFactoryTarget>
{
}

public class A : IAsyncFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c, DFactoryTarget d){}
    ValueTask<AFactoryTarget> IAsyncFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IAsyncFactory<BFactoryTarget>
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IAsyncFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
}
public class BFactoryTarget {}
public class C : IAsyncFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IAsyncFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IAsyncFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IAsyncFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Registration(typeof(C))
                new DiagnosticResult("SI1001", @"Registration(typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.RunAsync<TResult, TParam>(global::System.Func<global::AFactoryTarget, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _4 = new global::C();
        var _7 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _6 = new global::D(_7);
        var _5 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).CreateAsync();
        var _3 = new global::B((global::C)_4, _5);
        var _2 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).CreateAsync();
        var _8 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _11 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _10 = new global::D(_11);
        var _9 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_10).CreateAsync();
        var _1 = new global::A(_2, _8, _9);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        TResult result;
        try
        {
            result = await func((global::AFactoryTarget)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_10).ReleaseAsync(_9);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_11);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_8);
            await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).ReleaseAsync(_2);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).ReleaseAsync(_5);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_7);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _4 = new global::C();
        var _7 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _6 = new global::D(_7);
        var _5 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).CreateAsync();
        var _3 = new global::B((global::C)_4, _5);
        var _2 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).CreateAsync();
        var _8 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _11 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _10 = new global::D(_11);
        var _9 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_10).CreateAsync();
        var _1 = new global::A(_2, _8, _9);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_10).ReleaseAsync(_9);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_11);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_8);
            await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_3).ReleaseAsync(_2);
            await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_6).ReleaseAsync(_5);
            await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_4).ReleaseAsync(_7);
        }

        );
    }
}");
        }

        [Fact]
        public void SingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), Scope.SingleInstance)]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D), Scope.SingleInstance)]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, C c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C {}
public class D 
{
    public D(C c){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }

        await this._lock1.WaitAsync();
        try
        {
            await (this._disposeAction1?.Invoke() ?? default);
        }
        finally
        {
            this._lock1.Release();
        }
    }

    private global::A _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::D _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private global::D GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        this._lock1.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::C();
            var _0 = new global::D((global::C)_1);
            this._singleInstanceField1 = _0;
            this._disposeAction1 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private global::A GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _2 = new global::C();
            var _3 = GetSingleInstanceField1();
            var _1 = new global::B((global::C)_2, (global::D)_3);
            var _0 = new global::A((global::B)_1, (global::C)_2);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWihCasts()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), Scope.SingleInstance, typeof(C), typeof(IC))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A 
{
    public A(B b, IC c){}
}
public class B 
{
    public B(IC c, D d){}
}
public class C : IC {}
public class D 
{
    public D(C c){}
}
public interface IC {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::C _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::C GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::C();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = GetSingleInstanceField0();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = GetSingleInstanceField0();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A), Scope.SingleInstance)]
[Registration(typeof(B))]
[Registration(typeof(C), Scope.SingleInstance)]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(B b, C c){}

    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
public class B 
{
    public B(C c, D d){}
}
public class C : IRequiresAsyncInitialization { public ValueTask InitializeAsync()  => new ValueTask();  }
public class D : E
{
    public D(C c){}
}

public class E : IRequiresAsyncInitialization
{
    ValueTask IRequiresAsyncInitialization.InitializeAsync() => new ValueTask();
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }

        await this._lock1.WaitAsync();
        try
        {
            await (this._disposeAction1?.Invoke() ?? default);
        }
        finally
        {
            this._lock1.Release();
        }
    }

    private global::A _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::C _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private async global::System.Threading.Tasks.ValueTask<global::C> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        await this._lock1.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::C();
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            this._singleInstanceField1 = _0;
            this._disposeAction1 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private async global::System.Threading.Tasks.ValueTask<global::A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _2 = await GetSingleInstanceField1();
            var _3 = new global::D((global::C)_2);
            await ((global::StrongInject.IRequiresAsyncInitialization)_3).InitializeAsync();
            var _1 = new global::B((global::C)_2, (global::D)_3);
            var _0 = new global::A((global::B)_1, (global::C)_2);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(A), Scope.SingleInstance, Scope.InstancePerResolution)]
[FactoryRegistration(typeof(B), Scope.SingleInstance, Scope.SingleInstance)]
[FactoryRegistration(typeof(C), Scope.InstancePerResolution, Scope.SingleInstance)]
[FactoryRegistration(typeof(D), Scope.InstancePerResolution, Scope.InstancePerResolution)]
[Registration(typeof(C), Scope.InstancePerResolution, typeof(C))]
public partial class Container : IAsyncContainer<AFactoryTarget>
{
}

public class A : IAsyncFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c){}
    ValueTask<AFactoryTarget> IAsyncFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IAsyncFactory<BFactoryTarget>
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IAsyncFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
}
public class BFactoryTarget {}
public class C : IAsyncFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IAsyncFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IAsyncFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IAsyncFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Registration(typeof(C), Scope.InstancePerResolution, typeof(C))
                new DiagnosticResult("SI1001", @"Registration(typeof(C), Scope.InstancePerResolution, typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }

        await this._lock1.WaitAsync();
        try
        {
            await (this._disposeAction1?.Invoke() ?? default);
        }
        finally
        {
            this._lock1.Release();
        }

        await this._lock2.WaitAsync();
        try
        {
            await (this._disposeAction2?.Invoke() ?? default);
        }
        finally
        {
            this._lock2.Release();
        }

        await this._lock3.WaitAsync();
        try
        {
            await (this._disposeAction3?.Invoke() ?? default);
        }
        finally
        {
            this._lock3.Release();
        }
    }

    private global::A _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::BFactoryTarget _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private global::B _singleInstanceField2;
    private global::System.Threading.SemaphoreSlim _lock2 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction2;
    private global::CFactoryTarget _singleInstanceField3;
    private global::System.Threading.SemaphoreSlim _lock3 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction3;
    private async global::System.Threading.Tasks.ValueTask<global::CFactoryTarget> GetSingleInstanceField3()
    {
        if (!object.ReferenceEquals(_singleInstanceField3, null))
            return _singleInstanceField3;
        await this._lock3.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::C();
            var _0 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_1).CreateAsync();
            this._singleInstanceField3 = _0;
            this._disposeAction3 = async () =>
            {
                await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_1).ReleaseAsync(_0);
            }

            ;
        }
        finally
        {
            this._lock3.Release();
        }

        return _singleInstanceField3;
    }

    private async global::System.Threading.Tasks.ValueTask<global::B> GetSingleInstanceField2()
    {
        if (!object.ReferenceEquals(_singleInstanceField2, null))
            return _singleInstanceField2;
        await this._lock2.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::C();
            var _4 = await GetSingleInstanceField3();
            var _3 = new global::D(_4);
            var _2 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_3).CreateAsync();
            var _0 = new global::B((global::C)_1, _2);
            this._singleInstanceField2 = _0;
            this._disposeAction2 = async () =>
            {
                await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_3).ReleaseAsync(_2);
            }

            ;
        }
        finally
        {
            this._lock2.Release();
        }

        return _singleInstanceField2;
    }

    private async global::System.Threading.Tasks.ValueTask<global::BFactoryTarget> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        await this._lock1.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = await GetSingleInstanceField2();
            var _0 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_1).CreateAsync();
            this._singleInstanceField1 = _0;
            this._disposeAction1 = async () =>
            {
                await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_1).ReleaseAsync(_0);
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private async global::System.Threading.Tasks.ValueTask<global::A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = await GetSingleInstanceField1();
            var _2 = await GetSingleInstanceField3();
            var _0 = new global::A(_1, _2);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.RunAsync<TResult, TParam>(global::System.Func<global::AFactoryTarget, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = await GetSingleInstanceField0();
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        TResult result;
        try
        {
            result = await func((global::AFactoryTarget)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = await GetSingleInstanceField0();
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void MultipleResolvesShareSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), Scope.SingleInstance, typeof(C), typeof(IC))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>, IAsyncContainer<B>
{
}

public class A 
{
    public A(IC c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C : IC {}
public class D 
{
    public D(C c){}
}
public interface IC {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::C _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::C GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::C();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::IC)_1);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::IC)_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::B>.RunAsync<TResult, TParam>(global::System.Func<global::B, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _2 = new global::D((global::C)_1);
        var _0 = new global::B((global::C)_1, (global::D)_2);
        TResult result;
        try
        {
            result = await func((global::B)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::B>> global::StrongInject.IAsyncContainer<global::B>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _2 = new global::D((global::C)_1);
        var _0 = new global::B((global::C)_1, (global::D)_2);
        return new global::StrongInject.AsyncOwned<global::B>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void ReportMissingTypes()
        {
            string userSource = @"";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (1,1): Error SI0201: Missing Type 'StrongInject.IContainer`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IAsyncContainer`1'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IInstanceProvider`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IAsyncInstanceProvider`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IFactory`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IAsyncFactory`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IRequiresInitialization'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.IRequiresAsyncInitialization'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.Owned`1[T]'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.AsyncOwned`1'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.RegistrationAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.ModuleRegistrationAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.FactoryRegistrationAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.FactoryAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.Helpers'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void RegistersInstanceProviderFields()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<A>, IAsyncContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<int[]>
{
    public InstanceProvider _instanceProvider1;
    internal IAsyncInstanceProvider _instanceProvider2;
    private IAsyncInstanceProvider<int[]> _instanceProvider3;
}

public class A {}
public class B {}
public class C {}
public class D {}

public class InstanceProvider : IAsyncInstanceProvider<A>, IAsyncInstanceProvider<B>
{
    public ValueTask<A> GetAsync() => throw null;
    ValueTask<B> IAsyncInstanceProvider<B>.GetAsync() => throw null;
}

public interface IAsyncInstanceProvider : IAsyncInstanceProvider<C>, IAsyncInstanceProvider<D>
{
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (8,37): Warning CS0649: Field 'Container._instanceProvider2' is never assigned to, and will always have its default value null
                // _instanceProvider2
                new DiagnosticResult("CS0649", @"_instanceProvider2", DiagnosticSeverity.Warning).WithLocation(8, 37),
                // (9,43): Warning CS0649: Field 'Container._instanceProvider3' is never assigned to, and will always have its default value null
                // _instanceProvider3
                new DiagnosticResult("CS0649", @"_instanceProvider3", DiagnosticSeverity.Warning).WithLocation(9, 43));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::A>)this._instanceProvider1).GetAsync();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::A>)this._instanceProvider1).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::A>)this._instanceProvider1).GetAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::A>)this._instanceProvider1).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::B>.RunAsync<TResult, TParam>(global::System.Func<global::B, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::B>)this._instanceProvider1).GetAsync();
        TResult result;
        try
        {
            result = await func((global::B)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::B>)this._instanceProvider1).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::B>> global::StrongInject.IAsyncContainer<global::B>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::B>)this._instanceProvider1).GetAsync();
        return new global::StrongInject.AsyncOwned<global::B>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::B>)this._instanceProvider1).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::C>)this._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::C)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::C>)this._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::C>)this._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::C>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::C>)this._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::D)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::D>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Int32[]>.RunAsync<TResult, TParam>(global::System.Func<global::System.Int32[], TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)this._instanceProvider3).GetAsync();
        TResult result;
        try
        {
            result = await func((global::System.Int32[])_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)this._instanceProvider3).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Int32[]>> global::StrongInject.IAsyncContainer<global::System.Int32[]>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)this._instanceProvider3).GetAsync();
        return new global::StrongInject.AsyncOwned<global::System.Int32[]>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)this._instanceProvider3).ReleaseAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void CanUseStaticInstanceProviderFields()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<A>, IAsyncContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<int[]>
{
    public static InstanceProvider _instanceProvider1;
    internal static IAsyncInstanceProvider _instanceProvider2;
    private static IAsyncInstanceProvider<int[]> _instanceProvider3;
}

public class A {}
public class B {}
public class C {}
public class D {}

public class InstanceProvider : IAsyncInstanceProvider<A>, IAsyncInstanceProvider<B>
{
    public ValueTask<A> GetAsync() => throw null;
    ValueTask<B> IAsyncInstanceProvider<B>.GetAsync() => throw null;
}

public interface IAsyncInstanceProvider : IAsyncInstanceProvider<C>, IAsyncInstanceProvider<D>
{
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (8,44): Warning CS0649: Field 'Container._instanceProvider2' is never assigned to, and will always have its default value null
                // _instanceProvider2
                new DiagnosticResult("CS0649", @"_instanceProvider2", DiagnosticSeverity.Warning).WithLocation(8, 44),
                // (9,50): Warning CS0649: Field 'Container._instanceProvider3' is never assigned to, and will always have its default value null
                // _instanceProvider3
                new DiagnosticResult("CS0649", @"_instanceProvider3", DiagnosticSeverity.Warning).WithLocation(9, 50));

            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::A>)global::Container._instanceProvider1).GetAsync();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::A>)global::Container._instanceProvider1).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::A>)global::Container._instanceProvider1).GetAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::A>)global::Container._instanceProvider1).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::B>.RunAsync<TResult, TParam>(global::System.Func<global::B, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::B>)global::Container._instanceProvider1).GetAsync();
        TResult result;
        try
        {
            result = await func((global::B)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::B>)global::Container._instanceProvider1).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::B>> global::StrongInject.IAsyncContainer<global::B>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::B>)global::Container._instanceProvider1).GetAsync();
        return new global::StrongInject.AsyncOwned<global::B>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::B>)global::Container._instanceProvider1).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::C>)global::Container._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::C)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::C>)global::Container._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::C>)global::Container._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::C>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::C>)global::Container._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)global::Container._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::D)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)global::Container._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)global::Container._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::D>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)global::Container._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Int32[]>.RunAsync<TResult, TParam>(global::System.Func<global::System.Int32[], TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)global::Container._instanceProvider3).GetAsync();
        TResult result;
        try
        {
            result = await func((global::System.Int32[])_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)global::Container._instanceProvider3).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Int32[]>> global::StrongInject.IAsyncContainer<global::System.Int32[]>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)global::Container._instanceProvider3).GetAsync();
        return new global::StrongInject.AsyncOwned<global::System.Int32[]>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32[]>)global::Container._instanceProvider3).ReleaseAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void DependenciesAreOverriddenByInstanceProviderFields()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
    public InstanceProvider _instanceProvider;
}

public class A
{
    public A(B b, IC c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C : IC {}
public interface IC {}
public class D
{
    public D(C c){}
}

public class InstanceProvider : IAsyncInstanceProvider<IC>, IAsyncInstanceProvider<D>
{
    public ValueTask<IC> GetAsync() => throw null;
    ValueTask<D> IAsyncInstanceProvider<D>.GetAsync() => throw null;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider).GetAsync();
        var _1 = new global::B((global::C)_2, _3);
        var _4 = await ((global::StrongInject.IAsyncInstanceProvider<global::IC>)this._instanceProvider).GetAsync();
        var _0 = new global::A((global::B)_1, _4);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::IC>)this._instanceProvider).ReleaseAsync(_4);
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider).ReleaseAsync(_3);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::C();
        var _3 = await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider).GetAsync();
        var _1 = new global::B((global::C)_2, _3);
        var _4 = await ((global::StrongInject.IAsyncInstanceProvider<global::IC>)this._instanceProvider).GetAsync();
        var _0 = new global::A((global::B)_1, _4);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::IC>)this._instanceProvider).ReleaseAsync(_4);
            await ((global::StrongInject.IAsyncInstanceProvider<global::D>)this._instanceProvider).ReleaseAsync(_3);
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfMultipleInstanceProviderFieldsProvideSameType()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<int>, IAsyncContainer<string>, IAsyncContainer<bool>
{
    public InstanceProvider1 _instanceProvider1;
    internal InstanceProvider2 _instanceProvider2;
    private IAsyncInstanceProvider<int> _instanceProvider3;
}

public class InstanceProvider1 : IAsyncInstanceProvider<int>, IAsyncInstanceProvider<bool>
{
    public ValueTask<bool> GetAsync() => throw null;
    ValueTask<int> IAsyncInstanceProvider<int>.GetAsync() => throw null;
}

public class InstanceProvider2 : IAsyncInstanceProvider<string>, IAsyncInstanceProvider<bool>
{
    public ValueTask<string> GetAsync() => throw null;
    ValueTask<bool> IAsyncInstanceProvider<bool>.GetAsync() => throw null;
}

";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,30): Error SI0015: Both fields 'Container._instanceProvider1' and 'Container._instanceProvider2' are instance providers for 'bool'
                // _instanceProvider1
                new DiagnosticResult("SI0015", @"_instanceProvider1", DiagnosticSeverity.Error).WithLocation(7, 30),
                // (7,30): Error SI0015: Both fields 'Container._instanceProvider1' and 'Container._instanceProvider3' are instance providers for 'int'
                // _instanceProvider1
                new DiagnosticResult("SI0015", @"_instanceProvider1", DiagnosticSeverity.Error).WithLocation(7, 30),
                // (8,32): Error SI0015: Both fields 'Container._instanceProvider1' and 'Container._instanceProvider2' are instance providers for 'bool'
                // _instanceProvider2
                new DiagnosticResult("SI0015", @"_instanceProvider2", DiagnosticSeverity.Error).WithLocation(8, 32),
                // (9,41): Error SI0015: Both fields 'Container._instanceProvider1' and 'Container._instanceProvider3' are instance providers for 'int'
                // _instanceProvider3
                new DiagnosticResult("SI0015", @"_instanceProvider3", DiagnosticSeverity.Error).WithLocation(9, 41));
            comp.GetDiagnostics().Verify(
                // (8,32): Warning CS0649: Field 'Container._instanceProvider2' is never assigned to, and will always have its default value null
                // _instanceProvider2
                new DiagnosticResult("CS0649", @"_instanceProvider2", DiagnosticSeverity.Warning).WithLocation(8, 32),
                // (9,41): Warning CS0649: Field 'Container._instanceProvider3' is never assigned to, and will always have its default value null
                // _instanceProvider3
                new DiagnosticResult("CS0649", @"_instanceProvider3", DiagnosticSeverity.Warning).WithLocation(9, 41));

            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Int32>.RunAsync<TResult, TParam>(global::System.Func<global::System.Int32, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this._instanceProvider3).GetAsync();
        TResult result;
        try
        {
            result = await func((global::System.Int32)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this._instanceProvider3).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Int32>> global::StrongInject.IAsyncContainer<global::System.Int32>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this._instanceProvider3).GetAsync();
        return new global::StrongInject.AsyncOwned<global::System.Int32>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this._instanceProvider3).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.String>.RunAsync<TResult, TParam>(global::System.Func<global::System.String, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.String>)this._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::System.String)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.String>)this._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.String>> global::StrongInject.IAsyncContainer<global::System.String>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.String>)this._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::System.String>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.String>)this._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Boolean>)this._instanceProvider2).GetAsync();
        TResult result;
        try
        {
            result = await func((global::System.Boolean)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Boolean>)this._instanceProvider2).ReleaseAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Boolean>)this._instanceProvider2).GetAsync();
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Boolean>)this._instanceProvider2).ReleaseAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void CorrectDisposal()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;
using System;

[FactoryRegistration(typeof(A))]
[FactoryRegistration(typeof(B), Scope.SingleInstance, Scope.SingleInstance)]
[FactoryRegistration(typeof(C), Scope.InstancePerResolution, Scope.SingleInstance)]
[FactoryRegistration(typeof(D), Scope.InstancePerResolution, Scope.InstancePerResolution)]
[Registration(typeof(C))]
[Registration(typeof(E))]
[Registration(typeof(F))]
[Registration(typeof(G))]
[Registration(typeof(H))]
[Registration(typeof(I), Scope.SingleInstance)]
public partial class Container : IAsyncContainer<AFactoryTarget>
{
    IAsyncInstanceProvider<int> instanceProvider;
}

public class A : IAsyncFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c, E e, int i){}
    ValueTask<AFactoryTarget> IAsyncFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IAsyncFactory<BFactoryTarget>, IDisposable
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IAsyncFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
    public void Dispose() {}
}
public class BFactoryTarget {}
public class C : IAsyncFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IAsyncFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IAsyncFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IAsyncFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
public class E : IDisposable { public E(F f) {} public void Dispose() {} }
public class F : IAsyncDisposable { public F(G g) {} ValueTask IAsyncDisposable.DisposeAsync() => default; }
public class G : IDisposable, IAsyncDisposable { public G(H h) {} void IDisposable.Dispose() {} public ValueTask DisposeAsync() => default; }
public class H { public H(I i) {} }
public class I : IDisposable { public I(int i) {} public void Dispose() {} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (10,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Registration(typeof(C))
                new DiagnosticResult("SI1001", @"Registration(typeof(C))", DiagnosticSeverity.Warning).WithLocation(10, 2));
            comp.GetDiagnostics().Verify(
                // (18,33): Warning CS0649: Field 'Container.instanceProvider' is never assigned to, and will always have its default value null
                // instanceProvider
                new DiagnosticResult("CS0649", @"instanceProvider", DiagnosticSeverity.Warning).WithLocation(18, 33));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock3.WaitAsync();
        try
        {
            await (this._disposeAction3?.Invoke() ?? default);
        }
        finally
        {
            this._lock3.Release();
        }

        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }

        await this._lock1.WaitAsync();
        try
        {
            await (this._disposeAction1?.Invoke() ?? default);
        }
        finally
        {
            this._lock1.Release();
        }

        await this._lock2.WaitAsync();
        try
        {
            await (this._disposeAction2?.Invoke() ?? default);
        }
        finally
        {
            this._lock2.Release();
        }
    }

    private global::BFactoryTarget _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::B _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private global::CFactoryTarget _singleInstanceField2;
    private global::System.Threading.SemaphoreSlim _lock2 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction2;
    private async global::System.Threading.Tasks.ValueTask<global::CFactoryTarget> GetSingleInstanceField2()
    {
        if (!object.ReferenceEquals(_singleInstanceField2, null))
            return _singleInstanceField2;
        await this._lock2.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::C();
            var _0 = await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_1).CreateAsync();
            this._singleInstanceField2 = _0;
            this._disposeAction2 = async () =>
            {
                await ((global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_1).ReleaseAsync(_0);
            }

            ;
        }
        finally
        {
            this._lock2.Release();
        }

        return _singleInstanceField2;
    }

    private async global::System.Threading.Tasks.ValueTask<global::B> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        await this._lock1.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::C();
            var _4 = await GetSingleInstanceField2();
            var _3 = new global::D(_4);
            var _2 = await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_3).CreateAsync();
            var _0 = new global::B((global::C)_1, _2);
            this._singleInstanceField1 = _0;
            this._disposeAction1 = async () =>
            {
                ((global::System.IDisposable)_0).Dispose();
                await ((global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_3).ReleaseAsync(_2);
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private async global::System.Threading.Tasks.ValueTask<global::BFactoryTarget> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = await GetSingleInstanceField1();
            var _0 = await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_1).CreateAsync();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
                await ((global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_1).ReleaseAsync(_0);
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    private global::I _singleInstanceField3;
    private global::System.Threading.SemaphoreSlim _lock3 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction3;
    private async global::System.Threading.Tasks.ValueTask<global::I> GetSingleInstanceField3()
    {
        if (!object.ReferenceEquals(_singleInstanceField3, null))
            return _singleInstanceField3;
        await this._lock3.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).GetAsync();
            var _0 = new global::I(_1);
            this._singleInstanceField3 = _0;
            this._disposeAction3 = async () =>
            {
                ((global::System.IDisposable)_0).Dispose();
                await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).ReleaseAsync(_1);
            }

            ;
        }
        finally
        {
            this._lock3.Release();
        }

        return _singleInstanceField3;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.RunAsync<TResult, TParam>(global::System.Func<global::AFactoryTarget, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = await GetSingleInstanceField0();
        var _3 = await GetSingleInstanceField2();
        var _8 = await GetSingleInstanceField3();
        var _7 = new global::H((global::I)_8);
        var _6 = new global::G((global::H)_7);
        var _5 = new global::F((global::G)_6);
        var _4 = new global::E((global::F)_5);
        var _9 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).GetAsync();
        var _1 = new global::A(_2, _3, (global::E)_4, _9);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        TResult result;
        try
        {
            result = await func((global::AFactoryTarget)_0, param);
        }
        finally
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).ReleaseAsync(_9);
            ((global::System.IDisposable)_4).Dispose();
            await ((global::System.IAsyncDisposable)_5).DisposeAsync();
            await ((global::System.IAsyncDisposable)_6).DisposeAsync();
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = await GetSingleInstanceField0();
        var _3 = await GetSingleInstanceField2();
        var _8 = await GetSingleInstanceField3();
        var _7 = new global::H((global::I)_8);
        var _6 = new global::G((global::H)_7);
        var _5 = new global::F((global::G)_6);
        var _4 = new global::E((global::F)_5);
        var _9 = await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).GetAsync();
        var _1 = new global::A(_2, _3, (global::E)_4, _9);
        var _0 = await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).CreateAsync();
        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0, async () =>
        {
            await ((global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_1).ReleaseAsync(_0);
            await ((global::StrongInject.IAsyncInstanceProvider<global::System.Int32>)this.instanceProvider).ReleaseAsync(_9);
            ((global::System.IDisposable)_4).Dispose();
            await ((global::System.IAsyncDisposable)_5).DisposeAsync();
            await ((global::System.IAsyncDisposable)_6).DisposeAsync();
        }

        );
    }
}");
        }

        [Fact]
        public void GeneratesContainerInNamespace()
        {
            string userSource = @"
using StrongInject;

namespace N.O.P
{
    [Registration(typeof(A))]
    public partial class Container : IAsyncContainer<A>
    {
    }

    public class A 
    {
    }
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
namespace N.O.P
{
    partial class Container
    {
        private int _disposed = 0;
        private bool Disposed => _disposed != 0;
        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
            if (disposed != 0)
                return;
        }

        async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::N.O.P.A>.RunAsync<TResult, TParam>(global::System.Func<global::N.O.P.A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
        {
            if (Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::N.O.P.A();
            TResult result;
            try
            {
                result = await func((global::N.O.P.A)_0, param);
            }
            finally
            {
            }

            return result;
        }

        async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::N.O.P.A>> global::StrongInject.IAsyncContainer<global::N.O.P.A>.ResolveAsync()
        {
            if (Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::N.O.P.A();
            return new global::StrongInject.AsyncOwned<global::N.O.P.A>(_0, async () =>
            {
            }

            );
        }
    }
}");
        }

        [Fact]
        public void GeneratesContainerInNestedType()
        {
            string userSource = @"
using StrongInject;

namespace N.O.P
{
    public partial class Outer1
    {
        public partial class Outer2
        {
            [Registration(typeof(A))]
            public partial class Container : IAsyncContainer<A>
            {
            }

            public class A 
            {
            }
        }
    }
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
namespace N.O.P
{
    partial class Outer1
    {
        partial class Outer2
        {
            partial class Container
            {
                private int _disposed = 0;
                private bool Disposed => _disposed != 0;
                public async global::System.Threading.Tasks.ValueTask DisposeAsync()
                {
                    var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
                    if (disposed != 0)
                        return;
                }

                async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::N.O.P.Outer1.Outer2.A>.RunAsync<TResult, TParam>(global::System.Func<global::N.O.P.Outer1.Outer2.A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
                {
                    if (Disposed)
                        throw new global::System.ObjectDisposedException(nameof(Container));
                    var _0 = new global::N.O.P.Outer1.Outer2.A();
                    TResult result;
                    try
                    {
                        result = await func((global::N.O.P.Outer1.Outer2.A)_0, param);
                    }
                    finally
                    {
                    }

                    return result;
                }

                async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::N.O.P.Outer1.Outer2.A>> global::StrongInject.IAsyncContainer<global::N.O.P.Outer1.Outer2.A>.ResolveAsync()
                {
                    if (Disposed)
                        throw new global::System.ObjectDisposedException(nameof(Container));
                    var _0 = new global::N.O.P.Outer1.Outer2.A();
                    return new global::StrongInject.AsyncOwned<global::N.O.P.Outer1.Outer2.A>(_0, async () =>
                    {
                    }

                    );
                }
            }
        }
    }
}");
        }

        [Fact]
        public void GeneratesContainerInGenericNestedType()
        {
            string userSource = @"
using StrongInject;

namespace N.O.P
{
    public partial class Outer1<T>
    {
        public partial class Outer2<T1, T2>
        {
            [Registration(typeof(A))]
            public partial class Container : IAsyncContainer<A>
            {
            }
        }
    }

    public class A 
    {
    }
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
namespace N.O.P
{
    partial class Outer1<T>
    {
        partial class Outer2<T1, T2>
        {
            partial class Container
            {
                private int _disposed = 0;
                private bool Disposed => _disposed != 0;
                public async global::System.Threading.Tasks.ValueTask DisposeAsync()
                {
                    var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
                    if (disposed != 0)
                        return;
                }

                async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::N.O.P.A>.RunAsync<TResult, TParam>(global::System.Func<global::N.O.P.A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
                {
                    if (Disposed)
                        throw new global::System.ObjectDisposedException(nameof(Container));
                    var _0 = new global::N.O.P.A();
                    TResult result;
                    try
                    {
                        result = await func((global::N.O.P.A)_0, param);
                    }
                    finally
                    {
                    }

                    return result;
                }

                async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::N.O.P.A>> global::StrongInject.IAsyncContainer<global::N.O.P.A>.ResolveAsync()
                {
                    if (Disposed)
                        throw new global::System.ObjectDisposedException(nameof(Container));
                    var _0 = new global::N.O.P.A();
                    return new global::StrongInject.AsyncOwned<global::N.O.P.A>(_0, async () =>
                    {
                    }

                    );
                }
            }
        }
    }
}");
        }

        [Fact]
        public void GeneratesThrowingImplementationForContainerWithMissingDependencies()
        {
            string userSource = @"
using StrongInject;

public class A {}

partial class Container : IAsyncContainer<A>
{
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,15): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 15));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfConstructorParameterPassedByRef()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A
{
    public A(ref B b){}
}
public class B{}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (4,2): Error SI0019: parameter 'ref B' of constructor 'A.A(ref B)' is passed as 'Ref'.
                // Registration(typeof(A))
                new DiagnosticResult("SI0019", @"Registration(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfFactoryConstructorParameterPassedByRef()
        {
            string userSource = @"
using StrongInject;

[FactoryRegistration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<int>
{
}

public class A : IFactory<int>
{
    public A(ref B b){}
    public int Create() => 42;
}
public class B{}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (4,2): Error SI0019: parameter 'ref B' of constructor 'A.A(ref B)' is passed as 'Ref'.
                // FactoryRegistration(typeof(A))
                new DiagnosticResult("SI0019", @"FactoryRegistration(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Int32>.Run<TResult, TParam>(global::System.Func<global::System.Int32, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer1()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': 'A' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer2()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer3()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(A))]
public partial class Container : IContainer<int>
{
}

public class A : IAsyncFactory<int>
{
    public ValueTask<int> CreateAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0103: Error while resolving dependencies for 'int': 'int' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer4()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(A))]
public partial class Container : IContainer<int>
{
}

public class A : IFactory<int>, IRequiresAsyncInitialization
{
    public int Create() => default;
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0103: Error while resolving dependencies for 'int': 'StrongInject.IFactory<int>' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer5()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<int>
{
    public IAsyncInstanceProvider<int> _instanceProvider;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (4,22): Error SI0103: Error while resolving dependencies for 'int': 'int' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer6()
        {
            string userSource = @"
using StrongInject;

[FactoryRegistration(typeof(A))]
public partial class Container : IContainer<int>
{
    public IAsyncInstanceProvider<B> _instanceProvider;
}

public class A : IFactory<int>
{
    public A(B b) {}
    public int Create() => default;
}
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (5,22): Error SI0103: Error while resolving dependencies for 'int': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorIfAsyncTypeRequiredByContainer7()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[FactoryRegistration(typeof(C))]
[Registration(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b) {} }
public class B {}
public class C : IAsyncFactory<B>
{
    public ValueTask<B> CreateAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22) );
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void CanGenerateSynchronousContainer()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = new global::A((global::B)_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = new global::A((global::B)_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresInitialization { public A(B b){} public void Initialize() {}}
public class B : IRequiresInitialization { public void Initialize() {} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        ((global::StrongInject.IRequiresInitialization)_1).Initialize();
        var _0 = new global::A((global::B)_1);
        ((global::StrongInject.IRequiresInitialization)_0).Initialize();
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        ((global::StrongInject.IRequiresInitialization)_1).Initialize();
        var _0 = new global::A((global::B)_1);
        ((global::StrongInject.IRequiresInitialization)_0).Initialize();
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithFactories()
        {
            string userSource = @"
using StrongInject;

[FactoryRegistration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<int>
{
}

public class A : IFactory<int> { public A(B b){} public int Create() => default; }
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Int32>.Run<TResult, TParam>(global::System.Func<global::System.Int32, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::B();
        var _1 = new global::A((global::B)_2);
        var _0 = ((global::StrongInject.IFactory<global::System.Int32>)_1).Create();
        TResult result;
        try
        {
            result = func((global::System.Int32)_0, param);
        }
        finally
        {
            ((global::StrongInject.IFactory<global::System.Int32>)_1).Release(_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _2 = new global::B();
        var _1 = new global::A((global::B)_2);
        var _0 = ((global::StrongInject.IFactory<global::System.Int32>)_1).Create();
        return new global::StrongInject.Owned<global::System.Int32>(_0, () =>
        {
            ((global::StrongInject.IFactory<global::System.Int32>)_1).Release(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithInstanceProviders()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
    IInstanceProvider<int> _instanceProvider;
}

public class A { public A(B b, int i){} }
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (8,28): Warning CS0649: Field 'Container._instanceProvider' is never assigned to, and will always have its default value null
                // _instanceProvider
                new DiagnosticResult("CS0649", @"_instanceProvider", DiagnosticSeverity.Warning).WithLocation(8, 28));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _2 = ((global::StrongInject.IInstanceProvider<global::System.Int32>)this._instanceProvider).Get();
        var _0 = new global::A((global::B)_1, _2);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
            ((global::StrongInject.IInstanceProvider<global::System.Int32>)this._instanceProvider).Release(_2);
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _2 = ((global::StrongInject.IInstanceProvider<global::System.Int32>)this._instanceProvider).Get();
        var _0 = new global::A((global::B)_1, _2);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
            ((global::StrongInject.IInstanceProvider<global::System.Int32>)this._instanceProvider).Release(_2);
        }

        );
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::B _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::B GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::B();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void SynchronousAndAsynchronousResolvesCanShareSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
[Registration(typeof(B), Scope.SingleInstance)]
[Registration(typeof(C))]
[Registration(typeof(D), Scope.SingleInstance)]
public partial class Container : IContainer<A>, IAsyncContainer<C>
{
}

public class A { public A(B b){} }
public class B {}
public class C : IRequiresAsyncInitialization { public C(B b, D d) {} public ValueTask InitializeAsync() => default; }
public class D : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock1.WaitAsync();
        try
        {
            await (this._disposeAction1?.Invoke() ?? default);
        }
        finally
        {
            this._lock1.Release();
        }

        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }
    }

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    private global::B _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::B GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::B();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }

    private global::D _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private async global::System.Threading.Tasks.ValueTask<global::D> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        await this._lock1.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::D();
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            this._singleInstanceField1 = _0;
            this._disposeAction1 = async () =>
            {
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _2 = await GetSingleInstanceField1();
        var _0 = new global::C((global::B)_1, (global::D)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func((global::C)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _2 = await GetSingleInstanceField1();
        var _0 = new global::C((global::B)_1, (global::D)_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::C>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void DisposalOfSingleInstanceDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IDisposable { public void Dispose(){} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::B _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::B GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::B();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0).Dispose();
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void DisposalOfMultipleSingleInstanceDependencies()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B), Scope.SingleInstance)]
[Registration(typeof(C), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IDisposable { public B(C c){} public void Dispose(){} }
public class C : IDisposable { public void Dispose(){} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }

        this._lock1.Wait();
        try
        {
            this._disposeAction1?.Invoke();
        }
        finally
        {
            this._lock1.Release();
        }
    }

    private global::B _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::C _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction1;
    private global::C GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        this._lock1.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::C();
            this._singleInstanceField1 = _0;
            this._disposeAction1 = () =>
            {
                ((global::System.IDisposable)_0).Dispose();
            }

            ;
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private global::B GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = GetSingleInstanceField1();
            var _0 = new global::B((global::C)_1);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0).Dispose();
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = GetSingleInstanceField0();
        var _0 = new global::A((global::B)_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void DoesNotDisposeUnusedSingleInstanceDependencies()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A), Scope.SingleInstance)]
[Registration(typeof(B), Scope.SingleInstance)]
[Registration(typeof(C), Scope.SingleInstance)]
public partial class Container : IContainer<C>
{
}

public class A : IDisposable { public A(A a){} public void Dispose(){} }
public class B : IDisposable { public void Dispose(){} }
public class C : IDisposable { public void Dispose(){} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::C _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::C GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _0 = new global::C();
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0).Dispose();
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func((global::C)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::C>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithoutParameters()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IAsyncContainer<Func<A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::A>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
        global::System.Func<global::A> _0 = () =>
        {
            var _1 = new global::B();
            var _0 = new global::A((global::B)_1);
            disposeActions1_0.Add(async () =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = await func((global::System.Func<global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                await disposeAction();
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
        global::System.Func<global::A> _0 = () =>
        {
            var _1 = new global::B();
            var _0 = new global::A((global::B)_1);
            disposeActions1_0.Add(async () =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0, async () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                await disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithParametersWhereParameterTypeIsRegistered()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::B, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::B, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithParametersWhereParameterTypeIsNotRegistered()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::B, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::B, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncUsedAsParameter()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A 
{
    public A(Func<int, string, B> b){}
}
public class B { public B(int i, string s, int i1){} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_1 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.String, global::B> _1 = (param0_0, param0_1) =>
        {
            var _0 = new global::B(param0_0, param0_1, param0_0);
            disposeActions1_1.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        var _0 = new global::A(_1);
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_1)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_1 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.String, global::B> _1 = (param0_0, param0_1) =>
        {
            var _0 = new global::B(param0_0, param0_1, param0_0);
            disposeActions1_1.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        var _0 = new global::A(_1);
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_1)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncUsedInsideFuncResolution()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(B))]
public partial class Container : IContainer<Func<int, A>>
{
}

public class A 
{
    public A(int a, Func<string, B> func){}
}
public class B { public B(int i, string s){} }
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::System.Int32, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::A> _0 = (param0_0) =>
        {
            var disposeActions2_1 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.String, global::B> _1 = (param1_0) =>
            {
                var _0 = new global::B(param0_0, param1_0);
                disposeActions2_1.Add(() =>
                {
                }

                );
                return _0;
            }

            ;
            var _0 = new global::A(param0_0, _1);
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_1)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::System.Int32, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::A>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::A> _0 = (param0_0) =>
        {
            var disposeActions2_1 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.String, global::B> _1 = (param1_0) =>
            {
                var _0 = new global::B(param0_0, param1_0);
                disposeActions2_1.Add(() =>
                {
                }

                );
                return _0;
            }

            ;
            var _0 = new global::A(param0_0, _1);
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_1)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CanResolveFuncOfFuncOfFunc()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<Func<bool, Func<string, Func<int, A>>>>
{
}

public class A 
{
    public A(int a, string b, bool c){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>> _0 = (param1_0) =>
            {
                var disposeActions3_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
                global::System.Func<global::System.Int32, global::A> _0 = (param2_0) =>
                {
                    var _0 = new global::A(param2_0, param1_0, param0_0);
                    disposeActions3_0.Add(() =>
                    {
                    }

                    );
                    return _0;
                }

                ;
                disposeActions2_0.Add(() =>
                {
                    foreach (var disposeAction in disposeActions3_0)
                        disposeAction();
                }

                );
                return _0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>> global::StrongInject.IContainer<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>> _0 = (param1_0) =>
            {
                var disposeActions3_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
                global::System.Func<global::System.Int32, global::A> _0 = (param2_0) =>
                {
                    var _0 = new global::A(param2_0, param1_0, param0_0);
                    disposeActions3_0.Add(() =>
                    {
                    }

                    );
                    return _0;
                }

                ;
                disposeActions2_0.Add(() =>
                {
                    foreach (var disposeAction in disposeActions3_0)
                        disposeAction();
                }

                );
                return _0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void DisposesOfFuncDependenciesButNotParameters()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[Registration(typeof(C))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b, C c){}
}
public class B : IDisposable
{
    public void Dispose(){}
}
public class C : IDisposable
{
    public void Dispose(){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::B, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _1 = new global::C();
            var _0 = new global::A(param0_0, (global::C)_1);
            disposeActions1_0.Add(() =>
            {
                ((global::System.IDisposable)_1).Dispose();
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::B, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _1 = new global::C();
            var _0 = new global::A(param0_0, (global::C)_1);
            disposeActions1_0.Add(() =>
            {
                ((global::System.IDisposable)_1).Dispose();
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void WarningOnUnusedParameters1()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<Func<int, string, A>>
{
}

public class A 
{
    public A(string s1, string s2){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1101: Warning while resolving dependencies for 'System.Func<int, string, A>': Parameter 'int' of delegate 'System.Func<int, string, A>' is not used in resolution of 'A'.
                // Container
                new DiagnosticResult("SI1101", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.String, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::System.Int32, global::System.String, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.String, global::A> _0 = (param0_0, param0_1) =>
        {
            var _0 = new global::A(param0_1, param0_1);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::System.Int32, global::System.String, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.String, global::A>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.String, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.String, global::A> _0 = (param0_0, param0_1) =>
        {
            var _0 = new global::A(param0_1, param0_1);
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.String, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void WarningOnUnusedParameters2()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<Func<int, Func<int, A>>>
{
}

public class A 
{
    public A(int a1, int a2){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1101: Warning while resolving dependencies for 'System.Func<int, System.Func<int, A>>': Parameter 'int' of delegate 'System.Func<int, System.Func<int, A>>' is not used in resolution of 'System.Func<int, A>'.
                // Container
                new DiagnosticResult("SI1101", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.Int32, global::A> _0 = (param1_0) =>
            {
                var _0 = new global::A(param1_0, param1_0);
                disposeActions2_0.Add(() =>
                {
                }

                );
                return _0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::System.Int32, global::A> _0 = (param1_0) =>
            {
                var _0 = new global::A(param1_0, param1_0);
                disposeActions2_0.Add(() =>
                {
                }

                );
                return _0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void WarningOnSingleInstanceReturnType()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A), Scope.SingleInstance)]
[Registration(typeof(B))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b){}
}

public class B
{
    public B(){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,22): Warning SI1103: Warning while resolving dependencies for 'System.Func<B, A>': Return type 'A' of delegate 'System.Func<B, A>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(7, 22),
                // (7,22): Warning SI1101: Warning while resolving dependencies for 'System.Func<B, A>': Parameter 'B' of delegate 'System.Func<B, A>' is not used in resolution of 'A'.
                // Container
                new DiagnosticResult("SI1101", @"Container", DiagnosticSeverity.Warning).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::A _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::A GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::B();
            var _0 = new global::A((global::B)_1);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::B, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = GetSingleInstanceField0();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::B, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::B, global::A> _0 = (param0_0) =>
        {
            var _0 = GetSingleInstanceField0();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void WarningOnDirectDelegateParameterReturnType()
        {
            string userSource = @"
using System;
using StrongInject;

public partial class Container : IContainer<Func<A, A>>
{
}

public class A 
{
    public A(){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (5,22): Warning SI1104: Warning while resolving dependencies for 'System.Func<A, A>': Return type 'A' of delegate 'System.Func<A, A>' is provided as a parameter to the delegate and so will be returned unchanged.
                // Container
                new DiagnosticResult("SI1104", @"Container", DiagnosticSeverity.Warning).WithLocation(5, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::A, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::A, global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::A, global::A> _0 = (param0_0) =>
        {
            disposeActions1_0.Add(() =>
            {
            }

            );
            return param0_0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::A, global::A>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::A, global::A>> global::StrongInject.IContainer<global::System.Func<global::A, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::A, global::A> _0 = (param0_0) =>
        {
            disposeActions1_0.Add(() =>
            {
            }

            );
            return param0_0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::A, global::A>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void WarningOnIndirectDelegateParameterReturnType()
        {
            string userSource = @"
using System;
using StrongInject;

public partial class Container : IContainer<Func<A, Func<A>>>
{
}

public class A 
{
    public A(){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (5,22): Warning SI1102: Warning while resolving dependencies for 'System.Func<A, System.Func<A>>': Return type 'A' of delegate 'System.Func<A>' is provided as a parameter to another delegate and so will always have the same value.
                // Container
                new DiagnosticResult("SI1102", @"Container", DiagnosticSeverity.Warning).WithLocation(5, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::A, global::System.Func<global::A>>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::A, global::System.Func<global::A>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::A, global::System.Func<global::A>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::A> _0 = () =>
            {
                disposeActions2_0.Add(() =>
                {
                }

                );
                return param0_0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::System.Func<global::A, global::System.Func<global::A>>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::A, global::System.Func<global::A>>> global::StrongInject.IContainer<global::System.Func<global::A, global::System.Func<global::A>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::System.Func<global::A, global::System.Func<global::A>> _0 = (param0_0) =>
        {
            var disposeActions2_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::System.Func<global::A> _0 = () =>
            {
                disposeActions2_0.Add(() =>
                {
                }

                );
                return param0_0;
            }

            ;
            disposeActions1_0.Add(() =>
            {
                foreach (var disposeAction in disposeActions2_0)
                    disposeAction();
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::System.Func<global::A, global::System.Func<global::A>>>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorOnMultipleParametersWithSameType()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<Func<int, int, A>>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0104: Error while resolving dependencies for 'System.Func<int, int, A>': delegate 'System.Func<int, int, A>' has multiple parameters of type 'int'.
                // Container
                new DiagnosticResult("SI0104", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22),
                // (6,22): Warning SI1101: Warning while resolving dependencies for 'System.Func<int, int, A>': Parameter 'int' of delegate 'System.Func<int, int, A>' is not used in resolution of 'A'.
                // Container
                new DiagnosticResult("SI1101", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.Int32, global::A>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::System.Int32, global::System.Int32, global::A>, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.Int32, global::A>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.Int32, global::A>>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorOnRecursiveFuncCall()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A 
{
    public A(Func<A> a){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0101: Error while resolving dependencies for 'A': 'A' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfSyncFuncRequiresAsyncResolution()
        {
            string userSource = @"
using System;
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A))]
public partial class Container : IAsyncContainer<Func<A>>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'System.Func<A>': 'A' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::A>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorOnParameterPassedAsRef()
        {
            string userSource = @"
using StrongInject;

public delegate A Del(ref int i);
[Registration(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0105: Error while resolving dependencies for 'Del': parameter 'ref int' of delegate 'Del' is passed as 'Ref'.
                // Container
                new DiagnosticResult("SI0105", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::Del>.Run<TResult, TParam>(global::System.Func<global::Del, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorOnParameterPassedAsIn()
        {
            string userSource = @"
using StrongInject;

public delegate A Del(in int i);
[Registration(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0105: Error while resolving dependencies for 'Del': parameter 'in int' of delegate 'Del' is passed as 'In'.
                // Container
                new DiagnosticResult("SI0105", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::Del>.Run<TResult, TParam>(global::System.Func<global::Del, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorOnParameterPassedAsOut()
        {
            string userSource = @"
using StrongInject;

public delegate A Del(out int i);
[Registration(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Error SI0105: Error while resolving dependencies for 'Del': parameter 'out int' of delegate 'Del' is passed as 'Out'.
                // Container
                new DiagnosticResult("SI0105", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::Del>.Run<TResult, TParam>(global::System.Func<global::Del, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void DelegateReturningTaskCanResolveDependenciesAsynchronously()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public delegate Task<A> Del(int i);
[Registration(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(int a){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::Del>.Run<TResult, TParam>(global::System.Func<global::Del, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::Del _0 = async (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::Del)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::Del _0 = async (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::Del>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void DelegateReturningValueTaskCanResolveDependenciesAsynchronously()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public delegate ValueTask<A> Del(int i);
[Registration(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(int a){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::Del>.Run<TResult, TParam>(global::System.Func<global::Del, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::Del _0 = async (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        TResult result;
        try
        {
            result = func((global::Del)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        global::Del _0 = async (param0_0) =>
        {
            var _0 = new global::A(param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
            disposeActions1_0.Add(() =>
            {
            }

            );
            return _0;
        }

        ;
        return new global::StrongInject.Owned<global::Del>(_0, () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void SingleInstanceDependencyCanDependOnDelegate()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public delegate ValueTask<A> Del(int i);
[Registration(typeof(A))]
[Registration(typeof(B), Scope.SingleInstance)]
public partial class Container : IContainer<B>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(int a){}
    public ValueTask InitializeAsync() => default;
}
public class B
{
    public B(Del d){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        this._lock0.Wait();
        try
        {
            this._disposeAction0?.Invoke();
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::B _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::B GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var disposeActions1_1 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            global::Del _1 = async (param0_0) =>
            {
                var _0 = new global::A(param0_0);
                await ((global::StrongInject.IRequiresAsyncInitialization)_0).InitializeAsync();
                disposeActions1_1.Add(() =>
                {
                }

                );
                return _0;
            }

            ;
            var _0 = new global::B(_1);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = () =>
            {
                foreach (var disposeAction in disposeActions1_1)
                    disposeAction();
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func((global::B)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::B>(_0, () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void PreferInstanceProviderToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IAsyncContainer<Func<A>>
{
    private IInstanceProvider<Func<A>> _instanceProvider;
}

public class A
{
    public A(){}
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (8,40): Warning CS0649: Field 'Container._instanceProvider' is never assigned to, and will always have its default value null
                // _instanceProvider
                new DiagnosticResult("CS0649", @"_instanceProvider", DiagnosticSeverity.Warning).WithLocation(8, 40));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::A>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = ((global::StrongInject.IInstanceProvider<global::System.Func<global::A>>)this._instanceProvider).Get();
        TResult result;
        try
        {
            result = await func((global::System.Func<global::A>)_0, param);
        }
        finally
        {
            ((global::StrongInject.IInstanceProvider<global::System.Func<global::A>>)this._instanceProvider).Release(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = ((global::StrongInject.IInstanceProvider<global::System.Func<global::A>>)this._instanceProvider).Get();
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0, async () =>
        {
            ((global::StrongInject.IInstanceProvider<global::System.Func<global::A>>)this._instanceProvider).Release(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void PreferFactoryToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
[FactoryRegistration(typeof(B))]
public partial class Container : IAsyncContainer<Func<A>>
{
}

public class A
{
}
public class B : IFactory<Func<A>>
{
    public Func<A> Create() => null;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::A>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = ((global::StrongInject.IFactory<global::System.Func<global::A>>)_1).Create();
        TResult result;
        try
        {
            result = await func((global::System.Func<global::A>)_0, param);
        }
        finally
        {
            ((global::StrongInject.IFactory<global::System.Func<global::A>>)_1).Release(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = ((global::StrongInject.IFactory<global::System.Func<global::A>>)_1).Create();
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0, async () =>
        {
            ((global::StrongInject.IFactory<global::System.Func<global::A>>)_1).Release(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void PreferDelegateParameterToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Registration(typeof(A))]
public partial class Container : IAsyncContainer<Func<Func<A>, Func<A>>>
{
}

public class A
{
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1104: Warning while resolving dependencies for 'System.Func<System.Func<A>, System.Func<A>>': Return type 'System.Func<A>' of delegate 'System.Func<System.Func<A>, System.Func<A>>' is provided as a parameter to the delegate and so will be returned unchanged.
                // Container
                new DiagnosticResult("SI1104", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
        global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>> _0 = (param0_0) =>
        {
            disposeActions1_0.Add(async () =>
            {
            }

            );
            return param0_0;
        }

        ;
        TResult result;
        try
        {
            result = await func((global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>)_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions1_0)
                await disposeAction();
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>> global::StrongInject.IAsyncContainer<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var disposeActions1_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
        global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>> _0 = (param0_0) =>
        {
            disposeActions1_0.Add(async () =>
            {
            }

            );
            return param0_0;
        }

        ;
        return new global::StrongInject.AsyncOwned<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>(_0, async () =>
        {
            foreach (var disposeAction in disposeActions1_0)
                await disposeAction();
        }

        );
    }
}");
        }

        [Fact]
        public void CannotResolveVoidReturningDelegate()
        {
            string userSource = @"
using StrongInject;
using System;

public partial class Container : IContainer<Action<int>>
{
}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (5,22): Error SI0102: Error while resolving dependencies for 'Action<int>': We have no source for instance of type 'Action<int>'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Action<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::System.Action<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Action<global::System.Int32>> global::StrongInject.IContainer<global::System.Action<global::System.Int32>>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void CanImportFactoryMethodFromModule()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public static A M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = global::Module.M((global::B)_1);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = global::Module.M((global::B)_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void FactoryMethodCanBeSingleInstance()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory(Scope.SingleInstance)]
    public static A M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
        await this._lock0.WaitAsync();
        try
        {
            await (this._disposeAction0?.Invoke() ?? default);
        }
        finally
        {
            this._lock0.Release();
        }
    }

    private global::A _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::A GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            var _1 = new global::B();
            var _0 = global::Module.M((global::B)_1);
            this._singleInstanceField0 = _0;
            this._disposeAction0 = async () =>
            {
                await global::StrongInject.Helpers.DisposeAsync(_0);
            }

            ;
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void NonPublicFactoryMethodIgnored()
        {
            string userSource = @"
using StrongInject;

class Module
{
    [Factory]
    public static A M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1003: Factory method 'Module.M(B)' is not publicly accessible, and containing module 'Module' is not a container, so it will be ignored.
                // Factory
                new DiagnosticResult("SI1003", @"Factory", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (12,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(12, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void NonStaticFactoryMethodIgnored()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public A M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1002: Factory method 'Module.M(B)' is not static, and containing module 'Module' is not a container, so will be ignored.
                // Factory
                new DiagnosticResult("SI1002", @"Factory", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (12,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(12, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void CanUseNonPublicStaticFactoryMethodDefinedInContainer()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = this.M((global::B)_1);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = this.M((global::B)_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void FactoryMethodDefinedInContainerOverridesExistingRegistration()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = this.M((global::B)_1);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = this.M((global::B)_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfPublicStaticFactoryMethodDefinedInContainerOverridesExistingRegistration()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    public static A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (8,6): Error SI0004: Module already contains registration for 'A'.
                // Factory
                new DiagnosticResult("SI0004", @"Factory", DiagnosticSeverity.Error).WithLocation(8, 6));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfMultipleFactoryMethodsDefinedByContainerForSameType()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
    [Factory]
    A M1() => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (8,6): Error SI0016: Both methods 'Container.M1()' and 'Container.M(B)' are factories for 'A'
                // Factory
                new DiagnosticResult("SI0016", @"Factory", DiagnosticSeverity.Error).WithLocation(8, 6),
                // (10,6): Error SI0016: Both methods 'Container.M1()' and 'Container.M(B)' are factories for 'A'
                // Factory
                new DiagnosticResult("SI0016", @"Factory", DiagnosticSeverity.Error).WithLocation(10, 6));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = this.M1();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = this.M1();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfInstanceProviderAndFactoryMethodDefinedByContainerForSameType()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
    public IInstanceProvider<A> _instanceProvider;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (8,6): Error SI0017: Both factory method 'Container.M(B)' and instance provider field 'Container._instanceProvider' are sources for 'A'
                // Factory
                new DiagnosticResult("SI0017", @"Factory", DiagnosticSeverity.Error).WithLocation(8, 6),
                // (10,33): Error SI0017: Both factory method 'Container.M(B)' and instance provider field 'Container._instanceProvider' are sources for 'A'
                // _instanceProvider
                new DiagnosticResult("SI0017", @"_instanceProvider", DiagnosticSeverity.Error).WithLocation(10, 33));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = ((global::StrongInject.IInstanceProvider<global::A>)this._instanceProvider).Get();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            ((global::StrongInject.IInstanceProvider<global::A>)this._instanceProvider).Release(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = ((global::StrongInject.IInstanceProvider<global::A>)this._instanceProvider).Get();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            ((global::StrongInject.IInstanceProvider<global::A>)this._instanceProvider).Release(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfFactoryMethodReturnsVoid()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    void M(B b){}
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (8,6): Error SI0014: Factory method 'Container.M(B)' returns void.
                // Factory
                new DiagnosticResult("SI0014", @"Factory", DiagnosticSeverity.Error).WithLocation(8, 6));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfPublicStaticFactoryMethodInContainerReturnsVoid()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B))]
[Registration(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    public static void M(B b){}
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (8,6): Error SI0014: Factory method 'Container.M(B)' returns void.
                // Factory
                new DiagnosticResult("SI0014", @"Factory", DiagnosticSeverity.Error).WithLocation(8, 6));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfFactoryMethodFromModuleOverridesExisingRegistration()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
public class Module
{
    [Factory]
    public static A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,6): Error SI0004: Module already contains registration for 'A'.
                // Factory
                new DiagnosticResult("SI0004", @"Factory", DiagnosticSeverity.Error).WithLocation(7, 6));
            comp.GetDiagnostics().Verify();
            Assert.Empty(generated);
        }

        [Fact]
        public void ErrorIfFactoryMethodTakesParameterByRef()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public static A M(ref B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,23): Error SI0018: parameter 'ref B' of factory method 'Module.M(ref B)' is passed as 'Ref'.
                // ref B b
                new DiagnosticResult("SI0018", @"ref B b", DiagnosticSeverity.Error).WithLocation(7, 23));
            comp.GetDiagnostics().Verify();
            Assert.Empty(generated);
        }

        [Fact]
        public void CanResolveAsyncFactoryMethod()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public class Module
{
    [Factory]
    public static Task<A> M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = await global::Module.M((global::B)_1);
        TResult result;
        try
        {
            result = await func((global::A)_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _1 = new global::B();
        var _0 = await global::Module.M((global::B)_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0);
        }

        );
    }
}");
        }

        [Fact]
        public void ErrorIfAsyncFactoryMethodUsedInSyncContainer()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public class Module
{
    [Factory]
    public static Task<A> M(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (13,22): Error SI0103: Error while resolving dependencies for 'A': 'A' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(13, 22));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfFactoryMethodIsGeneric()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public static A M<T>(B b) => null;
}

[Registration(typeof(B))]
[ModuleRegistration(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (6,6): Error SI0014: Factory method 'Module.M<T>(B)' is generic.
                // Factory
                new DiagnosticResult("SI0014", @"Factory", DiagnosticSeverity.Error).WithLocation(6, 6),
                // (12,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(12, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public async global::System.Threading.Tasks.ValueTask DisposeAsync()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::A>.RunAsync<TResult, TParam>(global::System.Func<global::A, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void WarnOnInstanceRequiringAsyncDisposalInSyncResolution()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

[Registration(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}
";
            var comp = RunGenerator(userSource, out var generatorDiagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            generatorDiagnostics.Verify(
                // (7,22): Warning SI1301: Cannot call asynchronous dispose for 'A' in implementation of synchronous container
                // Container
                new DiagnosticResult("SI1301", @"Container", DiagnosticSeverity.Warning).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        TResult result;
        try
        {
            result = func((global::A)_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        var _0 = new global::A();
        return new global::StrongInject.Owned<global::A>(_0, () =>
        {
        }

        );
    }
}");
        }
    }
}
