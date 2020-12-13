using FluentAssertions;
using Microsoft.CodeAnalysis;
using System.Linq;
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

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::A _0_0;
        _0_2 = new global::C();
        _0_3 = new global::D(c: _0_2);
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::A _0_0;
        _0_2 = new global::C();
        _0_3 = new global::D(c: _0_2);
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerResolutionDependenciesWithCasts()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C), typeof(C), typeof(IC))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_3;
        global::IC _0_2;
        global::D _0_4;
        global::B _0_1;
        global::A _0_0;
        _0_3 = new global::C();
        _0_2 = (global::IC)_0_3;
        _0_4 = new global::D(c: _0_3);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_3;
        global::IC _0_2;
        global::D _0_4;
        global::B _0_1;
        global::A _0_0;
        _0_3 = new global::C();
        _0_2 = (global::IC)_0_3;
        _0_4 = new global::D(c: _0_3);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerResolutionDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::A _0_0;
        _0_2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_2).InitializeAsync();
        _0_3 = new global::D(c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_3).InitializeAsync();
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::A _0_0;
        _0_2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_2).InitializeAsync();
        _0_3 = new global::D(c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_3).InitializeAsync();
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerResolutionDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A))]
[RegisterFactory(typeof(B))]
[RegisterFactory(typeof(C))]
[RegisterFactory(typeof(D))]
[Register(typeof(C))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Register(typeof(C))
                new DiagnosticResult("SI1001", @"Register(typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
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
        global::C _0_6;
        global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_11;
        global::CFactoryTarget _0_10;
        global::D _0_9;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_8;
        global::DFactoryTarget _0_7;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_4;
        global::BFactoryTarget _0_3;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_6 = new global::C();
        _0_11 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_6;
        _0_10 = await _0_11.CreateAsync();
        try
        {
            _0_9 = new global::D(c: _0_10);
            _0_8 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_9;
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_5 = new global::B(c: _0_6, d: _0_7);
                _0_4 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_5;
                _0_3 = await _0_4.CreateAsync();
                try
                {
                    _0_2 = new global::A(b: _0_3, c: _0_10);
                    _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                    _0_0 = await _0_1.CreateAsync();
                }
                catch
                {
                    await _0_4.ReleaseAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            await _0_11.ReleaseAsync(_0_10);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_4.ReleaseAsync(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            await _0_11.ReleaseAsync(_0_10);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_6;
        global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_11;
        global::CFactoryTarget _0_10;
        global::D _0_9;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_8;
        global::DFactoryTarget _0_7;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_4;
        global::BFactoryTarget _0_3;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_6 = new global::C();
        _0_11 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_6;
        _0_10 = await _0_11.CreateAsync();
        try
        {
            _0_9 = new global::D(c: _0_10);
            _0_8 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_9;
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_5 = new global::B(c: _0_6, d: _0_7);
                _0_4 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_5;
                _0_3 = await _0_4.CreateAsync();
                try
                {
                    _0_2 = new global::A(b: _0_3, c: _0_10);
                    _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                    _0_0 = await _0_1.CreateAsync();
                }
                catch
                {
                    await _0_4.ReleaseAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            await _0_11.ReleaseAsync(_0_10);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_4.ReleaseAsync(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            await _0_11.ReleaseAsync(_0_10);
        });
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), Scope.InstancePerDependency)]
[Register(typeof(B))]
[Register(typeof(C), Scope.InstancePerDependency)]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_2;
        global::C _0_4;
        global::D _0_3;
        global::B _0_1;
        global::C _0_5;
        global::A _0_0;
        _0_2 = new global::C();
        _0_4 = new global::C();
        _0_3 = new global::D(c: _0_4);
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_5 = new global::C();
        _0_0 = new global::A(b: _0_1, c: _0_5);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_2;
        global::C _0_4;
        global::D _0_3;
        global::B _0_1;
        global::C _0_5;
        global::A _0_0;
        _0_2 = new global::C();
        _0_4 = new global::C();
        _0_3 = new global::D(c: _0_4);
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_5 = new global::C();
        _0_0 = new global::A(b: _0_1, c: _0_5);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithCasts()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C), Scope.InstancePerDependency, typeof(C), typeof(IC))]
[Register(typeof(D), Scope.InstancePerDependency)]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_3;
        global::IC _0_2;
        global::C _0_5;
        global::D _0_4;
        global::B _0_1;
        global::C _0_7;
        global::IC _0_6;
        global::A _0_0;
        _0_3 = new global::C();
        _0_2 = (global::IC)_0_3;
        _0_5 = new global::C();
        _0_4 = new global::D(c: _0_5);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_7 = new global::C();
        _0_6 = (global::IC)_0_7;
        _0_0 = new global::A(b: _0_1, c: _0_6);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_3;
        global::IC _0_2;
        global::C _0_5;
        global::D _0_4;
        global::B _0_1;
        global::C _0_7;
        global::IC _0_6;
        global::A _0_0;
        _0_3 = new global::C();
        _0_2 = (global::IC)_0_3;
        _0_5 = new global::C();
        _0_4 = new global::D(c: _0_5);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_7 = new global::C();
        _0_6 = (global::IC)_0_7;
        _0_0 = new global::A(b: _0_1, c: _0_6);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A), Scope.InstancePerDependency)]
[Register(typeof(B), Scope.InstancePerDependency)]
[Register(typeof(C))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::B _0_4;
        global::A _0_0;
        _0_2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_2).InitializeAsync();
        _0_3 = new global::D(c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_3).InitializeAsync();
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_4 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2, b1: _0_4);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_2;
        global::D _0_3;
        global::B _0_1;
        global::B _0_4;
        global::A _0_0;
        _0_2 = new global::C();
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_2).InitializeAsync();
        _0_3 = new global::D(c: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_3).InitializeAsync();
        _0_1 = new global::B(c: _0_2, d: _0_3);
        _0_4 = new global::B(c: _0_2, d: _0_3);
        _0_0 = new global::A(b: _0_1, c: _0_2, b1: _0_4);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A))]
[RegisterFactory(typeof(B), Scope.InstancePerDependency)]
[RegisterFactory(typeof(C), Scope.InstancePerResolution, Scope.InstancePerDependency)]
[RegisterFactory(typeof(D), Scope.InstancePerDependency, Scope.InstancePerDependency)]
[Register(typeof(C))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Register(typeof(C))
                new DiagnosticResult("SI1001", @"Register(typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
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
        global::C _0_6;
        global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_11;
        global::CFactoryTarget _0_10;
        global::D _0_9;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_8;
        global::DFactoryTarget _0_7;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_4;
        global::BFactoryTarget _0_3;
        global::CFactoryTarget _0_12;
        global::CFactoryTarget _0_16;
        global::D _0_15;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_14;
        global::DFactoryTarget _0_13;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_6 = new global::C();
        _0_11 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_6;
        _0_10 = await _0_11.CreateAsync();
        try
        {
            _0_9 = new global::D(c: _0_10);
            _0_8 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_9;
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_5 = new global::B(c: _0_6, d: _0_7);
                _0_4 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_5;
                _0_3 = await _0_4.CreateAsync();
                try
                {
                    _0_12 = await _0_11.CreateAsync();
                    try
                    {
                        _0_16 = await _0_11.CreateAsync();
                        try
                        {
                            _0_15 = new global::D(c: _0_16);
                            _0_14 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_15;
                            _0_13 = await _0_14.CreateAsync();
                            try
                            {
                                _0_2 = new global::A(b: _0_3, c: _0_12, d: _0_13);
                                _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                                _0_0 = await _0_1.CreateAsync();
                            }
                            catch
                            {
                                await _0_14.ReleaseAsync(_0_13);
                                throw;
                            }
                        }
                        catch
                        {
                            await _0_11.ReleaseAsync(_0_16);
                            throw;
                        }
                    }
                    catch
                    {
                        await _0_11.ReleaseAsync(_0_12);
                        throw;
                    }
                }
                catch
                {
                    await _0_4.ReleaseAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            await _0_11.ReleaseAsync(_0_10);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_14.ReleaseAsync(_0_13);
            await _0_11.ReleaseAsync(_0_16);
            await _0_11.ReleaseAsync(_0_12);
            await _0_4.ReleaseAsync(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            await _0_11.ReleaseAsync(_0_10);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_6;
        global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_11;
        global::CFactoryTarget _0_10;
        global::D _0_9;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_8;
        global::DFactoryTarget _0_7;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_4;
        global::BFactoryTarget _0_3;
        global::CFactoryTarget _0_12;
        global::CFactoryTarget _0_16;
        global::D _0_15;
        global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_14;
        global::DFactoryTarget _0_13;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_6 = new global::C();
        _0_11 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_6;
        _0_10 = await _0_11.CreateAsync();
        try
        {
            _0_9 = new global::D(c: _0_10);
            _0_8 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_9;
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_5 = new global::B(c: _0_6, d: _0_7);
                _0_4 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_5;
                _0_3 = await _0_4.CreateAsync();
                try
                {
                    _0_12 = await _0_11.CreateAsync();
                    try
                    {
                        _0_16 = await _0_11.CreateAsync();
                        try
                        {
                            _0_15 = new global::D(c: _0_16);
                            _0_14 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_15;
                            _0_13 = await _0_14.CreateAsync();
                            try
                            {
                                _0_2 = new global::A(b: _0_3, c: _0_12, d: _0_13);
                                _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                                _0_0 = await _0_1.CreateAsync();
                            }
                            catch
                            {
                                await _0_14.ReleaseAsync(_0_13);
                                throw;
                            }
                        }
                        catch
                        {
                            await _0_11.ReleaseAsync(_0_16);
                            throw;
                        }
                    }
                    catch
                    {
                        await _0_11.ReleaseAsync(_0_12);
                        throw;
                    }
                }
                catch
                {
                    await _0_4.ReleaseAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            await _0_11.ReleaseAsync(_0_10);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_14.ReleaseAsync(_0_13);
            await _0_11.ReleaseAsync(_0_16);
            await _0_11.ReleaseAsync(_0_12);
            await _0_4.ReleaseAsync(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            await _0_11.ReleaseAsync(_0_10);
        });
    }
}");
        }

        [Fact]
        public void SingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D), Scope.SingleInstance)]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_1;
            global::D _0_0;
            _0_1 = new global::C();
            _0_0 = new global::D(c: _0_1);
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
            };
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
            global::C _0_2;
            global::D _0_3;
            global::B _0_1;
            global::A _0_0;
            _0_2 = new global::C();
            _0_3 = GetSingleInstanceField1();
            _0_1 = new global::B(c: _0_2, d: _0_3);
            _0_0 = new global::A(b: _0_1, c: _0_2);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWihCasts()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C), Scope.SingleInstance, typeof(C), typeof(IC))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_0;
            _0_0 = new global::C();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::C _0_3;
        global::IC _0_2;
        global::D _0_4;
        global::B _0_1;
        global::A _0_0;
        _0_3 = GetSingleInstanceField0();
        _0_2 = (global::IC)_0_3;
        _0_4 = new global::D(c: _0_3);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_3;
        global::IC _0_2;
        global::D _0_4;
        global::B _0_1;
        global::A _0_0;
        _0_3 = GetSingleInstanceField0();
        _0_2 = (global::IC)_0_3;
        _0_4 = new global::D(c: _0_3);
        _0_1 = new global::B(c: _0_2, d: _0_4);
        _0_0 = new global::A(b: _0_1, c: _0_2);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B))]
[Register(typeof(C), Scope.SingleInstance)]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_0;
            _0_0 = new global::C();
            await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
            };
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
            global::C _0_2;
            global::D _0_3;
            global::B _0_1;
            global::A _0_0;
            _0_2 = await GetSingleInstanceField1();
            _0_3 = new global::D(c: _0_2);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0_3).InitializeAsync();
            _0_1 = new global::B(c: _0_2, d: _0_3);
            _0_0 = new global::A(b: _0_1, c: _0_2);
            await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::A _0_0;
        _0_0 = await GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = await GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A), Scope.SingleInstance, Scope.InstancePerResolution)]
