using FluentAssertions;
using Microsoft.CodeAnalysis;
using StrongInject.Runtime;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongInject.Generator.Tests.Unit
{
    public class CalculateRegistrationsTests : TestBase
    {
        [Fact]
        public void CalculatesDirectRegistrations()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A), typeof(IA))]
[Registration(typeof(B), Lifetime.SingleInstance, typeof(B), typeof(IB))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IB {}
public interface IB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        lifetime: Lifetime.SingleInstance,
                        castTarget: comp.AssertGetTypeByMetadataName("IB"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        lifetime: Lifetime.SingleInstance,
                        castTarget: comp.AssertGetTypeByMetadataName("B"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CalculatesIndirectRegistrations()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A), typeof(IA))]
[ModuleRegistration(typeof(ModuleA))]
[ModuleRegistration(typeof(ModuleB), typeof(ID))]
public class Container
{
}

[Registration(typeof(B), typeof(IB))]
public class ModuleA
{
}

[Registration(typeof(C), typeof(IC))]
[Registration(typeof(D), Lifetime.SingleInstance, typeof(D), typeof(ID))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IB {}
public interface IB {}
public class C : IC {}
public interface IC {}
public class D : ID {}
public interface ID {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IB"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IC")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("C"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IC"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IC"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("D")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        lifetime: Lifetime.SingleInstance,
                        castTarget: comp.AssertGetTypeByMetadataName("D"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void DirectRegistrationsHasPriorityOverIndirect()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A), typeof(IA))]
[ModuleRegistration(typeof(Module))]
public class Container
{
}

[Registration(typeof(B), typeof(IA))]
public class Module
{
}

public class A : IA {}
public class B : IA {}
public interface IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            var moduleRegistrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Module"), comp, x => Assert.False(true, x.ToString()), default);
            moduleRegistrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });

            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfTwoModulesRegisterSameType()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[ModuleRegistration(typeof(ModuleA))]
[ModuleRegistration(typeof(ModuleB))]
public class Container
{
}

[Registration(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Registration(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (5,2): Error SI0002: 'IA' is registered by both modules 'ModuleA' and 'ModuleB'.
                // ModuleRegistration(typeof(ModuleA))
                new DiagnosticResult("SI0002", @"ModuleRegistration(typeof(ModuleA))").WithLocation(5, 2),
                // (6,2): Error SI0002: 'IA' is registered by both modules 'ModuleA' and 'ModuleB'.
                // ModuleRegistration(typeof(ModuleB))
                new DiagnosticResult("SI0002", @"ModuleRegistration(typeof(ModuleB))").WithLocation(6, 2));
            registrations.Should().HaveCount(1);
            registrations.Should().ContainKey(comp.GetTypeByMetadataName("IA")!);
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne1()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[ModuleRegistration(typeof(ModuleA))]
[ModuleRegistration(typeof(ModuleB), typeof(IA))]
public class Container
{
}

[Registration(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Registration(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne2()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[ModuleRegistration(typeof(ModuleA), typeof(IA))]
[ModuleRegistration(typeof(ModuleB))]
public class Container
{
}

[Registration(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Registration(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfModuleRegistersSameTypeTwice()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A), typeof(IA))]
[Registration(typeof(B), typeof(IA))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (6,2): Error SI0004: Module already contains registration for 'IA'.
                // Registration(typeof(B), typeof(IA))
                new DiagnosticResult("SI0004", @"Registration(typeof(B), typeof(IA))").WithLocation(6, 2));
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfModuleRegistersSameTypeTwiceThroughFactory()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Container]
[Registration(typeof(A), typeof(IA))]
[Registration(typeof(B), typeof(IFactory<IA>))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IFactory<IA> { public ValueTask<IA> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (7,2): Error SI0004: Module already contains registration for 'IA'.
                // Registration(typeof(B), typeof(IFactory<IA>))
                new DiagnosticResult("SI0004", @"Registration(typeof(B), typeof(IFactory<IA>))").WithLocation(7, 2));

            var factoryOfIA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("IA"));
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [factoryOfIA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfIA,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfIA,
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorWhenRegisteringOpenGeneric()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A<>))]
[Registration(typeof(A<int>), typeof(A<>))]
[Registration(typeof(B<>.C))]
public class Container
{
}

public class A<T> {}
public class B<T> {
    public class C {}
}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (5,2): Error SI0003: 'A<>' is invalid in a registration.
                // Registration(typeof(A<>))
                new DiagnosticResult("SI0003", @"Registration(typeof(A<>))").WithLocation(5, 2),
                // (6,2): Error SI0003: 'A<>' is invalid in a registration.
                // Registration(typeof(A<int>), typeof(A<>))
                new DiagnosticResult("SI0003", @"Registration(typeof(A<int>), typeof(A<>))").WithLocation(6, 2),
                // (7,2): Error SI0003: 'B<>.C' is invalid in a registration.
                // Registration(typeof(B<>.C))
                new DiagnosticResult("SI0003", @"Registration(typeof(B<>.C))").WithLocation(7, 2));

            registrations.Should().BeEmpty();
        }

        [Fact]
        public void ErrorIfRegisteredTypeIsNotBaseTypeOfRegisteredAs()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))]
