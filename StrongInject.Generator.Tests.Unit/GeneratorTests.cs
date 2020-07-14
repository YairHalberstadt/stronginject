using FluentAssertions;
using Microsoft.CodeAnalysis;
using NuGet.Frameworks;
using StrongInject.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void InstancePerDependencyDependencies()
        {
            string userSource = @"
using StrongInject.Runtime;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IContainer<A>
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
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWihCasts()
        {
            string userSource = @"
using StrongInject.Runtime;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), typeof(C), typeof(IC))]
[Registration(typeof(D))]
public partial class Container : IContainer<A>
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
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _2 = new global::C();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresInitialization
{
    public A(B b, C c){}

    ValueTask IRequiresInitialization.InitializeAsync() => new ValueTask();
}
public class B 
{
    public B(C c, D d){}
}
public class C : IRequiresInitialization { public ValueTask InitializeAsync()  => new ValueTask();  }
public class D : E
{
    public D(C c){}
}

public class E : IRequiresInitialization
{
    ValueTask IRequiresInitialization.InitializeAsync() => new ValueTask();
}
";
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _2 = new global::C();
        await ((global::StrongInject.Runtime.IRequiresInitialization)_2).InitializeAsync();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.Runtime.IRequiresInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        await ((global::StrongInject.Runtime.IRequiresInitialization)_0).InitializeAsync();
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void InstancePerDependencyDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Registration(typeof(A), typeof(IFactory<AFactoryTarget>))]
[Registration(typeof(B), typeof(IFactory<BFactoryTarget>))]
[Registration(typeof(C), typeof(C), typeof(IFactory<CFactoryTarget>))]
[Registration(typeof(D), typeof(IFactory<DFactoryTarget>))]
public partial class Container : IContainer<AFactoryTarget>
{
}

public class A : IFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c){}
    ValueTask<AFactoryTarget> IFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IFactory<BFactoryTarget>
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
}
public class BFactoryTarget {}
public class C : IFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
";
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    async global::System.Threading.Tasks.ValueTask<global::AFactoryTarget> global::StrongInject.Runtime.IContainer<global::AFactoryTarget>.ResolveAsync()
    {
        var _4 = new global::C();
        var _7 = await ((global::StrongInject.Runtime.IFactory<global::CFactoryTarget>)_4).CreateAsync();
        var _6 = new global::D(_7);
        var _5 = await ((global::StrongInject.Runtime.IFactory<global::DFactoryTarget>)_6).CreateAsync();
        var _3 = new global::B((global::C)_4, _5);
        var _2 = await ((global::StrongInject.Runtime.IFactory<global::BFactoryTarget>)_3).CreateAsync();
        var _1 = new global::A(_2, _7);
        var _0 = await ((global::StrongInject.Runtime.IFactory<global::AFactoryTarget>)_1).CreateAsync();
        return (global::AFactoryTarget)_0;
    }
}");
        }

        [Fact]
        public void SingleInstanceDependencies()
        {
            string userSource = @"
using StrongInject.Runtime;

[Registration(typeof(A), Lifetime.SingleInstance)]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D), Lifetime.SingleInstance)]
public partial class Container : IContainer<A>
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
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private global::A _singleInstanceField0;
    private global::D _singleInstanceField1;
    private async System.Threading.Tasks.ValueTask<D> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        var _1 = new global::C();
        var _0 = new global::D((global::C)_1);
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField1, _0, null);
        return _singleInstanceField1;
    }

    private async System.Threading.Tasks.ValueTask<A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        var _2 = new global::C();
        var _3 = await GetSingleInstanceField1();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField0, _0, null);
        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _0 = await GetSingleInstanceField0();
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWihCasts()
        {
            string userSource = @"
using StrongInject.Runtime;

[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C), Lifetime.SingleInstance, typeof(C), typeof(IC))]
[Registration(typeof(D))]
public partial class Container : IContainer<A>
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
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private global::C _singleInstanceField0;
    private async System.Threading.Tasks.ValueTask<C> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        var _0 = new global::C();
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField0, _0, null);
        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _2 = await GetSingleInstanceField0();
        var _3 = new global::D((global::C)_2);
        var _1 = new global::B((global::IC)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::IC)_2);
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithRequiresInitialization()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Registration(typeof(A), Lifetime.SingleInstance)]
[Registration(typeof(B))]
[Registration(typeof(C), Lifetime.SingleInstance)]
[Registration(typeof(D))]
public partial class Container : IContainer<A>
{
}

public class A : IRequiresInitialization
{
    public A(B b, C c){}

    ValueTask IRequiresInitialization.InitializeAsync() => new ValueTask();
}
public class B 
{
    public B(C c, D d){}
}
public class C : IRequiresInitialization { public ValueTask InitializeAsync()  => new ValueTask();  }
public class D : E
{
    public D(C c){}
}