[RegisterFactory(typeof(B), Scope.SingleInstance, Scope.SingleInstance)]
[RegisterFactory(typeof(C), Scope.InstancePerResolution, Scope.SingleInstance)]
[RegisterFactory(typeof(D), Scope.InstancePerResolution, Scope.InstancePerResolution)]
[Register(typeof(C), Scope.InstancePerResolution, typeof(C))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (9,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Register(typeof(C), Scope.InstancePerResolution, typeof(C))
                new DiagnosticResult("SI1001", @"Register(typeof(C), Scope.InstancePerResolution, typeof(C))", DiagnosticSeverity.Warning).WithLocation(9, 2));
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
            global::C _0_2;
            global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_1;
            global::CFactoryTarget _0_0;
            _0_2 = new global::C();
            _0_1 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_2;
            _0_0 = await _0_1.CreateAsync();
            this._singleInstanceField3 = _0_0;
            this._disposeAction3 = async () =>
            {
                await _0_1.ReleaseAsync(_0_0);
            };
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
            global::C _0_1;
            global::CFactoryTarget _0_5;
            global::D _0_4;
            global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_3;
            global::DFactoryTarget _0_2;
            global::B _0_0;
            _0_1 = new global::C();
            _0_5 = await GetSingleInstanceField3();
            _0_4 = new global::D(c: _0_5);
            _0_3 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_0 = new global::B(c: _0_1, d: _0_2);
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }

            this._singleInstanceField2 = _0_0;
            this._disposeAction2 = async () =>
            {
                await _0_3.ReleaseAsync(_0_2);
            };
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
            global::B _0_2;
            global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_1;
            global::BFactoryTarget _0_0;
            _0_2 = await GetSingleInstanceField2();
            _0_1 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_2;
            _0_0 = await _0_1.CreateAsync();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
                await _0_1.ReleaseAsync(_0_0);
            };
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
            global::BFactoryTarget _0_1;
            global::CFactoryTarget _0_2;
            global::A _0_0;
            _0_1 = await GetSingleInstanceField1();
            _0_2 = await GetSingleInstanceField3();
            _0_0 = new global::A(b: _0_1, c: _0_2);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_2 = await GetSingleInstanceField0();
        _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
        _0_0 = await _0_1.CreateAsync();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_2 = await GetSingleInstanceField0();
        _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
        _0_0 = await _0_1.CreateAsync();
        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
        });
    }
}");
        }

        [Fact]
        public void MultipleResolvesShareSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C), Scope.SingleInstance, typeof(C), typeof(IC))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_0;
            _0_0 = new global::C();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::C _0_2;
        global::IC _0_1;
        global::A _0_0;
        _0_2 = GetSingleInstanceField0();
        _0_1 = (global::IC)_0_2;
        _0_0 = new global::A(c: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_2;
        global::IC _0_1;
        global::A _0_0;
        _0_2 = GetSingleInstanceField0();
        _0_1 = (global::IC)_0_2;
        _0_0 = new global::A(c: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::B>.RunAsync<TResult, TParam>(global::System.Func<global::B, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_1;
        global::D _0_2;
        global::B _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = new global::D(c: _0_1);
        _0_0 = new global::B(c: _0_1, d: _0_2);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_1;
        global::D _0_2;
        global::B _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = new global::D(c: _0_1);
        _0_0 = new global::B(c: _0_1, d: _0_2);
        return new global::StrongInject.AsyncOwned<global::B>(_0_0, async () =>
        {
        });
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
                // (1,1): Error SI0201: Missing Type 'StrongInject.RegisterAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.RegisterModuleAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.RegisterFactoryAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.RegisterDecoratorAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.FactoryAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.DecoratorFactoryAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.InstanceAttribute'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1),
                // (1,1): Error SI0201: Missing Type 'StrongInject.Helpers'. Are you missing an assembly reference?
                // Missing Type.SI0201
                new DiagnosticResult("SI0201", @"<UNKNOWN>", DiagnosticSeverity.Error).WithLocation(1, 1));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorIfInstanceUsedAsFactoryDuplicatesContainerRegistration()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IAsyncContainer<A>
{
    [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] public InstanceFactory _instanceFactory;
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

public class InstanceFactory : IAsyncFactory<IC>, IAsyncFactory<D>
{
    public ValueTask<IC> CreateAsync() => throw null;
    ValueTask<D> IAsyncFactory<D>.CreateAsync() => throw null;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (9,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'D' and no best source. Try adding a single registration for 'D' directly to the container, and moving any existing registrations for 'D' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(9, 22));
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
        public void CorrectDisposal()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;
using System;

[RegisterFactory(typeof(A))]
[RegisterFactory(typeof(B), Scope.SingleInstance, Scope.SingleInstance)]
[RegisterFactory(typeof(C), Scope.InstancePerResolution, Scope.SingleInstance)]
[RegisterFactory(typeof(D), Scope.InstancePerResolution, Scope.InstancePerResolution)]
[Register(typeof(C))]
[Register(typeof(E))]
[Register(typeof(F))]
[Register(typeof(G))]
[Register(typeof(H))]
[Register(typeof(I), Scope.SingleInstance)]
public partial class Container : IAsyncContainer<AFactoryTarget>
{
    [Instance(Options.UseAsFactory)] IAsyncFactory<int> _factory;
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (10,2): Warning SI1001: 'C' implements 'StrongInject.IAsyncFactory<CFactoryTarget>'. Did you mean to use FactoryRegistration instead?
                // Register(typeof(C))
                new DiagnosticResult("SI1001", @"Register(typeof(C))", DiagnosticSeverity.Warning).WithLocation(10, 2));
            comp.GetDiagnostics().Verify(
                // (18,57): Warning CS0649: Field 'Container._factory' is never assigned to, and will always have its default value null
                // _factory
                new DiagnosticResult("CS0649", @"_factory", DiagnosticSeverity.Warning).WithLocation(18, 57));
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
            global::C _0_2;
            global::StrongInject.IAsyncFactory<global::CFactoryTarget> _0_1;
            global::CFactoryTarget _0_0;
            _0_2 = new global::C();
            _0_1 = (global::StrongInject.IAsyncFactory<global::CFactoryTarget>)_0_2;
            _0_0 = await _0_1.CreateAsync();
            this._singleInstanceField2 = _0_0;
            this._disposeAction2 = async () =>
            {
                await _0_1.ReleaseAsync(_0_0);
            };
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
            global::C _0_1;
            global::CFactoryTarget _0_5;
            global::D _0_4;
            global::StrongInject.IAsyncFactory<global::DFactoryTarget> _0_3;
            global::DFactoryTarget _0_2;
            global::B _0_0;
            _0_1 = new global::C();
            _0_5 = await GetSingleInstanceField2();
            _0_4 = new global::D(c: _0_5);
            _0_3 = (global::StrongInject.IAsyncFactory<global::DFactoryTarget>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_0 = new global::B(c: _0_1, d: _0_2);
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }

            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
                await _0_3.ReleaseAsync(_0_2);
            };
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
            global::B _0_2;
            global::StrongInject.IAsyncFactory<global::BFactoryTarget> _0_1;
            global::BFactoryTarget _0_0;
            _0_2 = await GetSingleInstanceField1();
            _0_1 = (global::StrongInject.IAsyncFactory<global::BFactoryTarget>)_0_2;
            _0_0 = await _0_1.CreateAsync();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
                await _0_1.ReleaseAsync(_0_0);
            };
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
            global::StrongInject.IAsyncFactory<global::System.Int32> _0_2;
            global::System.Int32 _0_1;
            global::I _0_0;
            _0_2 = this._factory;
            _0_1 = await _0_2.CreateAsync();
            try
            {
                _0_0 = new global::I(i: _0_1);
            }
            catch
            {
                await _0_2.ReleaseAsync(_0_1);
                throw;
            }

            this._singleInstanceField3 = _0_0;
            this._disposeAction3 = async () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
                await _0_2.ReleaseAsync(_0_1);
            };
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
        global::BFactoryTarget _0_3;
        global::CFactoryTarget _0_4;
        global::I _0_9;
        global::H _0_8;
        global::G _0_7;
        global::F _0_6;
        global::E _0_5;
        global::StrongInject.IAsyncFactory<global::System.Int32> _0_11;
        global::System.Int32 _0_10;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_4 = await GetSingleInstanceField2();
        _0_9 = await GetSingleInstanceField3();
        _0_8 = new global::H(i: _0_9);
        _0_7 = new global::G(h: _0_8);
        try
        {
            _0_6 = new global::F(g: _0_7);
            try
            {
                _0_5 = new global::E(f: _0_6);
                try
                {
                    _0_11 = this._factory;
                    _0_10 = await _0_11.CreateAsync();
                    try
                    {
                        _0_2 = new global::A(b: _0_3, c: _0_4, e: _0_5, i: _0_10);
                        _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                        _0_0 = await _0_1.CreateAsync();
                    }
                    catch
                    {
                        await _0_11.ReleaseAsync(_0_10);
                        throw;
                    }
                }
                catch
                {
                    ((global::System.IDisposable)_0_5).Dispose();
                    throw;
                }
            }
            catch
            {
                await ((global::System.IAsyncDisposable)_0_6).DisposeAsync();
                throw;
            }
        }
        catch
        {
            await ((global::System.IAsyncDisposable)_0_7).DisposeAsync();
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_11.ReleaseAsync(_0_10);
            ((global::System.IDisposable)_0_5).Dispose();
            await ((global::System.IAsyncDisposable)_0_6).DisposeAsync();
            await ((global::System.IAsyncDisposable)_0_7).DisposeAsync();
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::AFactoryTarget>> global::StrongInject.IAsyncContainer<global::AFactoryTarget>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::BFactoryTarget _0_3;
        global::CFactoryTarget _0_4;
        global::I _0_9;
        global::H _0_8;
        global::G _0_7;
        global::F _0_6;
        global::E _0_5;
        global::StrongInject.IAsyncFactory<global::System.Int32> _0_11;
        global::System.Int32 _0_10;
        global::A _0_2;
        global::StrongInject.IAsyncFactory<global::AFactoryTarget> _0_1;
        global::AFactoryTarget _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_4 = await GetSingleInstanceField2();
        _0_9 = await GetSingleInstanceField3();
        _0_8 = new global::H(i: _0_9);
        _0_7 = new global::G(h: _0_8);
        try
        {
            _0_6 = new global::F(g: _0_7);
            try
            {
                _0_5 = new global::E(f: _0_6);
                try
                {
                    _0_11 = this._factory;
                    _0_10 = await _0_11.CreateAsync();
                    try
                    {
                        _0_2 = new global::A(b: _0_3, c: _0_4, e: _0_5, i: _0_10);
                        _0_1 = (global::StrongInject.IAsyncFactory<global::AFactoryTarget>)_0_2;
                        _0_0 = await _0_1.CreateAsync();
                    }
                    catch
                    {
                        await _0_11.ReleaseAsync(_0_10);
                        throw;
                    }
                }
                catch
                {
                    ((global::System.IDisposable)_0_5).Dispose();
                    throw;
                }
            }
            catch
            {
                await ((global::System.IAsyncDisposable)_0_6).DisposeAsync();
                throw;
            }
        }
        catch
        {
            await ((global::System.IAsyncDisposable)_0_7).DisposeAsync();
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::AFactoryTarget>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            await _0_11.ReleaseAsync(_0_10);
            ((global::System.IDisposable)_0_5).Dispose();
            await ((global::System.IAsyncDisposable)_0_6).DisposeAsync();
            await ((global::System.IAsyncDisposable)_0_7).DisposeAsync();
        });
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
    [Register(typeof(A))]
    public partial class Container : IAsyncContainer<A>
    {
    }

    public class A 
    {
    }
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::N.O.P.A _0_0;
            _0_0 = new global::N.O.P.A();
            TResult result;
            try
            {
                result = await func(_0_0, param);
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
            global::N.O.P.A _0_0;
            _0_0 = new global::N.O.P.A();
            return new global::StrongInject.AsyncOwned<global::N.O.P.A>(_0_0, async () =>
            {
            });
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
            [Register(typeof(A))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
                    global::N.O.P.Outer1.Outer2.A _0_0;
                    _0_0 = new global::N.O.P.Outer1.Outer2.A();
                    TResult result;
                    try
                    {
                        result = await func(_0_0, param);
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
                    global::N.O.P.Outer1.Outer2.A _0_0;
                    _0_0 = new global::N.O.P.Outer1.Outer2.A();
                    return new global::StrongInject.AsyncOwned<global::N.O.P.Outer1.Outer2.A>(_0_0, async () =>
                    {
                    });
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
            [Register(typeof(A))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
                    global::N.O.P.A _0_0;
                    _0_0 = new global::N.O.P.A();
                    TResult result;
                    try
                    {
                        result = await func(_0_0, param);
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
                    global::N.O.P.A _0_0;
                    _0_0 = new global::N.O.P.A();
                    return new global::StrongInject.AsyncOwned<global::N.O.P.A>(_0_0, async () =>
                    {
                    });
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A
{
    public A(ref B b){}
}
public class B{}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,2): Error SI0019: parameter 'ref B' of constructor 'A.A(ref B)' is passed as 'Ref'.
                // Register(typeof(A))
                new DiagnosticResult("SI0019", @"Register(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2),
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

[RegisterFactory(typeof(A))]
[Register(typeof(B))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,2): Error SI0019: parameter 'ref B' of constructor 'A.A(ref B)' is passed as 'Ref'.
                // RegisterFactory(typeof(A))
                new DiagnosticResult("SI0019", @"RegisterFactory(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2),
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

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[RegisterFactory(typeof(A))]
public partial class Container : IContainer<int>
{
}

public class A : IAsyncFactory<int>
{
    public ValueTask<int> CreateAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[RegisterFactory(typeof(A))]
public partial class Container : IContainer<int>
{
}

public class A : IFactory<int>, IRequiresAsyncInitialization
{
    public int Create() => default;
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
    [Instance(Options.UseAsFactory)] public IAsyncFactory<int> _instanceProvider;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[RegisterFactory(typeof(A))]
public partial class Container : IContainer<int>
{
    [Instance(Options.AsEverythingPossible)] public IAsyncFactory<IFactory<B>> _instanceProvider;
}

public class A : IFactory<int>
{
    public A(B b) {}
    public int Create() => default;
}
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[RegisterFactory(typeof(C))]
[Register(typeof(A))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void CanGenerateSynchronousContainer()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresInitialization { public A(B b){} public void Initialize() {}}
public class B : IRequiresInitialization { public void Initialize() {} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        ((global::StrongInject.IRequiresInitialization)_0_1).Initialize();
        _0_0 = new global::A(b: _0_1);
        ((global::StrongInject.IRequiresInitialization)_0_0).Initialize();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        ((global::StrongInject.IRequiresInitialization)_0_1).Initialize();
        _0_0 = new global::A(b: _0_1);
        ((global::StrongInject.IRequiresInitialization)_0_0).Initialize();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithFactories()
        {
            string userSource = @"
using StrongInject;

[RegisterFactory(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<int>
{
}

public class A : IFactory<int> { public A(B b){} public int Create() => default; }
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_3;
        global::A _0_2;
        global::StrongInject.IFactory<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_3 = new global::B();
        _0_2 = new global::A(b: _0_3);
        _0_1 = (global::StrongInject.IFactory<global::System.Int32>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_3;
        global::A _0_2;
        global::StrongInject.IFactory<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_3 = new global::B();
        _0_2 = new global::A(b: _0_3);
        _0_1 = (global::StrongInject.IFactory<global::System.Int32>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::System.Int32>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithInstanceProviders()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
    [Instance(Options.UseAsFactory)] IFactory<int> _instanceProvider;
}

public class A { public A(B b, int i){} }
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify(
                // (8,52): Warning CS0649: Field 'Container._instanceProvider' is never assigned to, and will always have its default value null
                // _instanceProvider
                new DiagnosticResult("CS0649", @"_instanceProvider", DiagnosticSeverity.Warning).WithLocation(8, 52));
            generatorDiagnostics.Verify();
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
        global::B _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_3;
        global::System.Int32 _0_2;
        global::A _0_0;
        _0_1 = new global::B();
        _0_3 = this._instanceProvider;
        _0_2 = _0_3.Create();
        try
        {
            _0_0 = new global::A(b: _0_1, i: _0_2);
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_3.Release(_0_2);
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_3;
        global::System.Int32 _0_2;
        global::A _0_0;
        _0_1 = new global::B();
        _0_3 = this._instanceProvider;
        _0_2 = _0_3.Create();
        try
        {
            _0_0 = new global::A(b: _0_1, i: _0_2);
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
            _0_3.Release(_0_2);
        });
    }
}");
        }

        [Fact]
        public void CanGenerateSynchronousContainerWithSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_0;
            _0_0 = new global::B();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
            };
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void SynchronousAndAsynchronousResolvesCanShareSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
[Register(typeof(C))]
[Register(typeof(D), Scope.SingleInstance)]
public partial class Container : IContainer<A>, IAsyncContainer<C>
{
}

public class A { public A(B b){} }
public class B {}
public class C : IRequiresAsyncInitialization { public C(B b, D d) {} public ValueTask InitializeAsync() => default; }
public class D : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_0;
            _0_0 = new global::B();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
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
            global::D _0_0;
            _0_0 = new global::D();
            await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
            };
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
        global::B _0_1;
        global::D _0_2;
        global::C _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = await GetSingleInstanceField1();
        _0_0 = new global::C(b: _0_1, d: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::B _0_1;
        global::D _0_2;
        global::C _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = await GetSingleInstanceField1();
        _0_0 = new global::C(b: _0_1, d: _0_2);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DisposalOfSingleInstanceDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IDisposable { public void Dispose(){} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_0;
            _0_0 = new global::B();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
            };
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DisposalOfMultipleSingleInstanceDependencies()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
[Register(typeof(C), Scope.SingleInstance)]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b){} }
public class B : IDisposable { public B(C c){} public void Dispose(){} }
public class C : IDisposable { public void Dispose(){} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_0;
            _0_0 = new global::C();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
            };
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
            global::C _0_1;
            global::B _0_0;
            _0_1 = GetSingleInstanceField1();
            _0_0 = new global::B(c: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
            };
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DoesNotDisposeUnusedSingleInstanceDependencies()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B), Scope.SingleInstance)]
[Register(typeof(C), Scope.SingleInstance)]
public partial class Container : IContainer<C>
{
}

public class A : IDisposable { public A(A a){} public void Dispose(){} }
public class B : IDisposable { public void Dispose(){} }
public class C : IDisposable { public void Dispose(){} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::C _0_0;
            _0_0 = new global::C();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
                ((global::System.IDisposable)_0_0).Dispose();
            };
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
        global::C _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithoutParameters()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IAsyncContainer<Func<A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::A> _0_0;
        _0_0 = () =>
        {
            global::B _1_1;
            global::A _1_0;
            _1_1 = new global::B();
            _1_0 = new global::A(b: _1_1);
            return _1_0;
        };
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::A> _0_0;
        _0_0 = () =>
        {
            global::B _1_1;
            global::A _1_0;
            _1_1 = new global::B();
            _1_0 = new global::A(b: _1_1);
            return _1_0;
        };
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithParametersWhereParameterTypeIsRegistered()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(b: param0_0);
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(b: param0_0);
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncWithParametersWhereParameterTypeIsNotRegistered()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Func<B, A>>
{
}

public class A 
{
    public A(B b){}
}
public class B {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(b: param0_0);
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(b: param0_0);
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncUsedAsParameter()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<A>
{
}

public class A 
{
    public A(Func<int, string, B> b){}
}
public class B { public B(int i, string s, int i1){} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Int32, global::System.String, global::B> _0_1;
        global::A _0_0;
        _0_1 = (param0_0, param0_1) =>
        {
            global::B _1_0;
            _1_0 = new global::B(i: param0_0, s: param0_1, i1: param0_0);
            return _1_0;
        };
        _0_0 = new global::A(b: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::System.Func<global::System.Int32, global::System.String, global::B> _0_1;
        global::A _0_0;
        _0_1 = (param0_0, param0_1) =>
        {
            global::B _1_0;
            _1_0 = new global::B(i: param0_0, s: param0_1, i1: param0_0);
            return _1_0;
        };
        _0_0 = new global::A(b: _0_1);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncUsedInsideFuncResolution()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
public partial class Container : IContainer<Func<int, A>>
{
}

public class A 
{
    public A(int a, Func<string, B> func){}
}
public class B { public B(int i, string s){} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Int32, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.String, global::B> _1_1;
            global::A _1_0;
            _1_1 = (param1_0) =>
            {
                global::B _2_0;
                _2_0 = new global::B(i: param0_0, s: param1_0);
                return _2_0;
            };
            _1_0 = new global::A(a: param0_0, func: _1_1);
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::A>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Int32, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.String, global::B> _1_1;
            global::A _1_0;
            _1_1 = (param1_0) =>
            {
                global::B _2_0;
                _2_0 = new global::B(i: param0_0, s: param1_0);
                return _2_0;
            };
            _1_0 = new global::A(a: param0_0, func: _1_1);
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::A>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveFuncOfFuncOfFunc()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Func<bool, Func<string, Func<int, A>>>>
{
}

public class A 
{
    public A(int a, string b, bool c){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>> _1_0;
            _1_0 = (param1_0) =>
            {
                global::System.Func<global::System.Int32, global::A> _2_0;
                _2_0 = (param2_0) =>
                {
                    global::A _3_0;
                    _3_0 = new global::A(a: param2_0, b: param1_0, c: param0_0);
                    return _3_0;
                };
                return _2_0;
            };
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>> global::StrongInject.IContainer<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>> _1_0;
            _1_0 = (param1_0) =>
            {
                global::System.Func<global::System.Int32, global::A> _2_0;
                _2_0 = (param2_0) =>
                {
                    global::A _3_0;
                    _3_0 = new global::A(a: param2_0, b: param1_0, c: param0_0);
                    return _3_0;
                };
                return _2_0;
            };
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::System.Boolean, global::System.Func<global::System.String, global::System.Func<global::System.Int32, global::A>>>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DisposesOfFuncDependencies()
        {
            string userSource = @"
using System;
using StrongInject;
using StrongInject.Modules;

[Register(typeof(A))]
[Register(typeof(B))]
[RegisterModule(typeof(ValueTupleModule))]
public partial class Container : IContainer<(B, Func<A>)>
{
}

public class A : IDisposable
{
    public A(B b){}
    public void Dispose(){}
}
public class B : IDisposable
{
    public void Dispose(){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<(global::B, global::System.Func<global::A>)>.Run<TResult, TParam>(global::System.Func<(global::B, global::System.Func<global::A>), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Action> disposeActions__0_2;
        global::System.Func<global::A> _0_2;
        (global::B, global::System.Func<global::A>) _0_0;
        _0_1 = new global::B();
        try
        {
            disposeActions__0_2 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            _0_2 = () =>
            {
                global::B _1_1;
                global::A _1_0;
                _1_1 = new global::B();
                try
                {
                    _1_0 = new global::A(b: _1_1);
                }
                catch
                {
                    ((global::System.IDisposable)_1_1).Dispose();
                    throw;
                }

                disposeActions__0_2.Add(() =>
                {
                    ((global::System.IDisposable)_1_0).Dispose();
                    ((global::System.IDisposable)_1_1).Dispose();
                });
                return _1_0;
            };
            try
            {
                _0_0 = global::StrongInject.Modules.ValueTupleModule.CreateValueTuple<global::B, global::System.Func<global::A>>(a: _0_1, b: _0_2);
            }
            catch
            {
                foreach (var disposeAction in disposeActions__0_2)
                    disposeAction();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)_0_1).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions__0_2)
                disposeAction();
            ((global::System.IDisposable)_0_1).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<(global::B, global::System.Func<global::A>)> global::StrongInject.IContainer<(global::B, global::System.Func<global::A>)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Action> disposeActions__0_2;
        global::System.Func<global::A> _0_2;
        (global::B, global::System.Func<global::A>) _0_0;
        _0_1 = new global::B();
        try
        {
            disposeActions__0_2 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
            _0_2 = () =>
            {
                global::B _1_1;
                global::A _1_0;
                _1_1 = new global::B();
                try
                {
                    _1_0 = new global::A(b: _1_1);
                }
                catch
                {
                    ((global::System.IDisposable)_1_1).Dispose();
                    throw;
                }

                disposeActions__0_2.Add(() =>
                {
                    ((global::System.IDisposable)_1_0).Dispose();
                    ((global::System.IDisposable)_1_1).Dispose();
                });
                return _1_0;
            };
            try
            {
                _0_0 = global::StrongInject.Modules.ValueTupleModule.CreateValueTuple<global::B, global::System.Func<global::A>>(a: _0_1, b: _0_2);
            }
            catch
            {
                foreach (var disposeAction in disposeActions__0_2)
                    disposeAction();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)_0_1).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<(global::B, global::System.Func<global::A>)>(_0_0, () =>
        {
            foreach (var disposeAction in disposeActions__0_2)
                disposeAction();
            ((global::System.IDisposable)_0_1).Dispose();
        });
    }
}");
        }

        [Fact]
        public void DisposesOfFuncDependenciesAsynchronously()
        {
            string userSource = @"
using System;
using StrongInject;
using StrongInject.Modules;
using System.Threading.Tasks;

[Register(typeof(A))]
[Register(typeof(B))]
[RegisterModule(typeof(ValueTupleModule))]
public partial class Container : IAsyncContainer<(B, Func<A>)>
{
}

public class A : IAsyncDisposable
{
    public A(B b){}
    public ValueTask DisposeAsync() => default;
}
public class B : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<(global::B, global::System.Func<global::A>)>.RunAsync<TResult, TParam>(global::System.Func<(global::B, global::System.Func<global::A>), TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>> disposeActions__0_2;
        global::System.Func<global::A> _0_2;
        (global::B, global::System.Func<global::A>) _0_0;
        _0_1 = new global::B();
        try
        {
            disposeActions__0_2 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
            _0_2 = () =>
            {
                global::B _1_1;
                global::A _1_0;
                _1_1 = new global::B();
                _1_0 = new global::A(b: _1_1);
                disposeActions__0_2.Add(async () =>
                {
                    await ((global::System.IAsyncDisposable)_1_0).DisposeAsync();
                    await ((global::System.IAsyncDisposable)_1_1).DisposeAsync();
                });
                return _1_0;
            };
            try
            {
                _0_0 = global::StrongInject.Modules.ValueTupleModule.CreateValueTuple<global::B, global::System.Func<global::A>>(a: _0_1, b: _0_2);
            }
            catch
            {
                foreach (var disposeAction in disposeActions__0_2)
                    await disposeAction();
                throw;
            }
        }
        catch
        {
            await ((global::System.IAsyncDisposable)_0_1).DisposeAsync();
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions__0_2)
                await disposeAction();
            await ((global::System.IAsyncDisposable)_0_1).DisposeAsync();
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<(global::B, global::System.Func<global::A>)>> global::StrongInject.IAsyncContainer<(global::B, global::System.Func<global::A>)>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>> disposeActions__0_2;
        global::System.Func<global::A> _0_2;
        (global::B, global::System.Func<global::A>) _0_0;
        _0_1 = new global::B();
        try
        {
            disposeActions__0_2 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Func<global::System.Threading.Tasks.ValueTask>>();
            _0_2 = () =>
            {
                global::B _1_1;
                global::A _1_0;
                _1_1 = new global::B();
                _1_0 = new global::A(b: _1_1);
                disposeActions__0_2.Add(async () =>
                {
                    await ((global::System.IAsyncDisposable)_1_0).DisposeAsync();
                    await ((global::System.IAsyncDisposable)_1_1).DisposeAsync();
                });
                return _1_0;
            };
            try
            {
                _0_0 = global::StrongInject.Modules.ValueTupleModule.CreateValueTuple<global::B, global::System.Func<global::A>>(a: _0_1, b: _0_2);
            }
            catch
            {
                foreach (var disposeAction in disposeActions__0_2)
                    await disposeAction();
                throw;
            }
        }
        catch
        {
            await ((global::System.IAsyncDisposable)_0_1).DisposeAsync();
            throw;
        }

        return new global::StrongInject.AsyncOwned<(global::B, global::System.Func<global::A>)>(_0_0, async () =>
        {
            foreach (var disposeAction in disposeActions__0_2)
                await disposeAction();
            await ((global::System.IAsyncDisposable)_0_1).DisposeAsync();
        });
    }
}");
        }

        [Fact]
        public void DisposesOfFuncDependenciesButNotParameters()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[Register(typeof(C))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Action> disposeActions__0_0;
        global::System.Func<global::B, global::A> _0_0;
        disposeActions__0_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        _0_0 = (param0_0) =>
        {
            global::C _1_1;
            global::A _1_0;
            _1_1 = new global::C();
            try
            {
                _1_0 = new global::A(b: param0_0, c: _1_1);
            }
            catch
            {
                ((global::System.IDisposable)_1_1).Dispose();
                throw;
            }

            disposeActions__0_0.Add(() =>
            {
                ((global::System.IDisposable)_1_1).Dispose();
            });
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            foreach (var disposeAction in disposeActions__0_0)
                disposeAction();
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Concurrent.ConcurrentBag<global::System.Action> disposeActions__0_0;
        global::System.Func<global::B, global::A> _0_0;
        disposeActions__0_0 = new global::System.Collections.Concurrent.ConcurrentBag<global::System.Action>();
        _0_0 = (param0_0) =>
        {
            global::C _1_1;
            global::A _1_0;
            _1_1 = new global::C();
            try
            {
                _1_0 = new global::A(b: param0_0, c: _1_1);
            }
            catch
            {
                ((global::System.IDisposable)_1_1).Dispose();
                throw;
            }

            disposeActions__0_0.Add(() =>
            {
                ((global::System.IDisposable)_1_1).Dispose();
            });
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0_0, () =>
        {
            foreach (var disposeAction in disposeActions__0_0)
                disposeAction();
        });
    }
}");
        }

        [Fact]
        public void WarningOnUnusedParameters1()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Func<int, string, A>>
{
}

public class A 
{
    public A(string s1, string s2){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Int32, global::System.String, global::A> _0_0;
        _0_0 = (param0_0, param0_1) =>
        {
            global::A _1_0;
            _1_0 = new global::A(s1: param0_1, s2: param0_1);
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.String, global::A>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.String, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Int32, global::System.String, global::A> _0_0;
        _0_0 = (param0_0, param0_1) =>
        {
            global::A _1_0;
            _1_0 = new global::A(s1: param0_1, s2: param0_1);
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.String, global::A>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WarningOnUnusedParameters2()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Func<int, Func<int, A>>>
{
}

public class A 
{
    public A(int a1, int a2){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.Int32, global::A> _1_0;
            _1_0 = (param1_0) =>
            {
                global::A _2_0;
                _2_0 = new global::A(a1: param1_0, a2: param1_0);
                return _2_0;
            };
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>> global::StrongInject.IContainer<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::System.Int32, global::A> _1_0;
            _1_0 = (param1_0) =>
            {
                global::A _2_0;
                _2_0 = new global::A(a1: param1_0, a2: param1_0);
                return _2_0;
            };
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::System.Int32, global::System.Func<global::System.Int32, global::A>>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WarningOnSingleInstanceReturnType()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A), Scope.SingleInstance)]
[Register(typeof(B))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_1;
            global::A _0_0;
            _0_1 = new global::B();
            _0_0 = new global::A(b: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
            };
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
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = GetSingleInstanceField0();
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::B, global::A>> global::StrongInject.IContainer<global::System.Func<global::B, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::B, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            global::A _1_0;
            _1_0 = GetSingleInstanceField0();
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::B, global::A>>(_0_0, () =>
        {
        });
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::A, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::A, global::A>> global::StrongInject.IContainer<global::System.Func<global::A, global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::A, global::A> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::A, global::A>>(_0_0, () =>
        {
        });
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::A, global::System.Func<global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::A> _1_0;
            _1_0 = () =>
            {
                return param0_0;
            };
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::A, global::System.Func<global::A>>> global::StrongInject.IContainer<global::System.Func<global::A, global::System.Func<global::A>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::A, global::System.Func<global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            global::System.Func<global::A> _1_0;
            _1_0 = () =>
            {
                return param0_0;
            };
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::A, global::System.Func<global::A>>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorOnMultipleParametersWithSameType()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Func<int, int, A>>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A 
{
    public A(Func<A> a){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(A))]
public partial class Container : IAsyncContainer<Func<A>>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
[Register(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
[Register(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
[Register(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A 
{
    public A(int a){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
[Register(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(int a){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::Del _0_0;
        _0_0 = async (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(a: param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_1_0).InitializeAsync();
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::Del _0_0;
        _0_0 = async (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(a: param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_1_0).InitializeAsync();
            return _1_0;
        };
        return new global::StrongInject.Owned<global::Del>(_0_0, () =>
        {
        });
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
[Register(typeof(A))]
public partial class Container : IContainer<Del>
{
}

public class A : IRequiresAsyncInitialization
{
    public A(int a){}
    public ValueTask InitializeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::Del _0_0;
        _0_0 = async (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(a: param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_1_0).InitializeAsync();
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::Del> global::StrongInject.IContainer<global::Del>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::Del _0_0;
        _0_0 = async (param0_0) =>
        {
            global::A _1_0;
            _1_0 = new global::A(a: param0_0);
            await ((global::StrongInject.IRequiresAsyncInitialization)_1_0).InitializeAsync();
            return _1_0;
        };
        return new global::StrongInject.Owned<global::Del>(_0_0, () =>
        {
        });
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
[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::Del _0_1;
            global::B _0_0;
            _0_1 = async (param0_0) =>
            {
                global::A _1_0;
                _1_0 = new global::A(a: param0_0);
                await ((global::StrongInject.IRequiresAsyncInitialization)_1_0).InitializeAsync();
                return _1_0;
            };
            _0_0 = new global::B(d: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
            };
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
        global::B _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void PreferInstanceUsedAsFactoryToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IAsyncContainer<Func<A>>
{
    [Instance(Options.UseAsFactory)] private IFactory<Func<A>> _instanceProvider;
}

public class A
{
    public A(){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (8,64): Warning CS0649: Field 'Container._instanceProvider' is never assigned to, and will always have its default value null
                // _instanceProvider
                new DiagnosticResult("CS0649", @"_instanceProvider", DiagnosticSeverity.Warning).WithLocation(8, 64));
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
        global::StrongInject.IFactory<global::System.Func<global::A>> _0_1;
        global::System.Func<global::A> _0_0;
        _0_1 = this._instanceProvider;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::System.Func<global::A>> _0_1;
        global::System.Func<global::A> _0_0;
        _0_1 = this._instanceProvider;
        _0_0 = _0_1.Create();
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0_0, async () =>
        {
            _0_1.Release(_0_0);
        });
    }
}");
        }

        [Fact]
        public void PreferFactoryToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
[RegisterFactory(typeof(B))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_2;
        global::StrongInject.IFactory<global::System.Func<global::A>> _0_1;
        global::System.Func<global::A> _0_0;
        _0_2 = new global::B();
        _0_1 = (global::StrongInject.IFactory<global::System.Func<global::A>>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::A>>> global::StrongInject.IAsyncContainer<global::System.Func<global::A>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::StrongInject.IFactory<global::System.Func<global::A>> _0_1;
        global::System.Func<global::A> _0_0;
        _0_2 = new global::B();
        _0_1 = (global::StrongInject.IFactory<global::System.Func<global::A>>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.AsyncOwned<global::System.Func<global::A>>(_0_0, async () =>
        {
            _0_1.Release(_0_0);
        });
    }
}");
        }

        [Fact]
        public void PreferDelegateParameterToProvideDelegate()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IAsyncContainer<Func<Func<A>, Func<A>>>
{
}

public class A
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>> global::StrongInject.IAsyncContainer<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        return new global::StrongInject.AsyncOwned<global::System.Func<global::System.Func<global::A>, global::System.Func<global::A>>>(_0_0, async () =>
        {
        });
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = global::Module.M(b: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = global::Module.M(b: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        });
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

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_1;
            global::A _0_0;
            _0_1 = new global::B();
            _0_0 = global::Module.M(b: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
                await global::StrongInject.Helpers.DisposeAsync(_0_0);
            };
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
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

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1002: Factory method 'Module.M(B)' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
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
        public void NonStaticFactoryMethodIgnored()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public A M(B b) => null;
}

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(B))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = this.M(b: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = this.M(b: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        });
    }
}");
        }

        [Fact]
        public void ErrorIfPrivateInstanceFactoryMethodDefinedInContainerDuplicatesExistingRegistration()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
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
        public void ErrorIfPublicStaticFactoryMethodDefinedInContainerOverridesExistingRegistration()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    public static A M(B b) => null;
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
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
        public void ErrorIfMultipleFactoryMethodsDefinedByContainerForSameType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
    [Factory]
    A M1() => null;
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
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
        public void ErrorIfInstanceUsedAsFactoryAndFactoryMethodDefinedByContainerForSameType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(B b) => null;
    [Instance(Options.UseAsFactory)]
    public IFactory<A> _instanceProvider;
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
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
        public void ErrorIfFactoryMethodReturnsVoid()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    void M(B b){}
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfPublicStaticFactoryMethodInContainerReturnsVoid()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B))]
[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [Factory]
    public static void M(B b){}
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfFactoryMethodFromModuleOverridesExisingRegistration()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
public class Module
{
    [Factory]
    public static A M(B b) => null;
}

public class A{}
public class B{}

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (16,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(16, 22));
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = await global::Module.M(b: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = await global::Module.M(b: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        });
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

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        public void ErrorIfNotAllGenericFactoryMethodTypeParametersUsedInReturnType()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory]
    public static A M<T>(B b) => null;
}

[Register(typeof(B))]
[RegisterModule(typeof(Module))]
public partial class Container : IAsyncContainer<A>
{
}

public class A{}
public class B{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Error SI0020: All type parameters must be used in return type of generic factory method 'Module.M<T>(B)'
                // Factory
                new DiagnosticResult("SI0020", @"Factory", DiagnosticSeverity.Error).WithLocation(6, 6),
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
        public void ErrorIfFactoryMethodIsRecursive()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IAsyncContainer<A>
{
    [Factory]
    A M(A a) => null;
}

public class A{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,22): Error SI0101: Error while resolving dependencies for 'A': 'A' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
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
        public void ErrorIfFactoryMethodRequiresAsyncResolutionInSyncContainer()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(B))]
public partial class Container : IContainer<A>
{
    [Factory]
    A M(B b) => null;
}

public class A{}
public class B : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
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
        public void FactoryMethodRequiringAsyncResolutionCanBeSingleInstance()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(B))]
public partial class Container : IAsyncContainer<A>
{
    [Factory(Scope.SingleInstance)]
    A M(B b) => null;
}

public class A{}
public class B : IRequiresAsyncInitialization
{
    public ValueTask InitializeAsync() => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
    private async global::System.Threading.Tasks.ValueTask<global::A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::B _0_1;
            global::A _0_0;
            _0_1 = new global::B();
            await ((global::StrongInject.IRequiresAsyncInitialization)_0_1).InitializeAsync();
            _0_0 = this.M(b: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
                await global::StrongInject.Helpers.DisposeAsync(_0_0);
            };
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
        global::A _0_0;
        _0_0 = await GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_0;
        _0_0 = await GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void TestAsyncFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<A>
{
    [Factory] ValueTask<A> M() => default;
}

public class A{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = await this.M();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = await this.M();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        });
    }
}");
        }

        [Fact]
        public void TestAsyncGenericFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<A>
{
    [Factory] ValueTask<T> M<T>() => default;
}

public class A{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = await this.M<global::A>();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::A>> global::StrongInject.IAsyncContainer<global::A>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = await this.M<global::A>();
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_0);
        });
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

[Register(typeof(A))]
public partial class Container : IContainer<A>
{
}

public class A : IAsyncDisposable
{
    public ValueTask DisposeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void NoErrorIfMultipleDependenciesRegisteredForATypeButNoneUsed()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(A), typeof(IInterface))]
[Register(typeof(B), typeof(B), typeof(IInterface))]
public partial class Container : IContainer<A>, IContainer<B>
{
}

public interface IInterface {}
public class A : IInterface {}
public class B : IInterface {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_0;
        _0_0 = new global::B();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_0;
        _0_0 = new global::B();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanImportInstanceFieldFromModule()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public static readonly A Instance;
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = global::Module.Instance;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = global::Module.Instance;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanImportInstancePropertyFromModule()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public static A Instance { get; }
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = global::Module.Instance;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = global::Module.Instance;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WarningIfInstanceFieldIsNotStatic()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public readonly A Instance;
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1004: Instance field 'Module.Instance' is not static, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void WarningIfInstancePropertyIsNotStatic()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public A Instance { get; }
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1004: Instance property 'Module.Instance' is not static, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void WarningIfInstanceFieldIsNotPublic()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    internal static readonly A Instance = null;
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1004: Instance field 'Module.Instance' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void WarningIfInstancePropertyIsNotPublic()
        {
            string userSource = @"
using StrongInject;

internal class Module
{
    [Instance]
    public static A Instance { get; }
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1004: Instance property 'Module.Instance' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void ErrorIfInstancePropertyIsWriteOnly()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public static A Instance { set {} }
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Error SI0021: Instance property 'Module.Instance' is write only.
                // Instance
                new DiagnosticResult("SI0021", @"Instance", DiagnosticSeverity.Error).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void WarningIfInstancePropertyGetMethodIsNotPublic()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Instance]
    public static A Instance { internal get; set; }
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<A>
{
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1004: Instance property 'Module.Instance' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (11,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(11, 22));
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
        public void CanUsePrivateInstanceFieldOnContainer()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<A>
{
    [Instance] private A AInstance = null;
}

public class A {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = this.AInstance;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this.AInstance;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DoesNotCallDisposeOnInstanceField()
        {
            string userSource = @"
using StrongInject;
using System;

public partial class Container : IContainer<IDisposable>
{
    [Instance] private IDisposable DisposableInstance = null;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::System.IDisposable>.Run<TResult, TParam>(global::System.Func<global::System.IDisposable, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.IDisposable _0_0;
        _0_0 = this.DisposableInstance;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.IDisposable> global::StrongInject.IContainer<global::System.IDisposable>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.IDisposable _0_0;
        _0_0 = this.DisposableInstance;
        return new global::StrongInject.Owned<global::System.IDisposable>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ArrayResolvesAllRegistrationsForType()
        {
            string userSource = @"
using StrongInject;

public class A : IA {}
public class B : IA {}
public class C : IA {}
public class IAFactory : IFactory<IA>
{
    public IA Create() => null;
}

[Register(typeof(B), Scope.SingleInstance, typeof(IA))]
[Register(typeof(C))]
[RegisterFactory(typeof(IAFactory))]
public class Module
{
    [Factory] public static IA FactoryOfA() => null; 
}

public interface IA {}

[Register(typeof(A), typeof(IA))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<IA[]>
{
    [Instance] private IA AInstance = null;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_0;
            _0_0 = new global::B();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IAFactory _0_3;
        global::StrongInject.IFactory<global::IA> _0_2;
        global::IA _0_1;
        global::B _0_5;
        global::IA _0_4;
        global::IA _0_6;
        global::IA _0_7;
        global::A _0_9;
        global::IA _0_8;
        global::IA[] _0_0;
        _0_3 = new global::IAFactory();
        _0_2 = (global::StrongInject.IFactory<global::IA>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_5 = GetSingleInstanceField0();
            _0_4 = (global::IA)_0_5;
            _0_6 = global::Module.FactoryOfA();
            try
            {
                _0_7 = this.AInstance;
                _0_9 = new global::A();
                _0_8 = (global::IA)_0_9;
                _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_4, (global::IA)_0_6, (global::IA)_0_7, (global::IA)_0_8, };
            }
            catch
            {
                global::StrongInject.Helpers.Dispose(_0_6);
                throw;
            }
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_6);
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IAFactory _0_3;
        global::StrongInject.IFactory<global::IA> _0_2;
        global::IA _0_1;
        global::B _0_5;
        global::IA _0_4;
        global::IA _0_6;
        global::IA _0_7;
        global::A _0_9;
        global::IA _0_8;
        global::IA[] _0_0;
        _0_3 = new global::IAFactory();
        _0_2 = (global::StrongInject.IFactory<global::IA>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_5 = GetSingleInstanceField0();
            _0_4 = (global::IA)_0_5;
            _0_6 = global::Module.FactoryOfA();
            try
            {
                _0_7 = this.AInstance;
                _0_9 = new global::A();
                _0_8 = (global::IA)_0_9;
                _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_4, (global::IA)_0_6, (global::IA)_0_7, (global::IA)_0_8, };
            }
            catch
            {
                global::StrongInject.Helpers.Dispose(_0_6);
                throw;
            }
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::IA[]>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_6);
            _0_2.Release(_0_1);
        });
    }
}");
        }

        [Fact]
        public void ArrayIgnoresDuplicateRegistrationForType1()
        {
            string userSource = @"
using StrongInject;

public class A : IA {}
public class B : IA {}
public interface IA {}

[Register(typeof(B), typeof(IA))]
[Register(typeof(A), typeof(IA))]
public class Module
{
}

[Register(typeof(A), typeof(IA))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<IA[]>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_3;
        global::IA[] _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_4 = new global::B();
        _0_3 = (global::IA)_0_4;
        _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_3, };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_3;
        global::IA[] _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_4 = new global::B();
        _0_3 = (global::IA)_0_4;
        _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_3, };
        return new global::StrongInject.Owned<global::IA[]>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ArrayIgnoresDuplicateRegistrationForType2()
        {
            string userSource = @"
using StrongInject;

public class A : IA {}
public class B : IA {}
public interface IA {}

[Register(typeof(B), typeof(IA))]
[Register(typeof(A), typeof(IA))]
public class Module1
{
}

[Register(typeof(A), typeof(IA))]
public class Module2
{
}

[RegisterModule(typeof(Module1))]
[RegisterModule(typeof(Module2))]
public partial class Container : IContainer<IA[]>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IA _0_1;
        global::A _0_4;
        global::IA _0_3;
        global::IA[] _0_0;
        _0_2 = new global::B();
        _0_1 = (global::IA)_0_2;
        _0_4 = new global::A();
        _0_3 = (global::IA)_0_4;
        _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_3, };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IA _0_1;
        global::A _0_4;
        global::IA _0_3;
        global::IA[] _0_0;
        _0_2 = new global::B();
        _0_1 = (global::IA)_0_2;
        _0_4 = new global::A();
        _0_3 = (global::IA)_0_4;
        _0_0 = new global::IA[]{(global::IA)_0_1, (global::IA)_0_3, };
        return new global::StrongInject.Owned<global::IA[]>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ArrayIgnoresExludedRegistrations()
        {
            string userSource = @"
using StrongInject;

public class A : IA {}
public class B : IA {}
public interface IA {}

[Register(typeof(A), typeof(IA))]
public class Module
{
}

[RegisterModule(typeof(Module), typeof(IA))]
public partial class Container : IContainer<IA[]>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (14,22): Warning SI1105: Warning while resolving dependencies for 'IA[]': Resolving all registration of type 'IA', but there are no such registrations.
                // Container
                new DiagnosticResult("SI1105", @"Container", DiagnosticSeverity.Warning).WithLocation(14, 22));
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

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA[] _0_0;
        _0_0 = new global::IA[]{};
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA[] _0_0;
        _0_0 = new global::IA[]{};
        return new global::StrongInject.Owned<global::IA[]>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfArrayDependenciesAreRecursive()
        {
            string userSource = @"
using StrongInject;

public class A : IA { public A(IA[] ia){} }
public class B : IA {}
public interface IA {}

[Register(typeof(A), typeof(IA))]
public class Module
{
}

[Register(typeof(B), typeof(IA))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<IA[]>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (15,22): Error SI0101: Error while resolving dependencies for 'IA[]': 'IA[]' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(15, 22));
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

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ErrorIfArrayDependenciesRequireAsyncResolutionInSyncContainer()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public class A : IA, IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
public class B : IA {}
public interface IA {}

[Register(typeof(A), typeof(IA))]
public class Module
{
}

[Register(typeof(B), typeof(IA))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<IA[]>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (16,22): Error SI0103: Error while resolving dependencies for 'IA[]': 'IA' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(16, 22));
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

    TResult global::StrongInject.IContainer<global::IA[]>.Run<TResult, TParam>(global::System.Func<global::IA[], TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::IA[]> global::StrongInject.IContainer<global::IA[]>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void ArrayDependenciesDontIncludeDelegateParameters()
        {
            string userSource = @"
using StrongInject;
using System;

public class A : IA {}
public class B : IA {}
public interface IA {}

[Register(typeof(A), typeof(IA))]
public class Module
{
}

[Register(typeof(B), typeof(IA))]
[RegisterModule(typeof(Module))]
public partial class Container : IContainer<Func<IA, IA[]>>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (16,22): Warning SI1101: Warning while resolving dependencies for 'System.Func<IA, IA[]>': Parameter 'IA' of delegate 'IA[]' is not used in resolution of 'IA[]'.
                // Container
                new DiagnosticResult("SI1101", @"Container", DiagnosticSeverity.Warning).WithLocation(16, 22));
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

    TResult global::StrongInject.IContainer<global::System.Func<global::IA, global::IA[]>>.Run<TResult, TParam>(global::System.Func<global::System.Func<global::IA, global::IA[]>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::IA, global::IA[]> _0_0;
        _0_0 = (param0_0) =>
        {
            global::B _1_2;
            global::IA _1_1;
            global::A _1_4;
            global::IA _1_3;
            global::IA[] _1_0;
            _1_2 = new global::B();
            _1_1 = (global::IA)_1_2;
            _1_4 = new global::A();
            _1_3 = (global::IA)_1_4;
            _1_0 = new global::IA[]{(global::IA)_1_1, (global::IA)_1_3, };
            return _1_0;
        };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Func<global::IA, global::IA[]>> global::StrongInject.IContainer<global::System.Func<global::IA, global::IA[]>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::IA, global::IA[]> _0_0;
        _0_0 = (param0_0) =>
        {
            global::B _1_2;
            global::IA _1_1;
            global::A _1_4;
            global::IA _1_3;
            global::IA[] _1_0;
            _1_2 = new global::B();
            _1_1 = (global::IA)_1_2;
            _1_4 = new global::A();
            _1_3 = (global::IA)_1_4;
            _1_0 = new global::IA[]{(global::IA)_1_1, (global::IA)_1_3, };
            return _1_0;
        };
        return new global::StrongInject.Owned<global::System.Func<global::IA, global::IA[]>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveSimpleTypeFromGenericFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<string>
{
    [Factory] T Resolve<T>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<global::System.String>.Run<TResult, TParam>(global::System.Func<global::System.String, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.String _0_0;
        _0_0 = this.Resolve<global::System.String>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.String> global::StrongInject.IContainer<global::System.String>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.String _0_0;
        _0_0 = this.Resolve<global::System.String>();
        return new global::StrongInject.Owned<global::System.String>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveNamedTypeFromGenericFactoryMethod1()
        {
            string userSource = @"
using StrongInject;
using System.Collections.Generic;

public partial class Container : IContainer<List<string>>
{
    [Factory] List<T> Resolve<T>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String>>.Run<TResult, TParam>(global::System.Func<global::System.Collections.Generic.List<global::System.String>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String> _0_0;
        _0_0 = this.Resolve<global::System.String>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String>> global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String> _0_0;
        _0_0 = this.Resolve<global::System.String>();
        return new global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String>>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
        }

        [Fact]
        public void CanResolveNamedTypeFromGenericFactoryMethod2()
        {
            string userSource = @"
using StrongInject;
using System.Collections.Generic;

public partial class Container : IContainer<List<string[]>>
{
    [Factory] List<T> Resolve<T>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String[]>>.Run<TResult, TParam>(global::System.Func<global::System.Collections.Generic.List<global::System.String[]>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String[]> _0_0;
        _0_0 = this.Resolve<global::System.String[]>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String[]>> global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String[]>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String[]> _0_0;
        _0_0 = this.Resolve<global::System.String[]>();
        return new global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String[]>>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
        }

        [Fact]
        public void CanResolveNamedTypeFromGenericFactoryMethod3()
        {
            string userSource = @"
using StrongInject;
using System.Collections.Generic;

public partial class Container : IContainer<List<string[]>>
{
    [Factory] List<T[]> Resolve<T>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String[]>>.Run<TResult, TParam>(global::System.Func<global::System.Collections.Generic.List<global::System.String[]>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String[]> _0_0;
        _0_0 = this.Resolve<global::System.String>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String[]>> global::StrongInject.IContainer<global::System.Collections.Generic.List<global::System.String[]>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Collections.Generic.List<global::System.String[]> _0_0;
        _0_0 = this.Resolve<global::System.String>();
        return new global::StrongInject.Owned<global::System.Collections.Generic.List<global::System.String[]>>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
        }

        [Fact]
        public void CanResolveNamedTypeFromGenericFactoryMethod4()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<(int, object, int, int)>
{
    [Factory] (T1, T2, T1, T3) Resolve<T1, T2, T3>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32) _0_0;
        _0_0 = this.Resolve<global::System.Int32, global::System.Object, global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32) _0_0;
        _0_0 = this.Resolve<global::System.Int32, global::System.Object, global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Object, global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanResolveArrayTypeFromGenericFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<(int, object, int, string)[]>
{
    [Factory] (T1, T2, T1, T3)[] Resolve<T1, T2, T3>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[]>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[] _0_0;
        _0_0 = this.Resolve<global::System.Int32, global::System.Object, global::System.String>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[]> global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[] _0_0;
        _0_0 = this.Resolve<global::System.Int32, global::System.Object, global::System.String>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Object, global::System.Int32, global::System.String)[]>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
        }

        [Fact]
        public void CanResolveTypeIncludingClassTypeParameterFromGenericFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public partial class Container<T> : IContainer<(T, int)>
{
    [Factory] (T, T1) Resolve<T1>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container<T>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<(T, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(T, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container<T>));
        (T, global::System.Int32) _0_0;
        _0_0 = this.Resolve<global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(T, global::System.Int32)> global::StrongInject.IContainer<(T, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container<T>));
        (T, global::System.Int32) _0_0;
        _0_0 = this.Resolve<global::System.Int32>();
        return new global::StrongInject.Owned<(T, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfNotAllTypeParametersUsedInReturnType()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<(int, object, int)>
{
    [Factory] (T1, T2, T1) Resolve<T1, T2, T3>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for '(int, object, int)': We have no source for instance of type '(int, object, int)'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (6,6): Error SI0020: All type parameters must be used in return type of generic factory method 'Container.Resolve<T1, T2, T3>()'
                // Factory
                new DiagnosticResult("SI0020", @"Factory", DiagnosticSeverity.Error).WithLocation(6, 6));
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Object, global::System.Int32), TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Object, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Object, global::System.Int32)>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void LooksForRegisteredInstancesOfArgumentsOfConstructedType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A<int>))]
public partial class Container : IContainer<int>
{
    [Factory] T Resolve<T>(A<T> a) => default;
}

public class A<T>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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
        global::A<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_1 = new global::A<global::System.Int32>();
        _0_0 = this.Resolve<global::System.Int32>(a: _0_1);
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_1 = new global::A<global::System.Int32>();
        _0_0 = this.Resolve<global::System.Int32>(a: _0_1);
        return new global::StrongInject.Owned<global::System.Int32>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorOnRecursiveGenericMethodFactoryDependencies()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<int>
{
    [Factory] T Resolve<T>(A<T> a) => default;
}

public class A<T>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0107: Error while resolving dependencies for 'int': The Dependency tree is deeper than the maximum depth of 200.
                // Container
                new DiagnosticResult("SI0107", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
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
        public void FollowsRecursiveGenericMethodFactoryDependenciesToPossibleResolution()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A<A<A<A<A<A<A<A<A<A<int>>>>>>>>>>))]
public partial class Container : IContainer<int>
{
    [Factory] T Resolve<T>(A<T> a) => default;
}

public class A<T>
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>> _0_10;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>> _0_9;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>> _0_8;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>> _0_7;
        global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>> _0_6;
        global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>> _0_5;
        global::A<global::A<global::A<global::A<global::System.Int32>>>> _0_4;
        global::A<global::A<global::A<global::System.Int32>>> _0_3;
        global::A<global::A<global::System.Int32>> _0_2;
        global::A<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_10 = new global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>>();
        _0_9 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>>(a: _0_10);
        try
        {
            _0_8 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>(a: _0_9);
            try
            {
                _0_7 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>(a: _0_8);
                try
                {
                    _0_6 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>(a: _0_7);
                    try
                    {
                        _0_5 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>(a: _0_6);
                        try
                        {
                            _0_4 = this.Resolve<global::A<global::A<global::A<global::A<global::System.Int32>>>>>(a: _0_5);
                            try
                            {
                                _0_3 = this.Resolve<global::A<global::A<global::A<global::System.Int32>>>>(a: _0_4);
                                try
                                {
                                    _0_2 = this.Resolve<global::A<global::A<global::System.Int32>>>(a: _0_3);
                                    try
                                    {
                                        _0_1 = this.Resolve<global::A<global::System.Int32>>(a: _0_2);
                                        try
                                        {
                                            _0_0 = this.Resolve<global::System.Int32>(a: _0_1);
                                        }
                                        catch
                                        {
                                            global::StrongInject.Helpers.Dispose(_0_1);
                                            throw;
                                        }
                                    }
                                    catch
                                    {
                                        global::StrongInject.Helpers.Dispose(_0_2);
                                        throw;
                                    }
                                }
                                catch
                                {
                                    global::StrongInject.Helpers.Dispose(_0_3);
                                    throw;
                                }
                            }
                            catch
                            {
                                global::StrongInject.Helpers.Dispose(_0_4);
                                throw;
                            }
                        }
                        catch
                        {
                            global::StrongInject.Helpers.Dispose(_0_5);
                            throw;
                        }
                    }
                    catch
                    {
                        global::StrongInject.Helpers.Dispose(_0_6);
                        throw;
                    }
                }
                catch
                {
                    global::StrongInject.Helpers.Dispose(_0_7);
                    throw;
                }
            }
            catch
            {
                global::StrongInject.Helpers.Dispose(_0_8);
                throw;
            }
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_9);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            global::StrongInject.Helpers.Dispose(_0_2);
            global::StrongInject.Helpers.Dispose(_0_3);
            global::StrongInject.Helpers.Dispose(_0_4);
            global::StrongInject.Helpers.Dispose(_0_5);
            global::StrongInject.Helpers.Dispose(_0_6);
            global::StrongInject.Helpers.Dispose(_0_7);
            global::StrongInject.Helpers.Dispose(_0_8);
            global::StrongInject.Helpers.Dispose(_0_9);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>> _0_10;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>> _0_9;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>> _0_8;
        global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>> _0_7;
        global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>> _0_6;
        global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>> _0_5;
        global::A<global::A<global::A<global::A<global::System.Int32>>>> _0_4;
        global::A<global::A<global::A<global::System.Int32>>> _0_3;
        global::A<global::A<global::System.Int32>> _0_2;
        global::A<global::System.Int32> _0_1;
        global::System.Int32 _0_0;
        _0_10 = new global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>>();
        _0_9 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>>(a: _0_10);
        try
        {
            _0_8 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>>(a: _0_9);
            try
            {
                _0_7 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>>(a: _0_8);
                try
                {
                    _0_6 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>>(a: _0_7);
                    try
                    {
                        _0_5 = this.Resolve<global::A<global::A<global::A<global::A<global::A<global::System.Int32>>>>>>(a: _0_6);
                        try
                        {
                            _0_4 = this.Resolve<global::A<global::A<global::A<global::A<global::System.Int32>>>>>(a: _0_5);
                            try
                            {
                                _0_3 = this.Resolve<global::A<global::A<global::A<global::System.Int32>>>>(a: _0_4);
                                try
                                {
                                    _0_2 = this.Resolve<global::A<global::A<global::System.Int32>>>(a: _0_3);
                                    try
                                    {
                                        _0_1 = this.Resolve<global::A<global::System.Int32>>(a: _0_2);
                                        try
                                        {
                                            _0_0 = this.Resolve<global::System.Int32>(a: _0_1);
                                        }
                                        catch
                                        {
                                            global::StrongInject.Helpers.Dispose(_0_1);
                                            throw;
                                        }
                                    }
                                    catch
                                    {
                                        global::StrongInject.Helpers.Dispose(_0_2);
                                        throw;
                                    }
                                }
                                catch
                                {
                                    global::StrongInject.Helpers.Dispose(_0_3);
                                    throw;
                                }
                            }
                            catch
                            {
                                global::StrongInject.Helpers.Dispose(_0_4);
                                throw;
                            }
                        }
                        catch
                        {
                            global::StrongInject.Helpers.Dispose(_0_5);
                            throw;
                        }
                    }
                    catch
                    {
                        global::StrongInject.Helpers.Dispose(_0_6);
                        throw;
                    }
                }
                catch
                {
                    global::StrongInject.Helpers.Dispose(_0_7);
                    throw;
                }
            }
            catch
            {
                global::StrongInject.Helpers.Dispose(_0_8);
                throw;
            }
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_9);
            throw;
        }

        return new global::StrongInject.Owned<global::System.Int32>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            global::StrongInject.Helpers.Dispose(_0_2);
            global::StrongInject.Helpers.Dispose(_0_3);
            global::StrongInject.Helpers.Dispose(_0_4);
            global::StrongInject.Helpers.Dispose(_0_5);
            global::StrongInject.Helpers.Dispose(_0_6);
            global::StrongInject.Helpers.Dispose(_0_7);
            global::StrongInject.Helpers.Dispose(_0_8);
            global::StrongInject.Helpers.Dispose(_0_9);
        });
    }
}");
        }

        [Fact]
        public void ErrorIfTypeParametersCantMatch()
        {
            string userSource = @"
using StrongInject;
using System.Collections.Generic;

public partial class Container<T> : IContainer<List<(string, int, object)>>
{
    [Factory] List<(T1, T2, T1)> Resolve<T1, T2>() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (5,22): Error SI0102: Error while resolving dependencies for 'System.Collections.Generic.List<(string, int, object)>': We have no source for instance of type 'System.Collections.Generic.List<(string, int, object)>'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container<T>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::System.Collections.Generic.List<(global::System.String, global::System.Int32, global::System.Object)>>.Run<TResult, TParam>(global::System.Func<global::System.Collections.Generic.List<(global::System.String, global::System.Int32, global::System.Object)>, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Collections.Generic.List<(global::System.String, global::System.Int32, global::System.Object)>> global::StrongInject.IContainer<global::System.Collections.Generic.List<(global::System.String, global::System.Int32, global::System.Object)>>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestNewConstraint()
        {
            string userSource = @"
using StrongInject;

public partial class Container1 : IContainer<A>, IContainer<A?>, IContainer<B>, IContainer<C>, IContainer<D>, IContainer<E>, IContainer<F>, IContainer<int>, IContainer<string>
{
    [Factory] T Resolve<T>() where T : new() => default;
}

public partial class Container2<T1> : IContainer<T1> where T1 : new()
{
    [Factory] T Resolve<T>() where T : new() => default;
}

public partial class Container3<T1> : IContainer<T1>
{
    [Factory] T Resolve<T>() where T : new() => default;
}

public struct A { public A(int i){} }
public class B {}
public abstract class C {}
public class D { public D() {} }
public class E { internal E() {} }
public class F { public F(int i) {} }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'C': We have no source for instance of type 'C'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'C': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'C' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'E': We have no source for instance of type 'E'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'E': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'E' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'F': We have no source for instance of type 'F'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'F': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'F' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'string': We have no source for instance of type 'string'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'string': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'string' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (14,22): Error SI0102: Error while resolving dependencies for 'T1': We have no source for instance of type 'T1'
                // Container3
                new DiagnosticResult("SI0102", @"Container3", DiagnosticSeverity.Error).WithLocation(14, 22),
                // (14,22): Warning SI1106: Warning while resolving dependencies for 'T1': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T1' as the required type arguments do not satisfy the generic constraints.
                // Container3
                new DiagnosticResult("SI1106", @"Container3", DiagnosticSeverity.Warning).WithLocation(14, 22));
            Assert.Equal(3, generated.Length);
            var ordered = generated.OrderBy(x => x).ToArray();
            ordered[0].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container1
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
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A?>.Run<TResult, TParam>(global::System.Func<global::A?, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A? _0_0;
        _0_0 = this.Resolve<global::A?>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A?> global::StrongInject.IContainer<global::A?>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A? _0_0;
        _0_0 = this.Resolve<global::A?>();
        return new global::StrongInject.Owned<global::A?>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::B _0_0;
        _0_0 = this.Resolve<global::B>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::B _0_0;
        _0_0 = this.Resolve<global::B>();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::D>.Run<TResult, TParam>(global::System.Func<global::D, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::D _0_0;
        _0_0 = this.Resolve<global::D>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::D> global::StrongInject.IContainer<global::D>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::D _0_0;
        _0_0 = this.Resolve<global::D>();
        return new global::StrongInject.Owned<global::D>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::E>.Run<TResult, TParam>(global::System.Func<global::E, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::E> global::StrongInject.IContainer<global::E>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::F>.Run<TResult, TParam>(global::System.Func<global::F, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::F> global::StrongInject.IContainer<global::F>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::System.Int32>.Run<TResult, TParam>(global::System.Func<global::System.Int32, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::System.Int32 _0_0;
        _0_0 = this.Resolve<global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Int32> global::StrongInject.IContainer<global::System.Int32>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::System.Int32 _0_0;
        _0_0 = this.Resolve<global::System.Int32>();
        return new global::StrongInject.Owned<global::System.Int32>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::System.String>.Run<TResult, TParam>(global::System.Func<global::System.String, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.String> global::StrongInject.IContainer<global::System.String>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
            ordered[1].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container2<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        try
        {
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_0);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        try
        {
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_0);
            throw;
        }

        return new global::StrongInject.Owned<T1>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
            ordered[2].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container3<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestStructConstraint()
        {
            string userSource = @"
using StrongInject;

public partial class Container1 : IContainer<A>, IContainer<A?>, IContainer<B>, IContainer<C>, IContainer<System.ValueType>
{
    [Factory] T Resolve<T>() where T : struct => default;
}

public partial class Container2<T1> : IContainer<T1> where T1 : struct
{
    [Factory] T Resolve<T>() where T : struct => default;
}

public partial class Container3<T1> : IContainer<T1>
{
    [Factory] T Resolve<T>() where T : struct => default;
}

public struct A {}
public class B {}
public enum C {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'A?': We have no source for instance of type 'A?'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'A?': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'A?' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'B': We have no source for instance of type 'B'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'B': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'B' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'System.ValueType': We have no source for instance of type 'System.ValueType'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'System.ValueType': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'System.ValueType' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (14,22): Error SI0102: Error while resolving dependencies for 'T1': We have no source for instance of type 'T1'
                // Container3
                new DiagnosticResult("SI0102", @"Container3", DiagnosticSeverity.Error).WithLocation(14, 22),
                // (14,22): Warning SI1106: Warning while resolving dependencies for 'T1': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T1' as the required type arguments do not satisfy the generic constraints.
                // Container3
                new DiagnosticResult("SI1106", @"Container3", DiagnosticSeverity.Warning).WithLocation(14, 22));
            Assert.Equal(3, generated.Length);
            var ordered = generated.OrderBy(x => x).ToArray();
            ordered[0].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container1
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
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A?>.Run<TResult, TParam>(global::System.Func<global::A?, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A?> global::StrongInject.IContainer<global::A?>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::C _0_0;
        _0_0 = this.Resolve<global::C>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::C _0_0;
        _0_0 = this.Resolve<global::C>();
        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::System.ValueType>.Run<TResult, TParam>(global::System.Func<global::System.ValueType, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.ValueType> global::StrongInject.IContainer<global::System.ValueType>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
            ordered[1].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container2<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        return new global::StrongInject.Owned<T1>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
            ordered[2].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container3<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestReferenceConstraint()
        {
            string userSource = @"
using StrongInject;

public partial class Container1 : IContainer<A>, IContainer<A?>, IContainer<B>, IContainer<C>, IContainer<I>, IContainer<System.ValueType>
{
    [Factory] T Resolve<T>() where T : class => default;
}

public partial class Container2<T1> : IContainer<T1> where T1 : class
{
    [Factory] T Resolve<T>() where T : class => default;
}

public partial class Container3<T1> : IContainer<T1>
{
    [Factory] T Resolve<T>() where T : class => default;
}

public struct A {}
public class B {}
public enum C {}
public interface I {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'A': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'A' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'A?': We have no source for instance of type 'A?'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'A?': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'A?' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'C': We have no source for instance of type 'C'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'C': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'C' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (14,22): Error SI0102: Error while resolving dependencies for 'T1': We have no source for instance of type 'T1'
                // Container3
                new DiagnosticResult("SI0102", @"Container3", DiagnosticSeverity.Error).WithLocation(14, 22),
                // (14,22): Warning SI1106: Warning while resolving dependencies for 'T1': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T1' as the required type arguments do not satisfy the generic constraints.
                // Container3
                new DiagnosticResult("SI1106", @"Container3", DiagnosticSeverity.Warning).WithLocation(14, 22));
            Assert.Equal(3, generated.Length);
            var ordered = generated.OrderBy(x => x).ToArray();
            ordered[0].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container1
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

    TResult global::StrongInject.IContainer<global::A?>.Run<TResult, TParam>(global::System.Func<global::A?, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A?> global::StrongInject.IContainer<global::A?>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::B _0_0;
        _0_0 = this.Resolve<global::B>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::B _0_0;
        _0_0 = this.Resolve<global::B>();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::I>.Run<TResult, TParam>(global::System.Func<global::I, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::I _0_0;
        _0_0 = this.Resolve<global::I>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::I> global::StrongInject.IContainer<global::I>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::I _0_0;
        _0_0 = this.Resolve<global::I>();
        return new global::StrongInject.Owned<global::I>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::System.ValueType>.Run<TResult, TParam>(global::System.Func<global::System.ValueType, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::System.ValueType _0_0;
        _0_0 = this.Resolve<global::System.ValueType>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.ValueType> global::StrongInject.IContainer<global::System.ValueType>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::System.ValueType _0_0;
        _0_0 = this.Resolve<global::System.ValueType>();
        return new global::StrongInject.Owned<global::System.ValueType>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
            ordered[1].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container2<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        try
        {
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_0);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        try
        {
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_0);
            throw;
        }

        return new global::StrongInject.Owned<T1>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
            ordered[2].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container3<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestUnmanagedConstraint()
        {
            string userSource = @"
using StrongInject;

public partial class Container1 : IContainer<A>, IContainer<A?>, IContainer<B>, IContainer<C>, IContainer<D>, IContainer<E>, IContainer<F<int>>, IContainer<G<int>>, IContainer<G<D>>, IContainer<System.ValueType>
{
    [Factory] T Resolve<T>() where T : unmanaged => default;
}

public partial class Container2<T1> : IContainer<T1> where T1 : unmanaged
{
    [Factory] T Resolve<T>() where T : unmanaged => default;
}

public partial class Container3<T1> : IContainer<T1>
{
    [Factory] T Resolve<T>() where T : unmanaged => default;
}

public struct A {}
public class B {}
public enum C {}
public struct D { string _s; }
public struct E { int _e; }
public struct F<T> where T : unmanaged { T _t; }
public struct G<T> where T : struct { T _t; }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify(
                // (22,26): Warning CS0169: The field 'D._s' is never used
                // _s
                new DiagnosticResult("CS0169", @"_s", DiagnosticSeverity.Warning).WithLocation(22, 26),
                // (23,23): Warning CS0169: The field 'E._e' is never used
                // _e
                new DiagnosticResult("CS0169", @"_e", DiagnosticSeverity.Warning).WithLocation(23, 23),
                // (24,44): Warning CS0169: The field 'F<T>._t' is never used
                // _t
                new DiagnosticResult("CS0169", @"_t", DiagnosticSeverity.Warning).WithLocation(24, 44),
                // (25,41): Warning CS0169: The field 'G<T>._t' is never used
                // _t
                new DiagnosticResult("CS0169", @"_t", DiagnosticSeverity.Warning).WithLocation(25, 41));
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'A?': We have no source for instance of type 'A?'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'A?': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'A?' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'B': We have no source for instance of type 'B'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'B': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'B' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'D': We have no source for instance of type 'D'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'D': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'D' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'G<D>': We have no source for instance of type 'G<D>'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'G<D>': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'G<D>' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'System.ValueType': We have no source for instance of type 'System.ValueType'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'System.ValueType': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'System.ValueType' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (14,22): Error SI0102: Error while resolving dependencies for 'T1': We have no source for instance of type 'T1'
                // Container3
                new DiagnosticResult("SI0102", @"Container3", DiagnosticSeverity.Error).WithLocation(14, 22),
                // (14,22): Warning SI1106: Warning while resolving dependencies for 'T1': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T1' as the required type arguments do not satisfy the generic constraints.
                // Container3
                new DiagnosticResult("SI1106", @"Container3", DiagnosticSeverity.Warning).WithLocation(14, 22));
            Assert.Equal(3, generated.Length);
            var ordered = generated.OrderBy(x => x).ToArray();
            ordered[0].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container1
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
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::A _0_0;
        _0_0 = this.Resolve<global::A>();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A?>.Run<TResult, TParam>(global::System.Func<global::A?, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A?> global::StrongInject.IContainer<global::A?>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::C _0_0;
        _0_0 = this.Resolve<global::C>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::C _0_0;
        _0_0 = this.Resolve<global::C>();
        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::D>.Run<TResult, TParam>(global::System.Func<global::D, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::D> global::StrongInject.IContainer<global::D>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::E>.Run<TResult, TParam>(global::System.Func<global::E, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::E _0_0;
        _0_0 = this.Resolve<global::E>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::E> global::StrongInject.IContainer<global::E>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::E _0_0;
        _0_0 = this.Resolve<global::E>();
        return new global::StrongInject.Owned<global::E>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::F<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::F<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::F<global::System.Int32> _0_0;
        _0_0 = this.Resolve<global::F<global::System.Int32>>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::F<global::System.Int32>> global::StrongInject.IContainer<global::F<global::System.Int32>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::F<global::System.Int32> _0_0;
        _0_0 = this.Resolve<global::F<global::System.Int32>>();
        return new global::StrongInject.Owned<global::F<global::System.Int32>>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::G<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::G<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::G<global::System.Int32> _0_0;
        _0_0 = this.Resolve<global::G<global::System.Int32>>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::G<global::System.Int32>> global::StrongInject.IContainer<global::G<global::System.Int32>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container1));
        global::G<global::System.Int32> _0_0;
        _0_0 = this.Resolve<global::G<global::System.Int32>>();
        return new global::StrongInject.Owned<global::G<global::System.Int32>>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::G<global::D>>.Run<TResult, TParam>(global::System.Func<global::G<global::D>, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::G<global::D>> global::StrongInject.IContainer<global::G<global::D>>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::System.ValueType>.Run<TResult, TParam>(global::System.Func<global::System.ValueType, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.ValueType> global::StrongInject.IContainer<global::System.ValueType>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
            ordered[1].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container2<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container2<T1>));
        T1 _0_0;
        _0_0 = this.Resolve<T1>();
        return new global::StrongInject.Owned<T1>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
            ordered[2].Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container3<T1>
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<T1>.Run<TResult, TParam>(global::System.Func<T1, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<T1> global::StrongInject.IContainer<T1>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestTypeConstraints1()
        {
            string userSource = @"
using StrongInject;

public partial class Container1<T1> : IContainer<T1> where T1 : A 
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container2<T1> : IContainer<T1> where T1 : B 
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container3<T1, T2> : IContainer<T2> where T1 : A where T2 : T1 
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container4<T1, T2> : IContainer<T2> where T1 : B where T2 : T1
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container5<T1> : IContainer<T1> where T1 : C 
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container6<T1, T2> : IContainer<T2> where T1 : C where T2 : T1 
{
    [Factory] T Resolve<T>() where T : A => default;
}

public partial class Container7 : IContainer<A>, IContainer<B>, IContainer<C>
{
    [Factory] T Resolve<T>() where T : A => default;
}

public class A {}
public class B : A {}
public class C {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (24,22): Error SI0102: Error while resolving dependencies for 'T1': We have no source for instance of type 'T1'
                // Container5
                new DiagnosticResult("SI0102", @"Container5", DiagnosticSeverity.Error).WithLocation(24, 22),
                // (24,22): Warning SI1106: Warning while resolving dependencies for 'T1': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T1' as the required type arguments do not satisfy the generic constraints.
                // Container5
                new DiagnosticResult("SI1106", @"Container5", DiagnosticSeverity.Warning).WithLocation(24, 22),
                // (29,22): Error SI0102: Error while resolving dependencies for 'T2': We have no source for instance of type 'T2'
                // Container6
                new DiagnosticResult("SI0102", @"Container6", DiagnosticSeverity.Error).WithLocation(29, 22),
                // (29,22): Warning SI1106: Warning while resolving dependencies for 'T2': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'T2' as the required type arguments do not satisfy the generic constraints.
                // Container6
                new DiagnosticResult("SI1106", @"Container6", DiagnosticSeverity.Warning).WithLocation(29, 22),
                // (34,22): Error SI0102: Error while resolving dependencies for 'C': We have no source for instance of type 'C'
                // Container7
                new DiagnosticResult("SI0102", @"Container7", DiagnosticSeverity.Error).WithLocation(34, 22),
                // (34,22): Warning SI1106: Warning while resolving dependencies for 'C': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'C' as the required type arguments do not satisfy the generic constraints.
                // Container7
                new DiagnosticResult("SI1106", @"Container7", DiagnosticSeverity.Warning).WithLocation(34, 22));
            Assert.Equal(7, generated.Length);
        }

        [Fact]
        public void TestTypeConstraints2()
        {
            string userSource = @"
using StrongInject;
using System;

public partial class Container : IContainer<Enum>, IContainer<E>, IContainer<E?>, IContainer<S>
{
    [Factory] T Resolve<T>() where T : Enum => default;
}

public enum E {}
public struct S {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (5,22): Error SI0102: Error while resolving dependencies for 'E?': We have no source for instance of type 'E?'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22),
                // (5,22): Warning SI1106: Warning while resolving dependencies for 'E?': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'E?' as the required type arguments do not satisfy the generic constraints.
                // Container
                new DiagnosticResult("SI1106", @"Container", DiagnosticSeverity.Warning).WithLocation(5, 22),
                // (5,22): Error SI0102: Error while resolving dependencies for 'S': We have no source for instance of type 'S'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22),
                // (5,22): Warning SI1106: Warning while resolving dependencies for 'S': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'S' as the required type arguments do not satisfy the generic constraints.
                // Container
                new DiagnosticResult("SI1106", @"Container", DiagnosticSeverity.Warning).WithLocation(5, 22));
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

    TResult global::StrongInject.IContainer<global::System.Enum>.Run<TResult, TParam>(global::System.Func<global::System.Enum, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Enum _0_0;
        _0_0 = this.Resolve<global::System.Enum>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::System.Enum> global::StrongInject.IContainer<global::System.Enum>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Enum _0_0;
        _0_0 = this.Resolve<global::System.Enum>();
        return new global::StrongInject.Owned<global::System.Enum>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::E>.Run<TResult, TParam>(global::System.Func<global::E, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::E _0_0;
        _0_0 = this.Resolve<global::E>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::E> global::StrongInject.IContainer<global::E>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::E _0_0;
        _0_0 = this.Resolve<global::E>();
        return new global::StrongInject.Owned<global::E>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::E?>.Run<TResult, TParam>(global::System.Func<global::E?, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::E?> global::StrongInject.IContainer<global::E?>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::S>.Run<TResult, TParam>(global::System.Func<global::S, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::S> global::StrongInject.IContainer<global::S>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void TestTypeConstraints3()
        {
            string userSource = @"
using StrongInject;

public partial class Container1 : IContainer<I>, IContainer<C>, IContainer<C2>, IContainer<S>, IContainer<S?>, IContainer<S2>
{
    [Factory] T Resolve<T>() where T : I => default;
}

public partial class Container2<T1, T2> : IContainer<T2> where T1 : I where T2 : T1
{
    [Factory] T Resolve<T>() where T : I => default;
}

public partial class Container3<T1, T2> : IContainer<T2> where T1 : C where T2 : T1
{
    [Factory] T Resolve<T>() where T : I => default;
}

public interface I {}
public class C : I {}
public class C2 {}
public struct S : I {}
public struct S2 {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'C2': We have no source for instance of type 'C2'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'C2': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'C2' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'S?': We have no source for instance of type 'S?'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'S?': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'S?' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'S2': We have no source for instance of type 'S2'
                // Container1
                new DiagnosticResult("SI0102", @"Container1", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Warning SI1106: Warning while resolving dependencies for 'S2': factory method 'StrongInject.Generator.FactoryMethod' cannot be used to resolve instance of type 'S2' as the required type arguments do not satisfy the generic constraints.
                // Container1
                new DiagnosticResult("SI1106", @"Container1", DiagnosticSeverity.Warning).WithLocation(4, 22));
        }

        [Fact]
        public void TestTypeConstraints4()
        {
            string userSource = @"
using StrongInject;

public partial class Container<T1> : IContainer<T1>
{
    [Factory] T Resolve<T>() where T : T1 => default;
}

public partial class Container<T1, T2> : IContainer<T2> where T2 : T1
{
    [Factory] T Resolve<T>() where T : T1 => default;
}

public partial class Container<T1, T2, T3> : IContainer<T3> where T2 : T1 where T3 : T2
{
    [Factory] T Resolve<T>() where T : T1 => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
        }

        [Fact]
        public void TestTypeConstraints5()
        {
            string userSource = @"
using StrongInject;

public interface A<out T> {}

public partial class Container1 : IContainer<(A<int>, int)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T2> => default;
}

public partial class Container2 : IContainer<(A<int>, string)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T2> => default;
}

public partial class Container3 : IContainer<(A<string>, object)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T2> => default;
}

public partial class Container4 : IContainer<(A<object>, string)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T2> => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (11,22): Error SI0102: Error while resolving dependencies for '(A<int>, string)': We have no source for instance of type '(A<int>, string)'
                // Container2
                new DiagnosticResult("SI0102", @"Container2", DiagnosticSeverity.Error).WithLocation(11, 22),
                // (11,22): Warning SI1106: Warning while resolving dependencies for '(A<int>, string)': factory method 'Container2.Resolve<T1, T2>()' cannot be used to resolve instance of type '(A<int>, string)' as the required type arguments do not satisfy the generic constraints.
                // Container2
                new DiagnosticResult("SI1106", @"Container2", DiagnosticSeverity.Warning).WithLocation(11, 22),
                // (21,22): Error SI0102: Error while resolving dependencies for '(A<object>, string)': We have no source for instance of type '(A<object>, string)'
                // Container4
                new DiagnosticResult("SI0102", @"Container4", DiagnosticSeverity.Error).WithLocation(21, 22),
                // (21,22): Warning SI1106: Warning while resolving dependencies for '(A<object>, string)': factory method 'Container4.Resolve<T1, T2>()' cannot be used to resolve instance of type '(A<object>, string)' as the required type arguments do not satisfy the generic constraints.
                // Container4
                new DiagnosticResult("SI1106", @"Container4", DiagnosticSeverity.Warning).WithLocation(21, 22));
        }

        [Fact]
        public void TestTypeConstraints6()
        {
            string userSource = @"
using StrongInject;

public class A<T1, T2> {}

public partial class Container1<T> : IContainer<(A<T, A<int, string>[]>, string)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T, A<int, T2>[]> => default;
}

public partial class Container2<T> : IContainer<(A<T, A<int, string>[]>, int)>
{
    [Factory] (T1, T2) Resolve<T1, T2>() where T1 : A<T, A<int, T2>[]> => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (11,22): Error SI0102: Error while resolving dependencies for '(A<T, A<int, string>[]>, int)': We have no source for instance of type '(A<T, A<int, string>[]>, int)'
                // Container2
                new DiagnosticResult("SI0102", @"Container2", DiagnosticSeverity.Error).WithLocation(11, 22),
                // (11,22): Warning SI1106: Warning while resolving dependencies for '(A<T, A<int, string>[]>, int)': factory method 'Container2<T>.Resolve<T1, T2>()' cannot be used to resolve instance of type '(A<T, A<int, string>[]>, int)' as the required type arguments do not satisfy the generic constraints.
                // Container2
                new DiagnosticResult("SI1106", @"Container2", DiagnosticSeverity.Warning).WithLocation(11, 22));
        }

        [Fact]
        public void CanImportGenericFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public class Module
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module.M<global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module.M<global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfGenericFactoryMethodsImportedFromMultipleModules()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module1))]
[RegisterModule(typeof(Module2))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (16,22): Error SI0106: Error while resolving dependencies for '(int, int)': We have multiple sources for instance of type '(int, int)' and no best source. Try adding a single registration for '(int, int)' directly to the container, and moving any existing registrations for '(int, int)' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(16, 22));
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void IgnoresGenericFactoryMethodsWhereConstraintsDontMatch()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() where T : class => default; 
}

public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module1))]
[RegisterModule(typeof(Module2))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module2.M<global::System.Int32, global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module2.M<global::System.Int32, global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ModuleOverridesRegistrationsItImports()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module2))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module2.M<global::System.Int32, global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module2.M<global::System.Int32, global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ModuleDoesNotOverrideRegistrationsItImportsIfConstraintsDontMatch()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() where T1 : class => default; 
}

[RegisterModule(typeof(Module2))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module1.M<global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module1.M<global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ModuleDoesNotOverrideModuleItDoesNotImport()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() where T1 : class => default; 
}

public class Module3
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module2))]
[RegisterModule(typeof(Module3))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify(
                // (22,22): Error SI0106: Error while resolving dependencies for '(int, int)': We have multiple sources for instance of type '(int, int)' and no best source. Try adding a single registration for '(int, int)' directly to the container, and moving any existing registrations for '(int, int)' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(22, 22));
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void NoErrorIfSameModuleImportedTwice()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() where T1 : class => default; 
}

[RegisterModule(typeof(Module1))]
public class Module3
{
    [Factory] public static (T1, T2) M<T1, T2>() where T1 : class => default; 
}

[RegisterModule(typeof(Module2))]
[RegisterModule(typeof(Module3))]
public partial class Container : IContainer<(int, int)>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32), TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module1.M<global::System.Int32>();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_0;
        _0_0 = global::Module1.M<global::System.Int32>();
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ResolveAllDeduplicatesIfSameModuleImportedTwice()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module3
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module2))]
[RegisterModule(typeof(Module3))]
public partial class Container : IContainer<(int, int)[]>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)[]>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32)[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_1;
        (global::System.Int32, global::System.Int32) _0_2;
        (global::System.Int32, global::System.Int32) _0_3;
        (global::System.Int32, global::System.Int32)[] _0_0;
        _0_1 = global::Module2.M<global::System.Int32, global::System.Int32>();
        _0_2 = global::Module1.M<global::System.Int32>();
        _0_3 = global::Module3.M<global::System.Int32, global::System.Int32>();
        _0_0 = new (global::System.Int32, global::System.Int32)[]{((global::System.Int32, global::System.Int32))_0_1, ((global::System.Int32, global::System.Int32))_0_2, ((global::System.Int32, global::System.Int32))_0_3, };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)[]> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_1;
        (global::System.Int32, global::System.Int32) _0_2;
        (global::System.Int32, global::System.Int32) _0_3;
        (global::System.Int32, global::System.Int32)[] _0_0;
        _0_1 = global::Module2.M<global::System.Int32, global::System.Int32>();
        _0_2 = global::Module1.M<global::System.Int32>();
        _0_3 = global::Module3.M<global::System.Int32, global::System.Int32>();
        _0_0 = new (global::System.Int32, global::System.Int32)[]{((global::System.Int32, global::System.Int32))_0_1, ((global::System.Int32, global::System.Int32))_0_2, ((global::System.Int32, global::System.Int32))_0_3, };
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)[]>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AppendGenericAndNonGenericResolutions()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [Factory] public static (T, T) M<T>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module2
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module1))]
public class Module3
{
    [Factory] public static (T1, T2) M<T1, T2>() => default; 
}

[RegisterModule(typeof(Module2))]
[RegisterModule(typeof(Module3))]
public partial class Container : IContainer<(int, int)[]>
{
    [Factory] public (int, int) M() => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            comp.GetDiagnostics().Verify();
            generatorDiagnostics.Verify();
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

    TResult global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)[]>.Run<TResult, TParam>(global::System.Func<(global::System.Int32, global::System.Int32)[], TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_1;
        (global::System.Int32, global::System.Int32) _0_2;
        (global::System.Int32, global::System.Int32) _0_3;
        (global::System.Int32, global::System.Int32) _0_4;
        (global::System.Int32, global::System.Int32)[] _0_0;
        _0_1 = this.M();
        _0_2 = global::Module2.M<global::System.Int32, global::System.Int32>();
        _0_3 = global::Module1.M<global::System.Int32>();
        _0_4 = global::Module3.M<global::System.Int32, global::System.Int32>();
        _0_0 = new (global::System.Int32, global::System.Int32)[]{((global::System.Int32, global::System.Int32))_0_1, ((global::System.Int32, global::System.Int32))_0_2, ((global::System.Int32, global::System.Int32))_0_3, ((global::System.Int32, global::System.Int32))_0_4, };
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<(global::System.Int32, global::System.Int32)[]> global::StrongInject.IContainer<(global::System.Int32, global::System.Int32)[]>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        (global::System.Int32, global::System.Int32) _0_1;
        (global::System.Int32, global::System.Int32) _0_2;
        (global::System.Int32, global::System.Int32) _0_3;
        (global::System.Int32, global::System.Int32) _0_4;
        (global::System.Int32, global::System.Int32)[] _0_0;
        _0_1 = this.M();
        _0_2 = global::Module2.M<global::System.Int32, global::System.Int32>();
        _0_3 = global::Module1.M<global::System.Int32>();
        _0_4 = global::Module3.M<global::System.Int32, global::System.Int32>();
        _0_0 = new (global::System.Int32, global::System.Int32)[]{((global::System.Int32, global::System.Int32))_0_1, ((global::System.Int32, global::System.Int32))_0_2, ((global::System.Int32, global::System.Int32))_0_3, ((global::System.Int32, global::System.Int32))_0_4, };
        return new global::StrongInject.Owned<(global::System.Int32, global::System.Int32)[]>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WrapsTypesInDecoratorsRegisteredByAttributes()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B))]
[RegisterDecorator(typeof(Decorator1), typeof(IA))]
[RegisterDecorator(typeof(Decorator2), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
}

public interface IA {}
public class A : IA {}
public class B {}
public class Decorator1 : IA
{
    public Decorator1(IA a){} 
}
public class Decorator2 : IA
{
    public Decorator2(IA a, B b){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_0;
        _0_3 = new global::A();
        _0_2 = (global::IA)_0_3;
        _0_1 = new global::Decorator1(a: _0_2);
        _0_4 = new global::B();
        _0_0 = new global::Decorator2(a: _0_1, b: _0_4);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_0;
        _0_3 = new global::A();
        _0_2 = (global::IA)_0_3;
        _0_1 = new global::Decorator1(a: _0_2);
        _0_4 = new global::B();
        _0_0 = new global::Decorator2(a: _0_1, b: _0_4);
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WrapsTypesInDecoratorsRegisteredByDecoratorFactory()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B))]
public partial class Container : IAsyncContainer<IA>
{
    [DecoratorFactory]
    public IA Decorator1(IA a) => default;

    [DecoratorFactory]
    public IA Decorator2(IA a, B b) => default;
}

public interface IA {}
public class A : IA {}
public class B {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_0;
        _0_3 = new global::A();
        _0_2 = (global::IA)_0_3;
        _0_1 = this.Decorator1(a: _0_2);
        _0_4 = new global::B();
        _0_0 = this.Decorator2(a: _0_1, b: _0_4);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::B _0_4;
        global::IA _0_0;
        _0_3 = new global::A();
        _0_2 = (global::IA)_0_3;
        _0_1 = this.Decorator1(a: _0_2);
        _0_4 = new global::B();
        _0_0 = this.Decorator2(a: _0_1, b: _0_4);
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfDecoratorsHaveNoParametersOfDecoratedType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B))]
[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
    [DecoratorFactory] IA Decorator(A a) => a;
}

