using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrongInject.Runtime;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongInject.Generator.Tests.Unit
{
    public class DependencySorterTests : TestBase
    {
        [Fact]
        public void OrdersDependenciesCorrectly()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("A"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => Assert.True(false, x.ToString()), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation());
            Assert.NotNull(sorted);
            sorted.Should().Equal(new[]
            {
                comp.AssertGetTypeByMetadataName("C"),
                comp.AssertGetTypeByMetadataName("D"),
                comp.AssertGetTypeByMetadataName("B"),
                comp.AssertGetTypeByMetadataName("A"),
            });
        }

        [Fact]
        public void IgnoresUnusedTypes()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("B"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => Assert.True(false, x.ToString()), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation());
            Assert.NotNull(sorted);
            sorted.Should().Equal(new[]
            {
                comp.AssertGetTypeByMetadataName("C"),
                comp.AssertGetTypeByMetadataName("D"),
                comp.AssertGetTypeByMetadataName("B"),
            });
        }

        [Fact]
        public void ErrorOnCircularDependency1()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("A"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => diagnostics.Add(x), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation());
            Assert.Null(sorted);
            diagnostics.Verify(
                // (9,14): Error SI0101: Error whilst resolving dependencies for 'A': 'B' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container").WithLocation(9, 14));
        }

        [Fact]
        public void ErrorOnCircularDependency2()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("A"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => diagnostics.Add(x), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation()); Assert.Null(sorted);
            diagnostics.Verify(
                // (9,14): Error SI0101: Error whilst resolving dependencies for 'A': 'C' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container").WithLocation(9, 14));
        }

        [Fact]
        public void ErrorOnCircularDependency3()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("A"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => diagnostics.Add(x), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation());            Assert.Null(sorted);
            diagnostics.Verify(
                // (9,14): Error SI0101: Error whilst resolving dependencies for 'A': 'A' has a circular dependency
                // Container
                new DiagnosticResult("SI0101", @"Container").WithLocation(9, 14));
        }

        [Fact]
        public void ErrorWhenDependencyNotRegistered()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
public class Container
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var sorted = DependencySorter.SortDependencies(comp.AssertGetTypeByMetadataName("A"), registrations.ToDictionary(x => x.Key, x => (InstanceSource)x.Value), x => diagnostics.Add(x), ((ClassDeclarationSyntax)comp.AssertGetTypeByMetadataName("Container").DeclaringSyntaxReferences.First().GetSyntax()).Identifier.GetLocation());
            Assert.Null(sorted);
            diagnostics.Verify(
                // (8,14): Error SI0102: Error whilst resolving dependencies for 'A': We have no source for instance of type 'D'
                // Container
                new DiagnosticResult("SI0102", @"Container").WithLocation(8, 14));
        }
    }
}