public class E : IRequiresInitialization
{
    ValueTask IRequiresInitialization.InitializeAsync() => new ValueTask();
}
";
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private global::A _singleInstanceField0;
    private global::C _singleInstanceField1;
    private async System.Threading.Tasks.ValueTask<C> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        var _0 = new global::C();
        await ((global::StrongInject.Runtime.IRequiresInitialization)_0).InitializeAsync();
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField1, _0, null);
        return _singleInstanceField1;
    }

    private async System.Threading.Tasks.ValueTask<A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        var _2 = await GetSingleInstanceField1();
        var _3 = new global::D((global::C)_2);
        await ((global::StrongInject.Runtime.IRequiresInitialization)_3).InitializeAsync();
        var _1 = new global::B((global::C)_2, (global::D)_3);
        var _0 = new global::A((global::B)_1, (global::C)_2);
        await ((global::StrongInject.Runtime.IRequiresInitialization)_0).InitializeAsync();
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField0, _0, null);
        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<global::A> global::StrongInject.Runtime.IContainer<global::A>.ResolveAsync()
    {
        var _0 = await GetSingleInstanceField0();
        return (global::A)_0;
    }
}");
        }

        [Fact]
        public void SingleInstanceDependenciesWithFactories()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Registration(typeof(A), Lifetime.SingleInstance, Lifetime.InstancePerDependency, typeof(IFactory<AFactoryTarget>))]
[Registration(typeof(B), Lifetime.SingleInstance, Lifetime.SingleInstance, typeof(IFactory<BFactoryTarget>))]
[Registration(typeof(C), Lifetime.InstancePerDependency, Lifetime.SingleInstance, typeof(C), typeof(IFactory<CFactoryTarget>))]
[Registration(typeof(D), Lifetime.InstancePerDependency, Lifetime.InstancePerDependency, typeof(IFactory<DFactoryTarget>))]
public partial class Container : IContainer<AFactoryTarget>
{
}

public class A : IFactory<AFactoryTarget>
{
    public A(BFactoryTarget b, CFactoryTarget c){}
    ValueTask<AFactoryTarget> IFactory<AFactoryTarget>.CreateAsync() => new ValueTask<AFactoryTarget>(new AFactoryTarget());
}
public class AFactoryTarget {}
public class B : IFactory<BFactoryTarget>
{
    public B(C c, DFactoryTarget d){}
    ValueTask<BFactoryTarget> IFactory<BFactoryTarget>.CreateAsync() => new ValueTask<BFactoryTarget>(new BFactoryTarget());
}
public class BFactoryTarget {}
public class C : IFactory<CFactoryTarget> 
{
    ValueTask<CFactoryTarget> IFactory<CFactoryTarget>.CreateAsync() => new ValueTask<CFactoryTarget>(new CFactoryTarget());
}
public class CFactoryTarget {}
public class D : IFactory<DFactoryTarget>
{
    public D(CFactoryTarget c){}
    ValueTask<DFactoryTarget> IFactory<DFactoryTarget>.CreateAsync() => new ValueTask<DFactoryTarget>(new DFactoryTarget());
}
public class DFactoryTarget {}
";
            var comp = RunGenerator(userSource, out var diagnostics, out var generated, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private global::A _singleInstanceField0;
    private global::BFactoryTarget _singleInstanceField1;
    private global::B _singleInstanceField2;
    private global::CFactoryTarget _singleInstanceField3;
    private async System.Threading.Tasks.ValueTask<CFactoryTarget> GetSingleInstanceField3()
    {
        if (!object.ReferenceEquals(_singleInstanceField3, null))
            return _singleInstanceField3;
        var _1 = new global::C();
        var _0 = await ((global::StrongInject.Runtime.IFactory<global::CFactoryTarget>)_1).CreateAsync();
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField3, _0, null);
        return _singleInstanceField3;
    }

    private async System.Threading.Tasks.ValueTask<B> GetSingleInstanceField2()
    {
        if (!object.ReferenceEquals(_singleInstanceField2, null))
            return _singleInstanceField2;
        var _1 = new global::C();
        var _4 = await GetSingleInstanceField3();
        var _3 = new global::D(_4);
        var _2 = await ((global::StrongInject.Runtime.IFactory<global::DFactoryTarget>)_3).CreateAsync();
        var _0 = new global::B((global::C)_1, _2);
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField2, _0, null);
        return _singleInstanceField2;
    }

    private async System.Threading.Tasks.ValueTask<BFactoryTarget> GetSingleInstanceField1()
    {
        if (!object.ReferenceEquals(_singleInstanceField1, null))
            return _singleInstanceField1;
        var _1 = await GetSingleInstanceField2();
        var _0 = await ((global::StrongInject.Runtime.IFactory<global::BFactoryTarget>)_1).CreateAsync();
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField1, _0, null);
        return _singleInstanceField1;
    }

    private async System.Threading.Tasks.ValueTask<A> GetSingleInstanceField0()
    {
        if (!object.ReferenceEquals(_singleInstanceField0, null))
            return _singleInstanceField0;
        var _1 = await GetSingleInstanceField1();
        var _2 = await GetSingleInstanceField3();
        var _0 = new global::A(_1, _2);
        global::System.Threading.Interlocked.CompareExchange(ref _singleInstanceField0, _0, null);
        return _singleInstanceField0;
    }

    async global::System.Threading.Tasks.ValueTask<global::AFactoryTarget> global::StrongInject.Runtime.IContainer<global::AFactoryTarget>.ResolveAsync()
    {
        var _1 = await GetSingleInstanceField0();
        var _0 = await ((global::StrongInject.Runtime.IFactory<global::AFactoryTarget>)_1).CreateAsync();
        return (global::AFactoryTarget)_0;
    }
}");
        }
    }
}