public interface IA {}
public class A : IA {}
public class B {}
public class Decorator : IA
{
    public Decorator(Decorator d){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,2): Error SI0022: Decorator 'Decorator' does not have a constructor parameter of decorated type 'IA'.
                // RegisterDecorator(typeof(Decorator), typeof(IA))
                new DiagnosticResult("SI0022", @"RegisterDecorator(typeof(Decorator), typeof(IA))", DiagnosticSeverity.Error).WithLocation(6, 2),
                // (9,6): Error SI0024: Decorator Factory 'Container.Decorator(A)' does not have a parameter of decorated type 'IA'.
                // DecoratorFactory
                new DiagnosticResult("SI0024", @"DecoratorFactory", DiagnosticSeverity.Error).WithLocation(9, 6));
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::IA _0_0;
        _0_1 = new global::A();
        _0_0 = (global::IA)_0_1;
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::IA _0_0;
        _0_1 = new global::A();
        _0_0 = (global::IA)_0_1;
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfDecoratorsHaveMultipleParametersOfDecoratedType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B))]
[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
    [DecoratorFactory] IA Decorator(IA a, IA b) => a;
}

public interface IA {}
public class A : IA {}
public class B {}
public class Decorator : IA
{
    public Decorator(IA a, B b, IA c){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,2): Error SI0023: Decorator 'Decorator' has multiple constructor parameters of decorated type 'IA'.
                // RegisterDecorator(typeof(Decorator), typeof(IA))
                new DiagnosticResult("SI0023", @"RegisterDecorator(typeof(Decorator), typeof(IA))", DiagnosticSeverity.Error).WithLocation(6, 2),
                // (9,6): Error SI0025: Decorator Factory 'Container.Decorator(IA, IA)' has multiple constructor parameters of decorated type 'IA'.
                // DecoratorFactory
                new DiagnosticResult("SI0025", @"DecoratorFactory", DiagnosticSeverity.Error).WithLocation(9, 6));
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::IA _0_0;
        _0_1 = new global::A();
        _0_0 = (global::IA)_0_1;
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::IA _0_0;
        _0_1 = new global::A();
        _0_0 = (global::IA)_0_1;
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WrapsTypesInDecoratorsRegisteredByGenericDecoratorFactory()
        {
            string userSource = @"
using StrongInject;
using System.Collections.Generic;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B))]
public partial class Container : IAsyncContainer<List<IA>>
{
    [DecoratorFactory]
    public T Decorator1<T>(T t) => t;