public class Container
{
}

public class A : B, IA, ID {}
public class B : C<int> {}
public class C<T> {}
public class D {};
public interface IA : IB {}
public interface IB : IC<int> {}
public interface IC<T> {}
public interface ID {}
public interface IE {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (5,2): Error SI0001: 'A' does not implement 'C<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(5, 2),
                // (5,2): Error SI0001: 'A' does not implement 'D'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(5, 2),
                // (5,2): Error SI0001: 'A' does not implement 'IC<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(5, 2),
                // (5,2): Error SI0001: 'A' does not implement 'IE'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(5, 2));

            var cIntType = comp.AssertGetTypeByMetadataName("C`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var icIntType = comp.AssertGetTypeByMetadataName("IC`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("A"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("B"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [cIntType] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: cIntType,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: cIntType,
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IB"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [icIntType] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: icIntType,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: icIntType,
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("ID")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("ID"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("ID"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void RegistersFactoryTypes()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Container]
[Registration(typeof(B), typeof(IFactory<A>))]
public class Container
{
}

public class A {}
public class B : IFactory<A> { public ValueTask<A> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfA,
                        isFactory: true,
                        requiresAsyncInitialization: false),
                [factoryOfA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfA,
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void RegistersRequiresInitializationTypes()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Container]
[Registration(typeof(B), typeof(IFactory<A>))]
public class Container
{
}

public class A {}
public class B : IFactory<A>, IRequiresInitialization { public ValueTask<A> CreateAsync() => throw null; public ValueTask InitializeAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfA,
                        isFactory: true,
                        requiresAsyncInitialization: true),
                [factoryOfA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfA,
                        isFactory: false,
                        requiresAsyncInitialization: true),
            });
        }

        [Fact]
        public void ErrorIfMultipleConstructors()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B))]
[Registration(typeof(C))]
[Registration(typeof(D))]
[Registration(typeof(E))]
[Registration(typeof(F))]
[Registration(typeof(G))]
[Registration(typeof(H))]
[Registration(typeof(I))]
public class Container
{
}

public class A {}
public class B { public B(int a) {} }
public class C { public C(int a) {} public C(bool b) {} }
public class D { public D() {}  }
public class E { public E() {} public E(int a) {} }
public class F { public F() {} public F(int a) {} public F(bool a) {} }
public class G { public G() {} public G() {} }
public class H { internal H() {} }
public class I { internal I(int a) {} public I(bool b) {} }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Equal("(24,39): error CS0111: Type 'G' already defines a member called '.ctor' with the same parameter types", Assert.Single(comp.GetDiagnostics()).ToString());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (7,2): Error SI0006: 'C' has multiple non-default public constructors.
                // Registration(typeof(C))
                new DiagnosticResult("SI0006", @"Registration(typeof(C))").WithLocation(7, 2),
                // (10,2): Error SI0006: 'F' has multiple non-default public constructors.
                // Registration(typeof(F))
                new DiagnosticResult("SI0006", @"Registration(typeof(F))").WithLocation(10, 2),
                // (12,2): Error SI0005: 'H' does not have any public constructors.
                // Registration(typeof(H))
                new DiagnosticResult("SI0005", @"Registration(typeof(H))").WithLocation(12, 2));

            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("A"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("B"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("D")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("D"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("E")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("E"),
                        registeredAs: comp.AssertGetTypeByMetadataName("E"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("E"),
                        isFactory: false,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("E").Constructors.First(x => x.Parameters.Length > 0)),
                [comp.AssertGetTypeByMetadataName("G")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("G"),
                        registeredAs: comp.AssertGetTypeByMetadataName("G"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("G"),
                        isFactory: false,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("G").Constructors[0]),
                [comp.AssertGetTypeByMetadataName("I")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("I"),
                        registeredAs: comp.AssertGetTypeByMetadataName("I"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("I"),
                        isFactory: false,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("I").Constructors.First(x=> x.DeclaredAccessibility == Accessibility.Public)),
            });
        }

        [Fact]
        public void ErrorIfRegisteredOrRegisteredAsTypesAreNotPublic()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(typeof(A))]
[Registration(typeof(B), typeof(IB), typeof(IB<A>))]
[Registration(typeof(C<A>))]
[Registration(typeof(Outer.Inner))]
public class Container
{
}

internal class A {}
public class B : IB, IB<A> {}
internal interface IB {}
public interface IB<T> {}
public class C<T> {}
internal class Outer
{
    public class Inner {}
}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => diagnostics.Add(x), default);
            diagnostics.Verify(
                // (5,2): Error SI0007: 'A' is not public.
                // Registration(typeof(A))
                new DiagnosticResult("SI0007", @"Registration(typeof(A))").WithLocation(5, 2),
                // (6,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(6, 2),
                // (6,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(6, 2),
                // (7,2): Error SI0007: 'C<A>' is not public.
                // Registration(typeof(C<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(C<A>))").WithLocation(7, 2),
                // (8,2): Error SI0007: 'Outer.Inner' is not public.
                // Registration(typeof(Outer.Inner))
                new DiagnosticResult("SI0007", @"Registration(typeof(Outer.Inner))").WithLocation(8, 2));

            registrations.Should().BeEmpty();
        }

        [Fact]
        public void ParametersRearrangedWithNamedParameters()
        {
            string userSource = @"
using StrongInject.Runtime;

[Container]
[Registration(registeredAs: new[] { typeof(IA) }, type: typeof(A))]
[Registration(lifetime: Lifetime.SingleInstance, registeredAs: new[] { typeof(B), typeof(IB) }, type: typeof(B))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IB {}
public interface IB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: comp.AssertGetTypeByMetadataName("IA"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        lifetime: Lifetime.SingleInstance,
                        castTarget: comp.AssertGetTypeByMetadataName("IB"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        lifetime: Lifetime.SingleInstance,
                        castTarget: comp.AssertGetTypeByMetadataName("B"),
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CanRegisterNonNamedTypeSymbolViaFactory()
        {
            string userSource = @"
using StrongInject.Runtime;
using System.Threading.Tasks;

[Container]
[Registration(typeof(A), typeof(IFactory<int[]>))]
public class Container
{
}

public class A : IFactory<int[]> { public ValueTask<int[]> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = RegistrationCalculator.CalculateRegistrations(comp.AssertGetTypeByMetadataName("Container"), comp, x => Assert.False(true, x.ToString()), default);
            var intArray = comp.CreateArrayTypeSymbol(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var factoryOfIntArray = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(intArray);
            registrations.Should().Equal(new Dictionary<ITypeSymbol, Registration>
            {
                [intArray] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: intArray,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfIntArray,
                        isFactory: true,
                        requiresAsyncInitialization: false),
                [factoryOfIntArray] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfIntArray,
                        lifetime: Lifetime.InstancePerDependency,
                        castTarget: factoryOfIntArray,
                        isFactory: false,
                        requiresAsyncInitialization: false),
            });
        }

        private static Registration Registration(
            INamedTypeSymbol type,
            ITypeSymbol registeredAs,
            Lifetime lifetime,
            INamedTypeSymbol castTarget,
            bool isFactory,
            bool requiresAsyncInitialization,
            IMethodSymbol? constructor = null)
        {
            if (constructor is null)
            {
                constructor = Assert.Single(type.Constructors);
            }

            return new Registration(type, registeredAs, lifetime, castTarget, isFactory, requiresAsyncInitialization, constructor);
        }
    }
}