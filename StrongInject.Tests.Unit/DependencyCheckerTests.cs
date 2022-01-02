using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Generator.Visitors;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public class DependencyCheckerVisitorTests : TestBase
    {
        public DependencyCheckerVisitorTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void NoErrorForCorrectDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void IgnoresErrorsInUnusedDependencies()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<B>
{
}

public class A 
{
    public A(B b, C c, E e){}
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
public class E {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorOnCircularDependency1()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
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
public class C 
{
    public C(B b){}
}
public class D 
{
    public D(C c){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (8,22): Error SI0101: Error while resolving dependencies for 'A': 'B' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(8, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorOnCircularDependency2()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
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
public class C 
{
    public C(C c){}
}
public class D 
{
    public D(C c){}
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (8,22): Error SI0101: Error while resolving dependencies for 'A': 'C' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(8, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorOnCircularDependency3()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
public partial class Container : IContainer<A>
{
}

public class A 
{
    public A(B b, C c, A a){}
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (8,22): Error SI0101: Error while resolving dependencies for 'A': 'A' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container", DiagnosticSeverity.Error).WithLocation(8, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorWhenDependencyNotRegistered()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
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
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'D'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorForAllMissingDependencies1()
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
    public A(B b, C c){}
}
public class B 
{
    public B(C c, D d){}
}
public class C {}
public class D 
{
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'C'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'D'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'C'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
        }

        [Fact]
        public void ErrorForAllMissingDependencies2()
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
    public A(B b, C c){}
}
public class B 
{
    public B(D d, E e){}
}
public class C {}
public class D {}
public class E {}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'D'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'E'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22),
                // (6,22): Error SI0102: Error while resolving dependencies for 'A': We have no source for instance of type 'C'
                // Container
                new DiagnosticResult("SI0102", @"Container", DiagnosticSeverity.Error).WithLocation(6, 22));
            comp.GetDiagnostics().Verify();
        }
    }
}