    [DecoratorFactory]
    public List<T> Decorator2<T>(List<T> a, B b) => default;

    [DecoratorFactory]
    public T[] Decorator3<T>(T[] a) => default;

    [Factory]
    public List<T> ListFactory<T>(T[] a) => default;
}

public interface IA {}
public class A : IA {}
public class B {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Collections.Generic.List<global::IA>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Collections.Generic.List<global::IA>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_9;
        global::A _0_8;
        global::IA _0_7;
        global::IA _0_6;
        global::IA[] _0_5;
        global::IA[] _0_4;
        global::IA[] _0_3;
        global::System.Collections.Generic.List<global::IA> _0_2;
        global::B _0_11;
        global::B _0_10;
        global::System.Collections.Generic.List<global::IA> _0_1;
        global::System.Collections.Generic.List<global::IA> _0_0;
        _0_9 = new global::A();
        _0_8 = this.Decorator1<global::A>(t: _0_9);
        _0_7 = (global::IA)_0_8;
        _0_6 = this.Decorator1<global::IA>(t: _0_7);
        _0_5 = new global::IA[]{(global::IA)_0_6, };
        _0_4 = this.Decorator3<global::IA>(a: _0_5);
        _0_3 = this.Decorator1<global::IA[]>(t: _0_4);
        _0_2 = this.ListFactory<global::IA>(a: _0_3);
        try
        {
            _0_11 = new global::B();
            _0_10 = this.Decorator1<global::B>(t: _0_11);
            _0_1 = this.Decorator2<global::IA>(a: _0_2, b: _0_10);
            _0_0 = this.Decorator1<global::System.Collections.Generic.List<global::IA>>(t: _0_1);
        }
        catch
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_2);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Collections.Generic.List<global::IA>>> global::StrongInject.IAsyncContainer<global::System.Collections.Generic.List<global::IA>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_9;
        global::A _0_8;
        global::IA _0_7;
        global::IA _0_6;
        global::IA[] _0_5;
        global::IA[] _0_4;
        global::IA[] _0_3;
        global::System.Collections.Generic.List<global::IA> _0_2;
        global::B _0_11;
        global::B _0_10;
        global::System.Collections.Generic.List<global::IA> _0_1;
        global::System.Collections.Generic.List<global::IA> _0_0;
        _0_9 = new global::A();
        _0_8 = this.Decorator1<global::A>(t: _0_9);
        _0_7 = (global::IA)_0_8;
        _0_6 = this.Decorator1<global::IA>(t: _0_7);
        _0_5 = new global::IA[]{(global::IA)_0_6, };
        _0_4 = this.Decorator3<global::IA>(a: _0_5);
        _0_3 = this.Decorator1<global::IA[]>(t: _0_4);
        _0_2 = this.ListFactory<global::IA>(a: _0_3);
        try
        {
            _0_11 = new global::B();
            _0_10 = this.Decorator1<global::B>(t: _0_11);
            _0_1 = this.Decorator2<global::IA>(a: _0_2, b: _0_10);
            _0_0 = this.Decorator1<global::System.Collections.Generic.List<global::IA>>(t: _0_1);
        }
        catch
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_2);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::System.Collections.Generic.List<global::IA>>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_2);
        });
    }
}");
        }

        [Fact]
        public void DoesNotDecorateDelegateParameters()
        {
            string userSource = @"
using StrongInject;
using System;

[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<Func<IA, IA>>
{
}

public interface IA {}
public class Decorator : IA
{
    public Decorator(IA a){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1104: Warning while resolving dependencies for 'System.Func<IA, IA>': Return type 'IA' of delegate 'System.Func<IA, IA>' is provided as a parameter to the delegate and so will be returned unchanged.
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Func<global::IA, global::IA>>.RunAsync<TResult, TParam>(global::System.Func<global::System.Func<global::IA, global::IA>, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::IA, global::IA> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Func<global::IA, global::IA>>> global::StrongInject.IAsyncContainer<global::System.Func<global::IA, global::IA>>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::IA, global::IA> _0_0;
        _0_0 = (param0_0) =>
        {
            return param0_0;
        };
        return new global::StrongInject.AsyncOwned<global::System.Func<global::IA, global::IA>>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DecoratesInstanceFieldOrProperties()
        {
            string userSource = @"
using StrongInject;

[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
    [Instance] IA _ia;
}

public interface IA {}
public class Decorator : IA
{
    public Decorator(IA a){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (7,19): Warning CS0649: Field 'Container._ia' is never assigned to, and will always have its default value null
                // _ia
                new DiagnosticResult("CS0649", @"_ia", DiagnosticSeverity.Warning).WithLocation(7, 19));
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

    private global::IA _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::IA GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::IA _0_1;
            global::IA _0_0;
            _0_1 = this._ia;
            _0_0 = new global::Decorator(a: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DecoratesSingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject;

[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
    [Factory(Scope.SingleInstance)] IA GetIA() => default;
}

public interface IA {}
public class Decorator : IA
{
    public Decorator(IA a){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    private global::IA _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::IA _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private global::IA GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        this._lock1.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::IA _0_0;
            _0_0 = this.GetIA();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
                await global::StrongInject.Helpers.DisposeAsync(_0_0);
            };
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private global::IA GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::IA _0_1;
            global::IA _0_0;
            _0_1 = GetSingleInstanceField1();
            _0_0 = new global::Decorator(a: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::IA _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DeduplicatesMultipleRegistrationsOfSameDecorator()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [DecoratorFactory] public static IA Decorator(IA a) => a;
    [DecoratorFactory] public static T Decorator<T>(T a) => a;
}

[RegisterModule(typeof(Module1))]
public class Module2
{
}

[RegisterModule(typeof(Module1))]
[RegisterModule(typeof(Module2))]
[Register(typeof(A), typeof(IA))]
[RegisterDecorator(typeof(Decorator), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
}

public interface IA {}
public class A : IA {}
public class Decorator : IA
{
    public Decorator(IA a){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_5;
        global::A _0_4;
        global::IA _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_5 = new global::A();
        _0_4 = global::Module1.Decorator<global::A>(a: _0_5);
        _0_3 = (global::IA)_0_4;
        _0_2 = global::Module1.Decorator(a: _0_3);
        _0_1 = new global::Decorator(a: _0_2);
        _0_0 = global::Module1.Decorator<global::IA>(a: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_5;
        global::A _0_4;
        global::IA _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_5 = new global::A();
        _0_4 = global::Module1.Decorator<global::A>(a: _0_5);
        _0_3 = (global::IA)_0_4;
        _0_2 = global::Module1.Decorator(a: _0_3);
        _0_1 = new global::Decorator(a: _0_2);
        _0_0 = global::Module1.Decorator<global::IA>(a: _0_1);
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void WarnOnNonStaticPublidDecoratorInModule()
        {
            string userSource = @"
using StrongInject;

public class Module1
{
    [DecoratorFactory] static int Decorator(int a) => a;
    [DecoratorFactory] public T Decorator<T>(T a) => a;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,6): Warning SI1002: Factory method 'Module1.Decorator(int)' is not either public and static, or protected, and containing module 'Module1' is not a container, so will be ignored.
                // DecoratorFactory
                new DiagnosticResult("SI1002", @"DecoratorFactory", DiagnosticSeverity.Warning).WithLocation(6, 6),
                // (7,6): Warning SI1002: Factory method 'Module1.Decorator<T>(T)' is not static, and containing module 'Module1' is not a container, so will be ignored.
                // DecoratorFactory
                new DiagnosticResult("SI1002", @"DecoratorFactory", DiagnosticSeverity.Warning).WithLocation(7, 6));
            comp.GetDiagnostics().Verify();
            Assert.Empty(generated);
        }

        [Fact]
        public void ErrorIfDecoratorParametersAreNotAvailable()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
public partial class Container : IAsyncContainer<IA>
{
    [DecoratorFactory] IA Decorator(IA a, B b) => a;
}

public interface IA {}
public class A : IA {}
public class B {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (5,22): Error SI0102: Error while resolving dependencies for 'IA': We have no source for instance of type 'B'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void DisposesDecoratorsRegisteredByAttributes()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B), typeof(IB))]
[RegisterDecorator(typeof(DecoratorA), typeof(IA))]
[RegisterDecorator(typeof(DecoratorB), typeof(IB))]
public partial class Container : IAsyncContainer<IA>, IContainer<IB>
{
}

public interface IA {}
public interface IB : IDisposable {}
public class A : IA {}
public class B : IB { public void Dispose(){} }
public class DecoratorA : IA, IAsyncDisposable
{
    public DecoratorA(IA a){}
    public ValueTask DisposeAsync() => default;
}
public class DecoratorB : IB
{
    public DecoratorB(IB b){}
    public void Dispose(){}
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_0 = new global::DecoratorA(a: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_0 = new global::DecoratorA(a: _0_1);
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::IB>.Run<TResult, TParam>(global::System.Func<global::IB, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IB _0_1;
        global::IB _0_0;
        _0_2 = new global::B();
        try
        {
            _0_1 = (global::IB)_0_2;
            _0_0 = new global::DecoratorB(b: _0_1);
        }
        catch
        {
            ((global::System.IDisposable)_0_2).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)_0_2).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::IB> global::StrongInject.IContainer<global::IB>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IB _0_1;
        global::IB _0_0;
        _0_2 = new global::B();
        try
        {
            _0_1 = (global::IB)_0_2;
            _0_0 = new global::DecoratorB(b: _0_1);
        }
        catch
        {
            ((global::System.IDisposable)_0_2).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::IB>(_0_0, () =>
        {
            ((global::System.IDisposable)_0_2).Dispose();
        });
    }
}");
        }

        [Fact]
        public void InitializeDecoratorsRegisteredByAttributes()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B), typeof(IB))]
[RegisterDecorator(typeof(DecoratorA), typeof(IA))]
[RegisterDecorator(typeof(DecoratorB), typeof(IB))]
public partial class Container : IAsyncContainer<IA>, IContainer<IB>
{
}

public interface IA {}
public interface IB {}
public class A : IA {}
public class B : IB {}
public class DecoratorA : IA, IRequiresAsyncInitialization
{
    public DecoratorA(IA a){}
    public ValueTask InitializeAsync() => default;
}
public class DecoratorB : IB, IRequiresInitialization
{
    public DecoratorB(IB b){}
    public void Initialize(){}
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_0 = new global::DecoratorA(a: _0_1);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_2 = new global::A();
        _0_1 = (global::IA)_0_2;
        _0_0 = new global::DecoratorA(a: _0_1);
        await ((global::StrongInject.IRequiresAsyncInitialization)_0_0).InitializeAsync();
        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::IB>.Run<TResult, TParam>(global::System.Func<global::IB, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IB _0_1;
        global::IB _0_0;
        _0_2 = new global::B();
        _0_1 = (global::IB)_0_2;
        _0_0 = new global::DecoratorB(b: _0_1);
        ((global::StrongInject.IRequiresInitialization)_0_0).Initialize();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::IB> global::StrongInject.IContainer<global::IB>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_2;
        global::IB _0_1;
        global::IB _0_0;
        _0_2 = new global::B();
        _0_1 = (global::IB)_0_2;
        _0_0 = new global::DecoratorB(b: _0_1);
        ((global::StrongInject.IRequiresInitialization)_0_0).Initialize();
        return new global::StrongInject.Owned<global::IB>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void TestAsyncDecorators()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [DecoratorFactory]
    public ValueTask<A> Decorator(A a) => default;
}

public class A{}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_1;
        global::A _0_0;
        _0_1 = new global::A();
        _0_0 = await this.Decorator(a: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_1;
        global::A _0_0;
        _0_1 = new global::A();
        _0_0 = await this.Decorator(a: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void TestAsyncGenericDecorators()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
public partial class Container : IAsyncContainer<A>
{
    [DecoratorFactory]
    public async ValueTask<T> Decorator<T>(T t) where T : INeedsInitialization { await t.Initialize(); return t; }
}

public interface INeedsInitialization { ValueTask Initialize(); }
public class A : INeedsInitialization { public ValueTask Initialize() => default; }";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_1;
        global::A _0_0;
        _0_1 = new global::A();
        _0_0 = await this.Decorator<global::A>(t: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::A _0_1;
        global::A _0_0;
        _0_1 = new global::A();
        _0_0 = await this.Decorator<global::A>(t: _0_1);
        return new global::StrongInject.AsyncOwned<global::A>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void DisposesDecoratorsWithDisposeOptionsButNotThoseWithDefaultOptions()
        {
            string userSource = @"
using StrongInject;
using System;

[Register(typeof(A), typeof(IA))]
[RegisterDecorator(typeof(Decorator1), typeof(IA), DecoratorOptions.Dispose)]
[RegisterDecorator(typeof(Decorator2), typeof(IA), DecoratorOptions.Default)]
public partial class Container : IAsyncContainer<IA>
{
    [DecoratorFactory(DecoratorOptions.Dispose)] IA Decorator1(IA a) => a;
    [DecoratorFactory(DecoratorOptions.Dispose)] T Decorator1<T>(T t) => t;
    [DecoratorFactory(DecoratorOptions.Default)] IA Decorator2(IA a) => a;
    [DecoratorFactory(DecoratorOptions.Default)] T Decorator2<T>(T t) => t;
}

public interface IA  {}
public class A : IA {}
public class Decorator1 : IA, IDisposable
{
    public Decorator1(IA a){} 
    public void Dispose(){} 
}
public class Decorator2 : IA, IDisposable
{
    public Decorator2(IA a){} 
    public void Dispose(){} 
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::IA>.RunAsync<TResult, TParam>(global::System.Func<global::IA, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_9;
        global::A _0_8;
        global::A _0_7;
        global::IA _0_6;
        global::IA _0_5;
        global::IA _0_4;
        global::IA _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_9 = new global::A();
        _0_8 = this.Decorator1<global::A>(t: _0_9);
        try
        {
            _0_7 = this.Decorator2<global::A>(t: _0_8);
            _0_6 = (global::IA)_0_7;
            _0_5 = new global::Decorator1(a: _0_6);
            try
            {
                _0_4 = new global::Decorator2(a: _0_5);
                _0_3 = this.Decorator1(a: _0_4);
                try
                {
                    _0_2 = this.Decorator2(a: _0_3);
                    _0_1 = this.Decorator1<global::IA>(t: _0_2);
                    try
                    {
                        _0_0 = this.Decorator2<global::IA>(t: _0_1);
                    }
                    catch
                    {
                        await global::StrongInject.Helpers.DisposeAsync(_0_1);
                        throw;
                    }
                }
                catch
                {
                    await global::StrongInject.Helpers.DisposeAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                ((global::System.IDisposable)_0_5).Dispose();
                throw;
            }
        }
        catch
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_8);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_1);
            await global::StrongInject.Helpers.DisposeAsync(_0_3);
            ((global::System.IDisposable)_0_5).Dispose();
            await global::StrongInject.Helpers.DisposeAsync(_0_8);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::IA>> global::StrongInject.IAsyncContainer<global::IA>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_9;
        global::A _0_8;
        global::A _0_7;
        global::IA _0_6;
        global::IA _0_5;
        global::IA _0_4;
        global::IA _0_3;
        global::IA _0_2;
        global::IA _0_1;
        global::IA _0_0;
        _0_9 = new global::A();
        _0_8 = this.Decorator1<global::A>(t: _0_9);
        try
        {
            _0_7 = this.Decorator2<global::A>(t: _0_8);
            _0_6 = (global::IA)_0_7;
            _0_5 = new global::Decorator1(a: _0_6);
            try
            {
                _0_4 = new global::Decorator2(a: _0_5);
                _0_3 = this.Decorator1(a: _0_4);
                try
                {
                    _0_2 = this.Decorator2(a: _0_3);
                    _0_1 = this.Decorator1<global::IA>(t: _0_2);
                    try
                    {
                        _0_0 = this.Decorator2<global::IA>(t: _0_1);
                    }
                    catch
                    {
                        await global::StrongInject.Helpers.DisposeAsync(_0_1);
                        throw;
                    }
                }
                catch
                {
                    await global::StrongInject.Helpers.DisposeAsync(_0_3);
                    throw;
                }
            }
            catch
            {
                ((global::System.IDisposable)_0_5).Dispose();
                throw;
            }
        }
        catch
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_8);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::IA>(_0_0, async () =>
        {
            await global::StrongInject.Helpers.DisposeAsync(_0_1);
            await global::StrongInject.Helpers.DisposeAsync(_0_3);
            ((global::System.IDisposable)_0_5).Dispose();
            await global::StrongInject.Helpers.DisposeAsync(_0_8);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsImplementedInterfacesIsRegisteredAsImplementedInterfacesButNotAsFactoriesOrBaseClasses()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<B>, IContainer<A>, IContainer<I3>, IContainer<I2>, IContainer<I1>, IContainer<IFactory<int>>, IContainer<int>
{
    [Instance(Options.AsImplementedInterfaces)] B _b;
}

interface I1 {}
interface I2 {}
interface I3 : I2 {}
public class A : I1 {}
public class B : A, I3, IFactory<int> {
    public int Create() => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'A'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'int': We have no source for instance of type 'int'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
            comp.GetDiagnostics().Verify(
                // (6,51): Warning CS0649: Field 'Container._b' is never assigned to, and will always have its default value null
                // _b
                new DiagnosticResult("CS0649", @"_b", DiagnosticSeverity.Warning).WithLocation(6, 51));
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

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_0;
        _0_0 = this._b;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_0;
        _0_0 = this._b;
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    TResult global::StrongInject.IContainer<global::I3>.Run<TResult, TParam>(global::System.Func<global::I3, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I3 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I3)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::I3> global::StrongInject.IContainer<global::I3>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I3 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I3)_0_1;
        return new global::StrongInject.Owned<global::I3>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::I2>.Run<TResult, TParam>(global::System.Func<global::I2, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I2 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I2)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::I2> global::StrongInject.IContainer<global::I2>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I2 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I2)_0_1;
        return new global::StrongInject.Owned<global::I2>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::I1>.Run<TResult, TParam>(global::System.Func<global::I1, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I1 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I1)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::I1> global::StrongInject.IContainer<global::I1>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::I1 _0_0;
        _0_1 = this._b;
        _0_0 = (global::I1)_0_1;
        return new global::StrongInject.Owned<global::I1>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_0;
        _0_1 = this._b;
        _0_0 = (global::StrongInject.IFactory<global::System.Int32>)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::System.Int32>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_0;
        _0_1 = this._b;
        _0_0 = (global::StrongInject.IFactory<global::System.Int32>)_0_1;
        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::System.Int32>>(_0_0, () =>
        {
        });
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
        public void InstanceWithAsBaseClassesIsRegisteredAsBaseClassesButNotAsImplementedInterfacesOrFactories()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<C>, IContainer<B>, IContainer<A>, IContainer<IFactory<int>>, IContainer<int>
{
    [Instance(Options.AsBaseClasses)] C _c;
}

public class A {}
public class B : A {}
public class C : B, IFactory<int> {
    public int Create() => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'StrongInject.IFactory<int>': We have no source for instance of type 'StrongInject.IFactory<int>'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (4,22): Error SI0102: Error while resolving dependencies for 'int': We have no source for instance of type 'int'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
            comp.GetDiagnostics().Verify(
                // (6,41): Warning CS0649: Field 'Container._c' is never assigned to, and will always have its default value null
                // _c
                new DiagnosticResult("CS0649", @"_c", DiagnosticSeverity.Warning).WithLocation(6, 41));
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

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_0;
        _0_0 = this._c;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_0;
        _0_0 = this._c;
        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_1;
        global::B _0_0;
        _0_1 = this._c;
        _0_0 = (global::B)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_1;
        global::B _0_0;
        _0_1 = this._c;
        _0_0 = (global::B)_0_1;
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_1;
        global::A _0_0;
        _0_1 = this._c;
        _0_0 = (global::A)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_1;
        global::A _0_0;
        _0_1 = this._c;
        _0_0 = (global::A)_0_1;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::System.Int32>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Resolve()
    {
        throw new global::System.NotImplementedException();
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
        public void InstanceWithAsBaseClassesIsNotRegisteredAsObject()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<object>
{
    [Instance(Options.AsBaseClasses)] A _a;
}

public class A {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'object': We have no source for instance of type 'object'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
            comp.GetDiagnostics().Verify(
                // (6,41): Warning CS0169: The field 'Container._a' is never used
                // _a
                new DiagnosticResult("CS0169", @"_a", DiagnosticSeverity.Warning).WithLocation(6, 41));
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

    TResult global::StrongInject.IContainer<global::System.Object>.Run<TResult, TParam>(global::System.Func<global::System.Object, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Object> global::StrongInject.IContainer<global::System.Object>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void InstanceWithUseAsFactoryIsRegisteredAsFactoriesButFactoryTargetIsntUsedAsFactory()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<IFactory<IFactory<int>>>, IContainer<IFactory<int>>, IContainer<int>
{
    [Instance(Options.UseAsFactory)] IFactory<IFactory<int>> _fac;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (4,22): Error SI0102: Error while resolving dependencies for 'int': We have no source for instance of type 'int'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(4, 22));
            comp.GetDiagnostics().Verify(
                // (6,62): Warning CS0649: Field 'Container._fac' is never assigned to, and will always have its default value null
                // _fac
                new DiagnosticResult("CS0649", @"_fac", DiagnosticSeverity.Warning).WithLocation(6, 62));
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

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>> _0_0;
        _0_0 = this._fac;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>> _0_0;
        _0_0 = this._fac;
        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>>>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>> _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_0;
        _0_1 = this._fac;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::System.Int32>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::System.Int32>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::StrongInject.IFactory<global::System.Int32>> _0_1;
        global::StrongInject.IFactory<global::System.Int32> _0_0;
        _0_1 = this._fac;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::System.Int32>>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
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
        public void InstanceWithAsEverythingPossibleRegistersEverythingForAllFactoryTargetsRecursively()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>
{
    [Instance(Options.AsEverythingPossible)] A _a;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (5,22): Error SI0103: Error while resolving dependencies for 'D': 'C' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
            comp.GetDiagnostics().Verify(
                // (7,48): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 48));
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        });
    }

    TResult global::StrongInject.IContainer<global::D>.Run<TResult, TParam>(global::System.Func<global::D, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::D> global::StrongInject.IContainer<global::D>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleDoesNotStackOverflowOnRecursion1()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<A>, IContainer<IFactory<A>>
{
    [Instance(Options.AsEverythingPossible)] A _a;
}

public class A : IFactory<A> { public A Create() => default; }";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (6,48): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(6, 48));
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
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::A>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::StrongInject.IFactory<global::A> _0_0;
        _0_1 = this._a;
        _0_0 = (global::StrongInject.IFactory<global::A>)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::A>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_1;
        global::StrongInject.IFactory<global::A> _0_0;
        _0_1 = this._a;
        _0_0 = (global::StrongInject.IFactory<global::A>)_0_1;
        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::A>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleDoesNotStackOverflowOnRecursion2()
        {
            string userSource = @"
using StrongInject;

public partial class Container : IContainer<A<int>>, IContainer<IFactory<A<A<int>>>>, IContainer<A<A<int>>>, IContainer<IFactory<A<A<A<int>>>>>, IContainer<A<A<A<int>>>>
{
    [Instance(Options.AsEverythingPossible)] A<int> _a;
}

public class A<T> : IFactory<A<A<T>>> { public A<A<T>> Create() => default; }";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (6,53): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(6, 53));
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

    TResult global::StrongInject.IContainer<global::A<global::System.Int32>>.Run<TResult, TParam>(global::System.Func<global::A<global::System.Int32>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A<global::System.Int32>> global::StrongInject.IContainer<global::A<global::System.Int32>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A<global::System.Int32>>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_1;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_0;
        _0_1 = this._a;
        _0_0 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_1;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_0;
        _0_1 = this._a;
        _0_0 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_1;
        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::A<global::A<global::System.Int32>>>.Run<TResult, TParam>(global::System.Func<global::A<global::A<global::System.Int32>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_2;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_1;
        global::A<global::A<global::System.Int32>> _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::A<global::A<global::System.Int32>>> global::StrongInject.IContainer<global::A<global::A<global::System.Int32>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_2;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_1;
        global::A<global::A<global::System.Int32>> _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::A<global::A<global::System.Int32>>>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_3;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_2;
        global::A<global::A<global::System.Int32>> _0_1;
        global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>> global::StrongInject.IContainer<global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_3;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_2;
        global::A<global::A<global::System.Int32>> _0_1;
        global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }

    TResult global::StrongInject.IContainer<global::A<global::A<global::A<global::System.Int32>>>>.Run<TResult, TParam>(global::System.Func<global::A<global::A<global::A<global::System.Int32>>>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_4;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_3;
        global::A<global::A<global::System.Int32>> _0_2;
        global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>> _0_1;
        global::A<global::A<global::A<global::System.Int32>>> _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>)_0_2;
            _0_0 = _0_1.Create();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
            _0_3.Release(_0_2);
        }

        return result;
    }

    global::StrongInject.Owned<global::A<global::A<global::A<global::System.Int32>>>> global::StrongInject.IContainer<global::A<global::A<global::A<global::System.Int32>>>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A<global::System.Int32> _0_4;
        global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>> _0_3;
        global::A<global::A<global::System.Int32>> _0_2;
        global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>> _0_1;
        global::A<global::A<global::A<global::System.Int32>>> _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::A<global::A<global::System.Int32>>>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IFactory<global::A<global::A<global::A<global::System.Int32>>>>)_0_2;
            _0_0 = _0_1.Create();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.Owned<global::A<global::A<global::A<global::System.Int32>>>>(_0_0, () =>
        {
            _0_1.Release(_0_0);
            _0_3.Release(_0_2);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleCanBeDecorated()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>
{
    [Instance(Options.AsEverythingPossible)] A _a;
    [DecoratorFactory] T M<T>(T t) => t;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (5,22): Error SI0103: Error while resolving dependencies for 'D': 'C' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(5, 22));
            comp.GetDiagnostics().Verify(
                // (7,48): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 48));
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
            global::A _0_1;
            global::A _0_0;
            _0_1 = this._a;
            _0_0 = this.M<global::A>(t: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    private global::StrongInject.IFactory<global::B> _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private global::StrongInject.IFactory<global::B> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        this._lock1.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::A _0_2;
            global::StrongInject.IFactory<global::B> _0_1;
            global::StrongInject.IFactory<global::B> _0_0;
            _0_2 = GetSingleInstanceField0();
            _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
            _0_0 = this.M<global::StrongInject.IFactory<global::B>>(t: _0_1);
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
            };
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::B _0_0;
        _0_2 = GetSingleInstanceField1();
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = this.M<global::B>(t: _0_1);
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::B _0_0;
        _0_2 = GetSingleInstanceField1();
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = this.M<global::B>(t: _0_1);
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::StrongInject.IAsyncFactory<global::C> _0_2;
        global::C _0_1;
        global::C _0_0;
        _0_6 = GetSingleInstanceField1();
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = this.M<global::B>(t: _0_5);
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_3);
            _0_1 = await _0_2.CreateAsync();
            try
            {
                _0_0 = this.M<global::C>(t: _0_1);
            }
            catch
            {
                await _0_2.ReleaseAsync(_0_1);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_2.ReleaseAsync(_0_1);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::StrongInject.IAsyncFactory<global::C> _0_2;
        global::C _0_1;
        global::C _0_0;
        _0_6 = GetSingleInstanceField1();
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = this.M<global::B>(t: _0_5);
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_3);
            _0_1 = await _0_2.CreateAsync();
            try
            {
                _0_0 = this.M<global::C>(t: _0_1);
            }
            catch
            {
                await _0_2.ReleaseAsync(_0_1);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
            await _0_2.ReleaseAsync(_0_1);
            _0_6.Release(_0_5);
        });
    }

    TResult global::StrongInject.IContainer<global::D>.Run<TResult, TParam>(global::System.Func<global::D, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::D> global::StrongInject.IContainer<global::D>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_12;
        global::B _0_11;
        global::B _0_10;
        global::StrongInject.IAsyncFactory<global::C> _0_9;
        global::StrongInject.IAsyncFactory<global::C> _0_8;
        global::C _0_7;
        global::C _0_6;
        global::StrongInject.IFactory<global::D> _0_5;
        global::StrongInject.IFactory<global::D> _0_4;
        global::D _0_3;
        global::D _0_2;
        global::E _0_1;
        global::E _0_0;
        _0_12 = GetSingleInstanceField1();
        _0_11 = _0_12.Create();
        try
        {
            _0_10 = this.M<global::B>(t: _0_11);
            _0_9 = (global::StrongInject.IAsyncFactory<global::C>)_0_10;
            _0_8 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_9);
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_6 = this.M<global::C>(t: _0_7);
                _0_5 = (global::StrongInject.IFactory<global::D>)_0_6;
                _0_4 = this.M<global::StrongInject.IFactory<global::D>>(t: _0_5);
                _0_3 = _0_4.Create();
                try
                {
                    _0_2 = this.M<global::D>(t: _0_3);
                    _0_1 = (global::E)_0_2;
                    _0_0 = this.M<global::E>(t: _0_1);
                }
                catch
                {
                    _0_4.Release(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            _0_12.Release(_0_11);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_4.Release(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            _0_12.Release(_0_11);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_12;
        global::B _0_11;
        global::B _0_10;
        global::StrongInject.IAsyncFactory<global::C> _0_9;
        global::StrongInject.IAsyncFactory<global::C> _0_8;
        global::C _0_7;
        global::C _0_6;
        global::StrongInject.IFactory<global::D> _0_5;
        global::StrongInject.IFactory<global::D> _0_4;
        global::D _0_3;
        global::D _0_2;
        global::E _0_1;
        global::E _0_0;
        _0_12 = GetSingleInstanceField1();
        _0_11 = _0_12.Create();
        try
        {
            _0_10 = this.M<global::B>(t: _0_11);
            _0_9 = (global::StrongInject.IAsyncFactory<global::C>)_0_10;
            _0_8 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_9);
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_6 = this.M<global::C>(t: _0_7);
                _0_5 = (global::StrongInject.IFactory<global::D>)_0_6;
                _0_4 = this.M<global::StrongInject.IFactory<global::D>>(t: _0_5);
                _0_3 = _0_4.Create();
                try
                {
                    _0_2 = this.M<global::D>(t: _0_3);
                    _0_1 = (global::E)_0_2;
                    _0_0 = this.M<global::E>(t: _0_1);
                }
                catch
                {
                    _0_4.Release(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            _0_12.Release(_0_11);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
            _0_4.Release(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            _0_12.Release(_0_11);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_12;
        global::B _0_11;
        global::B _0_10;
        global::StrongInject.IAsyncFactory<global::C> _0_9;
        global::StrongInject.IAsyncFactory<global::C> _0_8;
        global::C _0_7;
        global::C _0_6;
        global::StrongInject.IFactory<global::D> _0_5;
        global::StrongInject.IFactory<global::D> _0_4;
        global::D _0_3;
        global::D _0_2;
        global::I _0_1;
        global::I _0_0;
        _0_12 = GetSingleInstanceField1();
        _0_11 = _0_12.Create();
        try
        {
            _0_10 = this.M<global::B>(t: _0_11);
            _0_9 = (global::StrongInject.IAsyncFactory<global::C>)_0_10;
            _0_8 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_9);
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_6 = this.M<global::C>(t: _0_7);
                _0_5 = (global::StrongInject.IFactory<global::D>)_0_6;
                _0_4 = this.M<global::StrongInject.IFactory<global::D>>(t: _0_5);
                _0_3 = _0_4.Create();
                try
                {
                    _0_2 = this.M<global::D>(t: _0_3);
                    _0_1 = (global::I)_0_2;
                    _0_0 = this.M<global::I>(t: _0_1);
                }
                catch
                {
                    _0_4.Release(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            _0_12.Release(_0_11);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_4.Release(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            _0_12.Release(_0_11);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_12;
        global::B _0_11;
        global::B _0_10;
        global::StrongInject.IAsyncFactory<global::C> _0_9;
        global::StrongInject.IAsyncFactory<global::C> _0_8;
        global::C _0_7;
        global::C _0_6;
        global::StrongInject.IFactory<global::D> _0_5;
        global::StrongInject.IFactory<global::D> _0_4;
        global::D _0_3;
        global::D _0_2;
        global::I _0_1;
        global::I _0_0;
        _0_12 = GetSingleInstanceField1();
        _0_11 = _0_12.Create();
        try
        {
            _0_10 = this.M<global::B>(t: _0_11);
            _0_9 = (global::StrongInject.IAsyncFactory<global::C>)_0_10;
            _0_8 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_9);
            _0_7 = await _0_8.CreateAsync();
            try
            {
                _0_6 = this.M<global::C>(t: _0_7);
                _0_5 = (global::StrongInject.IFactory<global::D>)_0_6;
                _0_4 = this.M<global::StrongInject.IFactory<global::D>>(t: _0_5);
                _0_3 = _0_4.Create();
                try
                {
                    _0_2 = this.M<global::D>(t: _0_3);
                    _0_1 = (global::I)_0_2;
                    _0_0 = this.M<global::I>(t: _0_1);
                }
                catch
                {
                    _0_4.Release(_0_3);
                    throw;
                }
            }
            catch
            {
                await _0_8.ReleaseAsync(_0_7);
                throw;
            }
        }
        catch
        {
            _0_12.Release(_0_11);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
            _0_4.Release(_0_3);
            await _0_8.ReleaseAsync(_0_7);
            _0_12.Release(_0_11);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_4;
        global::B _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_4 = GetSingleInstanceField1();
        _0_3 = _0_4.Create();
        try
        {
            _0_2 = this.M<global::B>(t: _0_3);
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_1);
        }
        catch
        {
            _0_4.Release(_0_3);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_4.Release(_0_3);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.IFactory<global::B> _0_4;
        global::B _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_4 = GetSingleInstanceField1();
        _0_3 = _0_4.Create();
        try
        {
            _0_2 = this.M<global::B>(t: _0_3);
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = this.M<global::StrongInject.IAsyncFactory<global::C>>(t: _0_1);
        }
        catch
        {
            _0_4.Release(_0_3);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
            _0_4.Release(_0_3);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleAndDoNotDecorateIsNotDecorated()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>
{
    [Instance(Options.AsEverythingPossible | Options.DoNotDecorate)] A _a;
    [DecoratorFactory] T M<T>(T t) => t;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (7,72): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 72));
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::D>(_0_0, async () =>
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleAndFactoryTargetScopeShouldBeInstancePerResolutionUsesCorrectScope()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>
{
    [Instance(Options.AsEverythingPossible | Options.FactoryTargetScopeShouldBeInstancePerResolution)] A _a;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (7,106): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 106));
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::D>(_0_0, async () =>
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleAndFactoryTargetScopeShouldBeSingleInstanceUsesCorrectScope()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>
{
    [Instance(Options.AsEverythingPossible | Options.FactoryTargetScopeShouldBeSingleInstance)] A _a;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (7,99): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 99));
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
        await this._lock2.WaitAsync();
        try
        {
            await (this._disposeAction2?.Invoke() ?? default);
        }
        finally
        {
            this._lock2.Release();
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

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
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
            global::A _0_2;
            global::StrongInject.IFactory<global::B> _0_1;
            global::B _0_0;
            _0_2 = this._a;
            _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
            _0_0 = _0_1.Create();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
                _0_1.Release(_0_0);
            };
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
        global::B _0_0;
        _0_0 = GetSingleInstanceField0();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_0;
        _0_0 = GetSingleInstanceField0();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
        });
    }

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
            global::B _0_2;
            global::StrongInject.IAsyncFactory<global::C> _0_1;
            global::C _0_0;
            _0_2 = GetSingleInstanceField0();
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
                await _0_1.ReleaseAsync(_0_0);
            };
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
        global::C _0_0;
        _0_0 = await GetSingleInstanceField1();
        TResult result;
        try
        {
            result = await func(_0_0, param);
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
        global::C _0_0;
        _0_0 = await GetSingleInstanceField1();
        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
        });
    }

    private global::D _singleInstanceField2;
    private global::System.Threading.SemaphoreSlim _lock2 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction2;
    private async global::System.Threading.Tasks.ValueTask<global::D> GetSingleInstanceField2()
    {
        if (!object.ReferenceEquals(_singleInstanceField2, null))
            return _singleInstanceField2;
        await this._lock2.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::C _0_2;
            global::StrongInject.IFactory<global::D> _0_1;
            global::D _0_0;
            _0_2 = await GetSingleInstanceField1();
            _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
            _0_0 = _0_1.Create();
            this._singleInstanceField2 = _0_0;
            this._disposeAction2 = async () =>
            {
                _0_1.Release(_0_0);
            };
        }
        finally
        {
            this._lock2.Release();
        }

        return _singleInstanceField2;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_0;
        _0_0 = await GetSingleInstanceField2();
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_0;
        _0_0 = await GetSingleInstanceField2();
        return new global::StrongInject.AsyncOwned<global::D>(_0_0, async () =>
        {
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_1;
        global::E _0_0;
        _0_1 = await GetSingleInstanceField2();
        _0_0 = (global::E)_0_1;
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_1;
        global::E _0_0;
        _0_1 = await GetSingleInstanceField2();
        _0_0 = (global::E)_0_1;
        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_1;
        global::I _0_0;
        _0_1 = await GetSingleInstanceField2();
        _0_0 = (global::I)_0_1;
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::D _0_1;
        global::I _0_0;
        _0_1 = await GetSingleInstanceField2();
        _0_0 = (global::I)_0_1;
        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InstanceWithAsEverythingPossibleAndFactoryTargetScopeShouldBeInstancePerDependencyUsesCorrectScope()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

public partial class Container : IContainer<A>, IContainer<B>, IAsyncContainer<C>, IAsyncContainer<D>, IAsyncContainer<E>, IAsyncContainer<I>, IContainer<IAsyncFactory<C>>, IAsyncContainer<int>
{
    [Instance(Options.AsEverythingPossible | Options.FactoryTargetScopeShouldBeInstancePerDependency)] A _a;
    [Factory] int  M(D d1, D d2) => 42;
}

public class A : IFactory<B> { public B Create() => default; }
public class B : IAsyncFactory<C> { public ValueTask<C> CreateAsync() => default; }
public class C : IFactory<D> { public D Create() => default; }
public class D : E {}
public class E : I {}
public interface I {}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify(
                // (7,106): Warning CS0649: Field 'Container._a' is never assigned to, and will always have its default value null
                // _a
                new DiagnosticResult("CS0649", @"_a", DiagnosticSeverity.Warning).WithLocation(7, 106));
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

    void global::System.IDisposable.Dispose()
    {
        throw new global::StrongInject.StrongInjectException(""This container requires async disposal"");
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_0;
        _0_0 = this._a;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = this._a;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }

    TResult global::StrongInject.IContainer<global::B>.Run<TResult, TParam>(global::System.Func<global::B, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::B> global::StrongInject.IContainer<global::B>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::StrongInject.IFactory<global::B> _0_1;
        global::B _0_0;
        _0_2 = this._a;
        _0_1 = (global::StrongInject.IFactory<global::B>)_0_2;
        _0_0 = _0_1.Create();
        return new global::StrongInject.Owned<global::B>(_0_0, () =>
        {
            _0_1.Release(_0_0);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::C>.RunAsync<TResult, TParam>(global::System.Func<global::C, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::C>> global::StrongInject.IAsyncContainer<global::C>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_4;
        global::StrongInject.IFactory<global::B> _0_3;
        global::B _0_2;
        global::StrongInject.IAsyncFactory<global::C> _0_1;
        global::C _0_0;
        _0_4 = this._a;
        _0_3 = (global::StrongInject.IFactory<global::B>)_0_4;
        _0_2 = _0_3.Create();
        try
        {
            _0_1 = (global::StrongInject.IAsyncFactory<global::C>)_0_2;
            _0_0 = await _0_1.CreateAsync();
        }
        catch
        {
            _0_3.Release(_0_2);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::C>(_0_0, async () =>
        {
            await _0_1.ReleaseAsync(_0_0);
            _0_3.Release(_0_2);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::D>.RunAsync<TResult, TParam>(global::System.Func<global::D, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::D>> global::StrongInject.IAsyncContainer<global::D>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_6;
        global::StrongInject.IFactory<global::B> _0_5;
        global::B _0_4;
        global::StrongInject.IAsyncFactory<global::C> _0_3;
        global::C _0_2;
        global::StrongInject.IFactory<global::D> _0_1;
        global::D _0_0;
        _0_6 = this._a;
        _0_5 = (global::StrongInject.IFactory<global::B>)_0_6;
        _0_4 = _0_5.Create();
        try
        {
            _0_3 = (global::StrongInject.IAsyncFactory<global::C>)_0_4;
            _0_2 = await _0_3.CreateAsync();
            try
            {
                _0_1 = (global::StrongInject.IFactory<global::D>)_0_2;
                _0_0 = _0_1.Create();
            }
            catch
            {
                await _0_3.ReleaseAsync(_0_2);
                throw;
            }
        }
        catch
        {
            _0_5.Release(_0_4);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::D>(_0_0, async () =>
        {
            _0_1.Release(_0_0);
            await _0_3.ReleaseAsync(_0_2);
            _0_5.Release(_0_4);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::E>.RunAsync<TResult, TParam>(global::System.Func<global::E, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::E>> global::StrongInject.IAsyncContainer<global::E>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::E _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::E)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::E>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::I>.RunAsync<TResult, TParam>(global::System.Func<global::I, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::I>> global::StrongInject.IAsyncContainer<global::I>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::I _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_0 = (global::I)_0_1;
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::I>(_0_0, async () =>
        {
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }

    TResult global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.IAsyncFactory<global::C>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            _0_2.Release(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>> global::StrongInject.IContainer<global::StrongInject.IAsyncFactory<global::C>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_3;
        global::StrongInject.IFactory<global::B> _0_2;
        global::B _0_1;
        global::StrongInject.IAsyncFactory<global::C> _0_0;
        _0_3 = this._a;
        _0_2 = (global::StrongInject.IFactory<global::B>)_0_3;
        _0_1 = _0_2.Create();
        try
        {
            _0_0 = (global::StrongInject.IAsyncFactory<global::C>)_0_1;
        }
        catch
        {
            _0_2.Release(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::StrongInject.IAsyncFactory<global::C>>(_0_0, () =>
        {
            _0_2.Release(_0_1);
        });
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Int32>.RunAsync<TResult, TParam>(global::System.Func<global::System.Int32, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::B _0_12;
        global::StrongInject.IAsyncFactory<global::C> _0_11;
        global::C _0_10;
        global::StrongInject.IFactory<global::D> _0_9;
        global::D _0_8;
        global::System.Int32 _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_12 = _0_6.Create();
                    try
                    {
                        _0_11 = (global::StrongInject.IAsyncFactory<global::C>)_0_12;
                        _0_10 = await _0_11.CreateAsync();
                        try
                        {
                            _0_9 = (global::StrongInject.IFactory<global::D>)_0_10;
                            _0_8 = _0_9.Create();
                            try
                            {
                                _0_0 = this.M(d1: _0_1, d2: _0_8);
                            }
                            catch
                            {
                                _0_9.Release(_0_8);
                                throw;
                            }
                        }
                        catch
                        {
                            await _0_11.ReleaseAsync(_0_10);
                            throw;
                        }
                    }
                    catch
                    {
                        _0_6.Release(_0_12);
                        throw;
                    }
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
            _0_9.Release(_0_8);
            await _0_11.ReleaseAsync(_0_10);
            _0_6.Release(_0_12);
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Int32>> global::StrongInject.IAsyncContainer<global::System.Int32>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_7;
        global::StrongInject.IFactory<global::B> _0_6;
        global::B _0_5;
        global::StrongInject.IAsyncFactory<global::C> _0_4;
        global::C _0_3;
        global::StrongInject.IFactory<global::D> _0_2;
        global::D _0_1;
        global::B _0_12;
        global::StrongInject.IAsyncFactory<global::C> _0_11;
        global::C _0_10;
        global::StrongInject.IFactory<global::D> _0_9;
        global::D _0_8;
        global::System.Int32 _0_0;
        _0_7 = this._a;
        _0_6 = (global::StrongInject.IFactory<global::B>)_0_7;
        _0_5 = _0_6.Create();
        try
        {
            _0_4 = (global::StrongInject.IAsyncFactory<global::C>)_0_5;
            _0_3 = await _0_4.CreateAsync();
            try
            {
                _0_2 = (global::StrongInject.IFactory<global::D>)_0_3;
                _0_1 = _0_2.Create();
                try
                {
                    _0_12 = _0_6.Create();
                    try
                    {
                        _0_11 = (global::StrongInject.IAsyncFactory<global::C>)_0_12;
                        _0_10 = await _0_11.CreateAsync();
                        try
                        {
                            _0_9 = (global::StrongInject.IFactory<global::D>)_0_10;
                            _0_8 = _0_9.Create();
                            try
                            {
                                _0_0 = this.M(d1: _0_1, d2: _0_8);
                            }
                            catch
                            {
                                _0_9.Release(_0_8);
                                throw;
                            }
                        }
                        catch
                        {
                            await _0_11.ReleaseAsync(_0_10);
                            throw;
                        }
                    }
                    catch
                    {
                        _0_6.Release(_0_12);
                        throw;
                    }
                }
                catch
                {
                    _0_2.Release(_0_1);
                    throw;
                }
            }
            catch
            {
                await _0_4.ReleaseAsync(_0_3);
                throw;
            }
        }
        catch
        {
            _0_6.Release(_0_5);
            throw;
        }

        return new global::StrongInject.AsyncOwned<global::System.Int32>(_0_0, async () =>
        {
            _0_9.Release(_0_8);
            await _0_11.ReleaseAsync(_0_10);
            _0_6.Release(_0_12);
            _0_2.Release(_0_1);
            await _0_4.ReleaseAsync(_0_3);
            _0_6.Release(_0_5);
        });
    }
}");
        }

        [Fact]
        public void ImportsRegistrationsFromBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}

[Register(typeof(A))]
public class Module {}

public partial class Container : Module, IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ImportsRegistrationsFromBaseBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}

[Register(typeof(A))]
public class Module {}

public class InBetween : Module {}

public partial class Container : InBetween, IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::A _0_0;
        _0_0 = new global::A();
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::A _0_0;
        _0_0 = new global::A();
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void CanOverrideRegistrationImportedFromBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B : A {}
[Register(typeof(A))]
public class Module {}

[Register(typeof(B), typeof(A))]
public partial class Container : Module, IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = (global::A)_0_1;
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::A _0_0;
        _0_1 = new global::B();
        _0_0 = (global::A)_0_1;
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void ErrorIfRegistrationFromBaseClassConflictsWithThatFromImportedModule()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B : A {}

[Register(typeof(A))]
public class ModuleA {}

[Register(typeof(B), typeof(A))]
public class ModuleB{}

[RegisterModule(typeof(ModuleB))]
public partial class Container : ModuleA, IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (14,22): Error SI0106: Error while resolving dependencies for 'A': We have multiple sources for instance of type 'A' and no best source. Try adding a single registration for 'A' directly to the container, and moving any existing registrations for 'A' on the container to an imported module.
                // Container
                new DiagnosticResult("SI0106", @"Container", DiagnosticSeverity.Error).WithLocation(14, 22));
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
        public void ImportsProtectedInstanceFieldInstancePropertyFactoryAndDecoratorFromBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B {}
public class C {}

public class Module
{
    [Instance] protected A A = new A();
    [Instance] protected internal B B => new B();
    [Factory] protected C CreateC(A a, B b) => new C();
    [DecoratorFactory] protected C DecorateC(C c) => c;
}

public partial class Container : Module, IContainer<C>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = this.A;
        _0_3 = this.B;
        _0_1 = this.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = this.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = this.A;
        _0_3 = this.B;
        _0_1 = this.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = this.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        });
    }
}");
        }

        [Fact]
        public void ImportsPublicStaticInstanceFieldInstancePropertyFactoryAndDecoratorFromBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B {}
public class C {}

public class Module
{
    [Instance] public static A A = new A();
    [Instance] public static B B => new B();
    [Factory] public static C CreateC(A a, B b) => new C();
    [DecoratorFactory] public static C DecorateC(C c) => c;
}

public partial class Container : Module, IContainer<C>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = global::Module.A;
        _0_3 = global::Module.B;
        _0_1 = global::Module.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = global::Module.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = global::Module.A;
        _0_3 = global::Module.B;
        _0_1 = global::Module.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = global::Module.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        });
    }
}");
        }

        [Fact]
        public void ImportsProtectedStaticInstanceFieldInstancePropertyFactoryAndDecoratorFromBaseClass()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B {}
public class C {}

public class Module
{
    [Instance] protected internal static A A = new A();
    [Instance] protected static B B => new B();
    [Factory] protected internal static C CreateC(A a, B b) => new C();
    [DecoratorFactory] protected static C DecorateC(C c) => c;
}

public partial class Container : Module, IContainer<C>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    TResult global::StrongInject.IContainer<global::C>.Run<TResult, TParam>(global::System.Func<global::C, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = global::Module.A;
        _0_3 = global::Module.B;
        _0_1 = global::Module.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = global::Module.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        }

        return result;
    }

    global::StrongInject.Owned<global::C> global::StrongInject.IContainer<global::C>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::A _0_2;
        global::B _0_3;
        global::C _0_1;
        global::C _0_0;
        _0_2 = global::Module.A;
        _0_3 = global::Module.B;
        _0_1 = global::Module.CreateC(a: _0_2, b: _0_3);
        try
        {
            _0_0 = global::Module.DecorateC(c: _0_1);
        }
        catch
        {
            global::StrongInject.Helpers.Dispose(_0_1);
            throw;
        }

        return new global::StrongInject.Owned<global::C>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_1);
        });
    }
}");
        }

        [Fact]
        public void WarningIfInstanceFieldInstancePropertyFactoryAndDecoratorAreNotPublicStaticOrProtected()
        {
            string userSource = @"
using StrongInject;

public class A {}

public class Module
{
    [Instance] public A A1 = new A();
    [Instance] static A A2 => new A();
    [Factory] private protected A CreateA() => new A();
    [DecoratorFactory] internal A DecorateA(A a) => a;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (8,6): Warning SI1004: Instance field 'Module.A1' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(8, 6),
                // (9,6): Warning SI1004: Instance property 'Module.A2' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Instance
                new DiagnosticResult("SI1004", @"Instance", DiagnosticSeverity.Warning).WithLocation(9, 6),
                // (10,6): Warning SI1002: Factory method 'Module.CreateA()' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // Factory
                new DiagnosticResult("SI1002", @"Factory", DiagnosticSeverity.Warning).WithLocation(10, 6),
                // (11,6): Warning SI1002: Factory method 'Module.DecorateA(A)' is not either public and static, or protected, and containing module 'Module' is not a container, so will be ignored.
                // DecoratorFactory
                new DiagnosticResult("SI1002", @"DecoratorFactory", DiagnosticSeverity.Warning).WithLocation(11, 6));
            comp.GetDiagnostics().Verify();
            Assert.Empty(generated);
        }

        [Fact]
        public void OptionalParametersInTypeConstructor()
        {
            string userSource = @"
using StrongInject;

public class A { public A(B b = null, C c = null, string s  = """", D d = null,  int i = 5){} }
public class B {}
public class C {}
public class D {}

[Register(typeof(A))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<A>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'B' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22),
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'string' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22),
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'int' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22));
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
        global::C _0_1;
        global::D _0_2;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_0 = new global::A(c: _0_1, d: _0_2);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_1;
        global::D _0_2;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_0 = new global::A(c: _0_1, d: _0_2);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void OptionalParametersInDecoratorTypeConstructor()
        {
            string userSource = @"
using StrongInject;

public interface IA {}
public class Impl : IA {}
public class A : IA { public A(IA a, B b = null, C c = null, string s  = """", D d = null,  int i = 5){} }
public class B {}
public class C {}
public class D {}

[Register(typeof(Impl), typeof(IA))]
[RegisterDecorator(typeof(A), typeof(IA))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<IA>
{
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (15,22): Info SI2100: Info about resolving dependencies for 'IA': We have no source for instance of type 'B' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(15, 22),
                // (15,22): Info SI2100: Info about resolving dependencies for 'IA': We have no source for instance of type 'string' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(15, 22),
                // (15,22): Info SI2100: Info about resolving dependencies for 'IA': We have no source for instance of type 'int' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(15, 22));
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

    TResult global::StrongInject.IContainer<global::IA>.Run<TResult, TParam>(global::System.Func<global::IA, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::Impl _0_2;
        global::IA _0_1;
        global::C _0_3;
        global::D _0_4;
        global::IA _0_0;
        _0_2 = new global::Impl();
        _0_1 = (global::IA)_0_2;
        _0_3 = new global::C();
        _0_4 = new global::D();
        _0_0 = new global::A(a: _0_1, c: _0_3, d: _0_4);
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::IA> global::StrongInject.IContainer<global::IA>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::Impl _0_2;
        global::IA _0_1;
        global::C _0_3;
        global::D _0_4;
        global::IA _0_0;
        _0_2 = new global::Impl();
        _0_1 = (global::IA)_0_2;
        _0_3 = new global::C();
        _0_4 = new global::D();
        _0_0 = new global::A(a: _0_1, c: _0_3, d: _0_4);
        return new global::StrongInject.Owned<global::IA>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void OptionalParametersInFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B {}
public class C {}
public class D {}

[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<A>
{
    [Factory] public A CreateA(B b = null, C c = null, string s  = """", D d = null,  int i = 5) => null;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (11,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'B' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(11, 22),
                // (11,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'string' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(11, 22),
                // (11,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'int' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(11, 22));
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
        global::C _0_1;
        global::D _0_2;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_0 = this.CreateA(c: _0_1, d: _0_2);
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::C _0_1;
        global::D _0_2;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_0 = this.CreateA(c: _0_1, d: _0_2);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
            global::StrongInject.Helpers.Dispose(_0_0);
        });
    }
}");
        }

        [Fact]
        public void OptionalParametersInDecoratorFactoryMethod()
        {
            string userSource = @"
using StrongInject;

public class A {}
public class B {}
public class C {}
public class D {}

[Register(typeof(A))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<A>
{
    [DecoratorFactory] public A CreateA(B b = null, C c = null, string s  = """", D d = null,  int i = 5, A a = null) => null;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'B' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22),
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'string' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22),
                // (12,22): Info SI2100: Info about resolving dependencies for 'A': We have no source for instance of type 'int' used in an optional parameter. Using The default value instead.
                // Container
                new DiagnosticResult("SI2100", @"Container", DiagnosticSeverity.Info).WithLocation(12, 22));
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
        global::C _0_1;
        global::D _0_2;
        global::A _0_3;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_3 = new global::A();
        _0_0 = this.CreateA(c: _0_1, d: _0_2, a: _0_3);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::C _0_1;
        global::D _0_2;
        global::A _0_3;
        global::A _0_0;
        _0_1 = new global::C();
        _0_2 = new global::D();
        _0_3 = new global::A();
        _0_0 = this.CreateA(c: _0_1, d: _0_2, a: _0_3);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }
        
        [Fact]
        public void AsyncSingleInstanceCanBeResolvedFromNonAsyncFunc1()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory] string Create(int i) => default;
    [Factory] long Create(Func<string> func) => default;
    [Factory] bool Create(int i, long l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    private global::System.Int32 _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private async global::System.Threading.Tasks.ValueTask<global::System.Int32> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_0;
            _0_0 = await this.Create();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_1;
        global::System.Func<global::System.String> _0_3;
        global::System.Int64 _0_2;
        global::System.Boolean _0_0;
        _0_1 = await GetSingleInstanceField0();
        _0_3 = () =>
        {
            global::System.String _1_0;
            _1_0 = this.Create(i: _0_1);
            return _1_0;
        };
        _0_2 = this.Create(func: _0_3);
        _0_0 = this.Create(i: _0_1, l: _0_2);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_1;
        global::System.Func<global::System.String> _0_3;
        global::System.Int64 _0_2;
        global::System.Boolean _0_0;
        _0_1 = await GetSingleInstanceField0();
        _0_3 = () =>
        {
            global::System.String _1_0;
            _1_0 = this.Create(i: _0_1);
            return _1_0;
        };
        _0_2 = this.Create(func: _0_3);
        _0_0 = this.Create(i: _0_1, l: _0_2);
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncSingleInstanceCanBeResolvedFromNonAsyncFunc2()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory] string Create(int i) => default;
    [Factory] long Create(Func<string> func) => default;
    [Factory] bool Create(long l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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

    private global::System.Int32 _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private async global::System.Threading.Tasks.ValueTask<global::System.Int32> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_0;
            _0_0 = await this.Create();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::System.String _1_0;
            _1_0 = this.Create(i: _0_3);
            return _1_0;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::System.String _1_0;
            _1_0 = this.Create(i: _0_3);
            return _1_0;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncSingleInstanceCanBeResolvedFromNonAsyncFunc3()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory] string Create(Func<int> i) => default;
    [Factory] long Create(Func<string> func) => default;
    [Factory] bool Create(long l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1103: Warning while resolving dependencies for 'bool': Return type 'int' of delegate 'System.Func<int>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
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

    private global::System.Int32 _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private async global::System.Threading.Tasks.ValueTask<global::System.Int32> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_0;
            _0_0 = await this.Create();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::System.Func<global::System.Int32> _1_1;
            global::System.String _1_0;
            _1_1 = () =>
            {
                return _0_3;
            };
            _1_0 = this.Create(i: _1_1);
            return _1_0;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Int32 _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::System.Func<global::System.Int32> _1_1;
            global::System.String _1_0;
            _1_1 = () =>
            {
                return _0_3;
            };
            _1_0 = this.Create(i: _1_1);
            return _1_0;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncSingleInstanceCanBeResolvedFromNonAsyncFunc4()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory(Scope.SingleInstance)] string Create(Func<int> i) => default;
    [Factory] long Create(Func<string> func) => default;
    [Factory] bool Create(long l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1103: Warning while resolving dependencies for 'bool': Return type 'string' of delegate 'System.Func<string>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22),
                // (6,22): Warning SI1103: Warning while resolving dependencies for 'bool': Return type 'int' of delegate 'System.Func<int>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
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

    private global::System.String _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private global::System.Int32 _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction1;
    private async global::System.Threading.Tasks.ValueTask<global::System.Int32> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        await this._lock1.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_0;
            _0_0 = await this.Create();
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = async () =>
            {
            };
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private async global::System.Threading.Tasks.ValueTask<global::System.String> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_2;
            global::System.Func<global::System.Int32> _0_1;
            global::System.String _0_0;
            _0_2 = await GetSingleInstanceField1();
            _0_1 = () =>
            {
                return _0_2;
            };
            _0_0 = this.Create(i: _0_1);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.String _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            return _0_3;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.String _0_3;
        global::System.Func<global::System.String> _0_2;
        global::System.Int64 _0_1;
        global::System.Boolean _0_0;
        _0_3 = await GetSingleInstanceField0();
        _0_2 = () =>
        {
            return _0_3;
        };
        _0_1 = this.Create(func: _0_2);
        _0_0 = this.Create(l: _0_1);
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncSingleInstanceCanBeResolvedFromNonAsyncFunc5()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IAsyncContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory] string Create(Func<int> i) => default;
    [Factory] long Create(Func<string> func) => default;
    [Factory] bool Create(Func<ValueTask<long>> l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1103: Warning while resolving dependencies for 'bool': Return type 'int' of delegate 'System.Func<int>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22));
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

    private global::System.Int32 _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Func<global::System.Threading.Tasks.ValueTask> _disposeAction0;
    private async global::System.Threading.Tasks.ValueTask<global::System.Int32> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        await this._lock0.WaitAsync();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::System.Int32 _0_0;
            _0_0 = await this.Create();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = async () =>
            {
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<TResult> global::StrongInject.IAsyncContainer<global::System.Boolean>.RunAsync<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, global::System.Threading.Tasks.ValueTask<TResult>> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.ValueTask<global::System.Int64>> _0_1;
        global::System.Boolean _0_0;
        _0_1 = async () =>
        {
            global::System.Int32 _1_2;
            global::System.Func<global::System.String> _1_1;
            global::System.Int64 _1_0;
            _1_2 = await GetSingleInstanceField0();
            _1_1 = () =>
            {
                global::System.Func<global::System.Int32> _2_1;
                global::System.String _2_0;
                _2_1 = () =>
                {
                    return _1_2;
                };
                _2_0 = this.Create(i: _2_1);
                return _2_0;
            };
            _1_0 = this.Create(func: _1_1);
            return _1_0;
        };
        _0_0 = this.Create(l: _0_1);
        TResult result;
        try
        {
            result = await func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::System.Boolean>> global::StrongInject.IAsyncContainer<global::System.Boolean>.ResolveAsync()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.ValueTask<global::System.Int64>> _0_1;
        global::System.Boolean _0_0;
        _0_1 = async () =>
        {
            global::System.Int32 _1_2;
            global::System.Func<global::System.String> _1_1;
            global::System.Int64 _1_0;
            _1_2 = await GetSingleInstanceField0();
            _1_1 = () =>
            {
                global::System.Func<global::System.Int32> _2_1;
                global::System.String _2_0;
                _2_1 = () =>
                {
                    return _1_2;
                };
                _2_0 = this.Create(i: _2_1);
                return _2_0;
            };
            _1_0 = this.Create(func: _1_1);
            return _1_0;
        };
        _0_0 = this.Create(l: _0_1);
        return new global::StrongInject.AsyncOwned<global::System.Boolean>(_0_0, async () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncSingleInstanceCannotBeResolvedFromAsyncFuncIfContainerIsNonAsync()
        {
            string userSource = @"
using StrongInject;
using System;
using System.Threading.Tasks;

public partial class Container : IContainer<bool>
{
    [Factory(Scope.SingleInstance)] ValueTask<int> Create() => default;
    [Factory] string Create(Func<int> i) => default;
    [Factory] long Create(string func) => default;
    [Factory] bool Create(long l) => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify(
                // (6,22): Warning SI1103: Warning while resolving dependencies for 'bool': Return type 'int' of delegate 'System.Func<int>' has a single instance scope and so will always have the same value.
                // Container
                new DiagnosticResult("SI1103", @"Container", DiagnosticSeverity.Warning).WithLocation(6, 22),
                // (6,22): Error SI0103: Error while resolving dependencies for 'bool': 'int' can only be resolved asynchronously.
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

    TResult global::StrongInject.IContainer<global::System.Boolean>.Run<TResult, TParam>(global::System.Func<global::System.Boolean, TParam, TResult> func, TParam param)
    {
        throw new global::System.NotImplementedException();
    }

    global::StrongInject.Owned<global::System.Boolean> global::StrongInject.IContainer<global::System.Boolean>.Resolve()
    {
        throw new global::System.NotImplementedException();
    }
}");
        }

        [Fact]
        public void UseDelegateParameterBugInV_1_0_2()
        {
            string userSource = @"
using StrongInject;
using System;

public class BaseViewModel { }
public class ItemsViewModel : BaseViewModel
{
    public ItemsViewModel(
        INavigationService<ItemDetailViewModel> itemDetailNavigationService,
        INavigationService<NewItemViewModel> newItemNavigationService,
        Func<Item, ItemDetailViewModel> createItemDetailViewModel,
        Func<NewItemViewModel> createNewItemViewModel,
        IDataStore<Item> dataStore)
    { }
}

public interface INavigationService
{
}

public interface INavigationService<T> : INavigationService where T : BaseViewModel
{
}

public class NavigationService : INavigationService
{
    public NavigationService(INavigation navigation) { }
}

public class NavigationService<T> : NavigationService, INavigationService<T> where T : BaseViewModel
{
    public NavigationService(INavigation navigation, Func<T, IViewOf<T>> createView) : base(navigation) { }
}

public class ItemDetailViewModel : BaseViewModel
{
    public ItemDetailViewModel(Item item) { }
}

public interface IViewOf<T> where T : BaseViewModel { }

public class ItemDetailPage : IViewOf<ItemDetailViewModel>
{
    public ItemDetailPage(ItemDetailViewModel itemDetailViewModel) { }
}

public class NewItemViewModel : BaseViewModel
{
    public NewItemViewModel(IDataStore<Item> dataStore, INavigationService navigationService) { }
}

public class NewItemPage : IViewOf<NewItemViewModel>
{
    public NewItemPage(NewItemViewModel newItemViewModel) { }
}

public class MockDataStore : IDataStore<Item> { }

public interface IDataStore<T> { }

public class Item { }

public interface INavigation
{
}

[Register(typeof(ItemsViewModel))]
[Register(typeof(NavigationService), Scope.SingleInstance, typeof(INavigationService))]
[Register(typeof(ItemDetailViewModel))]
[Register(typeof(ItemDetailPage), typeof(IViewOf<ItemDetailViewModel>))]
[Register(typeof(NewItemViewModel))]
[Register(typeof(NewItemPage), typeof(IViewOf<NewItemViewModel>))]
[Register(typeof(MockDataStore), Scope.SingleInstance, typeof(IDataStore<Item>))]
public partial class Container : IContainer<ItemsViewModel>
{
    [Factory(Scope.SingleInstance)]
    INavigationService<T> CreateNavigationService<T>(INavigation navigation, Func<T, IViewOf<T>> createView) where T : BaseViewModel
        => new NavigationService<T>(navigation, createView);
    [Instance] INavigation Navigation => default;
}";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
        this._lock3.Wait();
        try
        {
            this._disposeAction3?.Invoke();
        }
        finally
        {
            this._lock3.Release();
        }

        this._lock2.Wait();
        try
        {
            this._disposeAction2?.Invoke();
        }
        finally
        {
            this._lock2.Release();
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

    private global::INavigationService<global::ItemDetailViewModel> _singleInstanceField0;
    private global::System.Threading.SemaphoreSlim _lock0 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction0;
    private global::INavigationService<global::ItemDetailViewModel> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        this._lock0.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::INavigation _0_1;
            global::System.Func<global::ItemDetailViewModel, global::IViewOf<global::ItemDetailViewModel>> _0_2;
            global::INavigationService<global::ItemDetailViewModel> _0_0;
            _0_1 = this.Navigation;
            _0_2 = (param0_0) =>
            {
                global::ItemDetailPage _1_1;
                global::IViewOf<global::ItemDetailViewModel> _1_0;
                _1_1 = new global::ItemDetailPage(itemDetailViewModel: param0_0);
                _1_0 = (global::IViewOf<global::ItemDetailViewModel>)_1_1;
                return _1_0;
            };
            _0_0 = this.CreateNavigationService<global::ItemDetailViewModel>(navigation: _0_1, createView: _0_2);
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
                global::StrongInject.Helpers.Dispose(_0_0);
            };
        }
        finally
        {
            this._lock0.Release();
        }

        return _singleInstanceField0;
    }

    private global::INavigationService<global::NewItemViewModel> _singleInstanceField1;
    private global::System.Threading.SemaphoreSlim _lock1 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction1;
    private global::INavigationService<global::NewItemViewModel> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        this._lock1.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::INavigation _0_1;
            global::System.Func<global::NewItemViewModel, global::IViewOf<global::NewItemViewModel>> _0_2;
            global::INavigationService<global::NewItemViewModel> _0_0;
            _0_1 = this.Navigation;
            _0_2 = (param0_0) =>
            {
                global::NewItemPage _1_1;
                global::IViewOf<global::NewItemViewModel> _1_0;
                _1_1 = new global::NewItemPage(newItemViewModel: param0_0);
                _1_0 = (global::IViewOf<global::NewItemViewModel>)_1_1;
                return _1_0;
            };
            _0_0 = this.CreateNavigationService<global::NewItemViewModel>(navigation: _0_1, createView: _0_2);
            this._singleInstanceField1 = _0_0;
            this._disposeAction1 = () =>
            {
                global::StrongInject.Helpers.Dispose(_0_0);
            };
        }
        finally
        {
            this._lock1.Release();
        }

        return _singleInstanceField1;
    }

    private global::MockDataStore _singleInstanceField2;
    private global::System.Threading.SemaphoreSlim _lock2 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction2;
    private global::MockDataStore GetSingleInstanceField2()
    {
        if (!object.ReferenceEquals(_singleInstanceField2, null))
            return _singleInstanceField2;
        this._lock2.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::MockDataStore _0_0;
            _0_0 = new global::MockDataStore();
            this._singleInstanceField2 = _0_0;
            this._disposeAction2 = () =>
            {
            };
        }
        finally
        {
            this._lock2.Release();
        }

        return _singleInstanceField2;
    }

    private global::NavigationService _singleInstanceField3;
    private global::System.Threading.SemaphoreSlim _lock3 = new global::System.Threading.SemaphoreSlim(1);
    private global::System.Action _disposeAction3;
    private global::NavigationService GetSingleInstanceField3()
    {
        if (!object.ReferenceEquals(_singleInstanceField3, null))
            return _singleInstanceField3;
        this._lock3.Wait();
        try
        {
            if (this.Disposed)
                throw new global::System.ObjectDisposedException(nameof(Container));
            global::INavigation _0_1;
            global::NavigationService _0_0;
            _0_1 = this.Navigation;
            _0_0 = new global::NavigationService(navigation: _0_1);
            this._singleInstanceField3 = _0_0;
            this._disposeAction3 = () =>
            {
            };
        }
        finally
        {
            this._lock3.Release();
        }

        return _singleInstanceField3;
    }

    TResult global::StrongInject.IContainer<global::ItemsViewModel>.Run<TResult, TParam>(global::System.Func<global::ItemsViewModel, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::INavigationService<global::ItemDetailViewModel> _0_1;
        global::INavigationService<global::NewItemViewModel> _0_2;
        global::System.Func<global::Item, global::ItemDetailViewModel> _0_3;
        global::System.Func<global::NewItemViewModel> _0_4;
        global::MockDataStore _0_6;
        global::IDataStore<global::Item> _0_5;
        global::ItemsViewModel _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = GetSingleInstanceField1();
        _0_3 = (param0_0) =>
        {
            global::ItemDetailViewModel _1_0;
            _1_0 = new global::ItemDetailViewModel(item: param0_0);
            return _1_0;
        };
        _0_4 = () =>
        {
            global::MockDataStore _0_2;
            global::IDataStore<global::Item> _1_1;
            global::NavigationService _0_4;
            global::INavigationService _1_3;
            global::NewItemViewModel _1_0;
            _0_2 = GetSingleInstanceField2();
            _1_1 = (global::IDataStore<global::Item>)_0_2;
            _0_4 = GetSingleInstanceField3();
            _1_3 = (global::INavigationService)_0_4;
            _1_0 = new global::NewItemViewModel(dataStore: _1_1, navigationService: _1_3);
            return _1_0;
        };
        _0_6 = GetSingleInstanceField2();
        _0_5 = (global::IDataStore<global::Item>)_0_6;
        _0_0 = new global::ItemsViewModel(itemDetailNavigationService: _0_1, newItemNavigationService: _0_2, createItemDetailViewModel: _0_3, createNewItemViewModel: _0_4, dataStore: _0_5);
        TResult result;
        try
        {
            result = func(_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::ItemsViewModel> global::StrongInject.IContainer<global::ItemsViewModel>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::INavigationService<global::ItemDetailViewModel> _0_1;
        global::INavigationService<global::NewItemViewModel> _0_2;
        global::System.Func<global::Item, global::ItemDetailViewModel> _0_3;
        global::System.Func<global::NewItemViewModel> _0_4;
        global::MockDataStore _0_6;
        global::IDataStore<global::Item> _0_5;
        global::ItemsViewModel _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = GetSingleInstanceField1();
        _0_3 = (param0_0) =>
        {
            global::ItemDetailViewModel _1_0;
            _1_0 = new global::ItemDetailViewModel(item: param0_0);
            return _1_0;
        };
        _0_4 = () =>
        {
            global::MockDataStore _0_2;
            global::IDataStore<global::Item> _1_1;
            global::NavigationService _0_4;
            global::INavigationService _1_3;
            global::NewItemViewModel _1_0;
            _0_2 = GetSingleInstanceField2();
            _1_1 = (global::IDataStore<global::Item>)_0_2;
            _0_4 = GetSingleInstanceField3();
            _1_3 = (global::INavigationService)_0_4;
            _1_0 = new global::NewItemViewModel(dataStore: _1_1, navigationService: _1_3);
            return _1_0;
        };
        _0_6 = GetSingleInstanceField2();
        _0_5 = (global::IDataStore<global::Item>)_0_6;
        _0_0 = new global::ItemsViewModel(itemDetailNavigationService: _0_1, newItemNavigationService: _0_2, createItemDetailViewModel: _0_3, createNewItemViewModel: _0_4, dataStore: _0_5);
        return new global::StrongInject.Owned<global::ItemsViewModel>(_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void RegressionTest_FalseCircularDependencyBug()
        {
            string userSource = @"
using StrongInject;
using System;

[Register(typeof(A))]
[Register(typeof(B), Scope.SingleInstance)]
[Register(typeof(C))]
public partial class Container : IContainer<A>
{
}

public class A { public A(B b, Func<C> c){} }
public class B { }
public class C { public C(B b1, B b2){} }";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
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
            global::B _0_0;
            _0_0 = new global::B();
            this._singleInstanceField0 = _0_0;
            this._disposeAction0 = () =>
            {
            };
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
        global::B _0_1;
        global::System.Func<global::C> _0_2;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::C _1_0;
            _1_0 = new global::C(b1: _0_1, b2: _0_1);
            return _1_0;
        };
        _0_0 = new global::A(b: _0_1, c: _0_2);
        TResult result;
        try
        {
            result = func(_0_0, param);
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
        global::B _0_1;
        global::System.Func<global::C> _0_2;
        global::A _0_0;
        _0_1 = GetSingleInstanceField0();
        _0_2 = () =>
        {
            global::C _1_0;
            _1_0 = new global::C(b1: _0_1, b2: _0_1);
            return _1_0;
        };
        _0_0 = new global::A(b: _0_1, c: _0_2);
        return new global::StrongInject.Owned<global::A>(_0_0, () =>
        {
        });
    }
}");
        }
    }
}
