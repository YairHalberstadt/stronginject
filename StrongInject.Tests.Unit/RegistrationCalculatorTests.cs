using FluentAssertions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public class RegistrationCalculatorTests : TestBase
    {
        public RegistrationCalculatorTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void CalculatesDirectRegistrations()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), typeof(IA))]
[Registration(typeof(B), Scope.SingleInstance, typeof(B), typeof(IB))]
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
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CalculatesIndirectRegistrations()
        {
            string userSource = @"
using StrongInject;

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
[Registration(typeof(D), Scope.SingleInstance, typeof(D), typeof(ID))]
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
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IC")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("C"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IC"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("D")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void DirectRegistrationsHasPriorityOverIndirect()
        {
            string userSource = @"
using StrongInject;

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

            var moduleRegistrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Module"));
            moduleRegistrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });

            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfTwoModulesRegisterSameType()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0002: Modules 'ModuleA' and 'ModuleB' provide differing registrations for 'IA'.
                // ModuleRegistration(typeof(ModuleA))
                new DiagnosticResult("SI0002", @"ModuleRegistration(typeof(ModuleA))").WithLocation(4, 2),
                // (5,2): Error SI0002: Modules 'ModuleA' and 'ModuleB' provide differing registrations for 'IA'.
                // ModuleRegistration(typeof(ModuleB))
                new DiagnosticResult("SI0002", @"ModuleRegistration(typeof(ModuleB))").WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().HaveCount(1);
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().ContainKey(comp.GetTypeByMetadataName("IA")!);
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne1()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne2()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfModuleRegistersSameTypeTwice()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Error SI0004: Module already contains registration for 'IA'.
                // Registration(typeof(B), typeof(IA))
                new DiagnosticResult("SI0004", @"Registration(typeof(B), typeof(IA))").WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfModuleRegistersSameTypeTwiceThroughFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (6,2): Error SI0004: Module already contains registration for 'IA'.
                // Registration(typeof(B), typeof(IFactory<IA>))
                new DiagnosticResult("SI0004", @"Registration(typeof(B), typeof(IFactory<IA>))").WithLocation(6, 2));

            var factoryOfIA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("IA"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [factoryOfIA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfIA,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorWhenRegisteringOpenGeneric()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0011: Unbound Generic Type 'A<>' is invalid in a registration.
                // Registration(typeof(A<>))
                new DiagnosticResult("SI0011", @"Registration(typeof(A<>))", DiagnosticSeverity.Error).WithLocation(4, 2),
                // (5,2): Error SI0011: Unbound Generic Type 'A<>' is invalid in a registration.
                // Registration(typeof(A<int>), typeof(A<>))
                new DiagnosticResult("SI0011", @"Registration(typeof(A<int>), typeof(A<>))", DiagnosticSeverity.Error).WithLocation(5, 2),
                // (6,2): Error SI0011: Unbound Generic Type 'B<>.C' is invalid in a registration.
                // Registration(typeof(B<>.C))
                new DiagnosticResult("SI0011", @"Registration(typeof(B<>.C))", DiagnosticSeverity.Error).WithLocation(6, 2));

            registrations.ToDictionary(x => x.Key, x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public void NoErrorWhenRegisteringErrorType()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(Invalid))]
[Registration(typeof(A<int>), typeof(Invalid))]
[Registration(typeof(A<int>), registeredAs: new [] { typeof(Invalid) })]
[Registration(typeof(A<int>), registeredAs: new System.Type[] { typeof(Invalid) })]
[Registration(typeof(A<Invalid>))]
[ModuleRegistration(typeof(Invalid))]
[ModuleRegistration(typeof(A<>), typeof(Invalid))]
[ModuleRegistration(typeof(A<>), exclusionList: new [] { typeof(Invalid) })]
[ModuleRegistration(typeof(A<>), exclusionList: new System.Type[] { typeof(Invalid) })]
public class Container
{
}

public class A<T> {}
public class A<T1, T2> {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify(
                // (4,22): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(4, 22),
                // (5,38): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(5, 38),
                // (6,61): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(6, 61),
                // (7,72): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(7, 72),
                // (8,24): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(8, 24),
                // (9,28): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(9, 28),
                // (10,41): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(10, 41),
                // (11,65): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(11, 65),
                // (12,76): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(12, 76));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();

            registrations.ToDictionary(x => x.Key, x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public void ErrorIfRegisteredTypeIsNotBaseTypeOfRegisteredAs()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0001: 'A' does not implement 'C<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'D'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'IC<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'IE'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2));

            var cIntType = comp.AssertGetTypeByMetadataName("C`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var icIntType = comp.AssertGetTypeByMetadataName("IC`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [cIntType] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: cIntType,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [icIntType] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: icIntType,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("ID")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("ID"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void RegistersFactoryTypes()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(B), typeof(IFactory<A>))]
public class Container
{
}

public class A {}
public class B : IFactory<A> { public ValueTask<A> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    new FactoryRegistration(
                        factoryType: factoryOfA,
                        factoryOf: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution),
                [factoryOfA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void RegistersRequiresInitializationTypes()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(B), typeof(IFactory<A>))]
public class Container
{
}

public class A {}
public class B : IFactory<A>, IRequiresInitialization { public ValueTask<A> CreateAsync() => throw null; public ValueTask InitializeAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    new FactoryRegistration(
                        factoryType: factoryOfA,
                        factoryOf: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution),
                [factoryOfA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: true),
            });
        }

        [Fact]
        public void ErrorIfMultipleConstructors()
        {
            string userSource = @"
using StrongInject;

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
            Assert.Equal("(23,39): error CS0111: Type 'G' already defines a member called '.ctor' with the same parameter types", Assert.Single(comp.GetDiagnostics()).ToString());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (6,2): Error SI0006: 'C' has multiple non-default public constructors.
                // Registration(typeof(C))
                new DiagnosticResult("SI0006", @"Registration(typeof(C))").WithLocation(6, 2),
                // (9,2): Error SI0006: 'F' has multiple non-default public constructors.
                // Registration(typeof(F))
                new DiagnosticResult("SI0006", @"Registration(typeof(F))").WithLocation(9, 2),
                // (11,2): Error SI0005: 'H' does not have any public constructors.
                // Registration(typeof(H))
                new DiagnosticResult("SI0005", @"Registration(typeof(H))").WithLocation(11, 2));

            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("D")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("E")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("E"),
                        registeredAs: comp.AssertGetTypeByMetadataName("E"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("E").Constructors.First(x => x.Parameters.Length > 0)),
                [comp.AssertGetTypeByMetadataName("G")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("G"),
                        registeredAs: comp.AssertGetTypeByMetadataName("G"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("G").Constructors[0]),
                [comp.AssertGetTypeByMetadataName("I")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("I"),
                        registeredAs: comp.AssertGetTypeByMetadataName("I"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("I").Constructors.First(x => x.DeclaredAccessibility == Accessibility.Public)),
            });
        }

        [Fact]
        public void ErrorIfRegisteredOrRegisteredAsTypesAreNotPublic()
        {
            string userSource = @"
using StrongInject;

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
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0007: 'A' is not public.
                // Registration(typeof(A))
                new DiagnosticResult("SI0007", @"Registration(typeof(A))").WithLocation(4, 2),
                // (5,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(5, 2),
                // (5,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(5, 2),
                // (6,2): Error SI0007: 'C<A>' is not public.
                // Registration(typeof(C<A>))
                new DiagnosticResult("SI0007", @"Registration(typeof(C<A>))").WithLocation(6, 2),
                // (7,2): Error SI0007: 'Outer.Inner' is not public.
                // Registration(typeof(Outer.Inner))
                new DiagnosticResult("SI0007", @"Registration(typeof(Outer.Inner))").WithLocation(7, 2));

            registrations.ToDictionary(x => x.Key, x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public void ParametersRearrangedWithNamedParameters()
        {
            string userSource = @"
using StrongInject;

[Registration(registeredAs: new[] { typeof(IA) }, type: typeof(A))]
[Registration(scope: Scope.SingleInstance, registeredAs: new[] { typeof(B), typeof(IB) }, type: typeof(B))]
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
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
                [comp.AssertGetTypeByMetadataName("B")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CanRegisterNonNamedTypeSymbolViaFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A), typeof(IFactory<int[]>))]
public class Container
{
}

public class A : IFactory<int[]> { public ValueTask<int[]> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var intArray = comp.CreateArrayTypeSymbol(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var factoryOfIntArray = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(intArray);
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [intArray] =
                    new FactoryRegistration(
                        factoryType: factoryOfIntArray,
                        factoryOf: intArray,
                        scope: Scope.InstancePerResolution),
                [factoryOfIntArray] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfIntArray,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameModule()
        {
            string userSource = @"
using StrongInject;

[ModuleRegistration(typeof(ModuleA))]
[ModuleRegistration(typeof(ModuleB))]
public class Container
{
}

[ModuleRegistration(typeof(ModuleC))]
public class ModuleA
{
}

[ModuleRegistration(typeof(ModuleC))]
public class ModuleB
{
}

[Registration(typeof(A))]
public class ModuleC
{
}

public class A {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void ErrorIfStructIsRegisteredAsSingleInstance()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), Scope.SingleInstance, typeof(IA))]
[Registration(typeof(B), Scope.SingleInstance)]
public class Container
{
}

public struct A : IA {}
public interface IA {}
public struct B {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0008: 'A' is a struct and cannot have a Single Instance scope.
                // Registration(typeof(A), Scope.SingleInstance, typeof(IA))
                new DiagnosticResult("SI0008", @"Registration(typeof(A), Scope.SingleInstance, typeof(IA))").WithLocation(4, 2),
                // (5,2): Error SI0008: 'B' is a struct and cannot have a Single Instance scope.
                // Registration(typeof(B), Scope.SingleInstance)
                new DiagnosticResult("SI0008", @"Registration(typeof(B), Scope.SingleInstance)").WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public void ErrorIfStructIsRegisteredAsSingleInstanceThroughFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Registration(typeof(A), Scope.SingleInstance, Scope.SingleInstance, typeof(IFactory<B>))]
public class Container
{
}

public class A : IFactory<B> { public ValueTask<B> CreateAsync() => throw null; }
public struct B {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Error SI0008: 'B' is a struct and cannot have a Single Instance scope.
                // Registration(typeof(A), Scope.SingleInstance, Scope.SingleInstance, typeof(IFactory<B>))
                new DiagnosticResult("SI0008", @"Registration(typeof(A), Scope.SingleInstance, Scope.SingleInstance, typeof(IFactory<B>))").WithLocation(5, 2));
            var factoryOfB = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("B"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [factoryOfB] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfB,
                        scope: Scope.SingleInstance,
                        requiresAsyncInitialization: false),
            });
        }


        [Fact]
        public void CanRegisterTypeAsCovariantInterfaceItImplements()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), typeof(IA<object>))]
public class Container
{
}

public interface IA<out T> {}
public class A : IA<string> {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var iAOfObject = comp.AssertGetTypeByMetadataName("IA`1").Construct(comp.AssertGetTypeByMetadataName(typeof(object).FullName!));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [iAOfObject] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: iAOfObject,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CanRegisterStructAsInterface()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), typeof(IA))]
public class Container
{
}

public interface IA {}
public struct A : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CanRegisterStructAsNullableConversion()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A), typeof(A?))]
public class Container
{
}

public struct A {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            var registrations = new RegistrationCalculator(comp, x => Assert.False(true, x.ToString()), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var nullableA = comp.AssertGetTypeByMetadataName(typeof(Nullable<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => x.Value).Should().Equal(new Dictionary<ITypeSymbol, InstanceSource>
            {
                [nullableA] =
                    Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: nullableA,
                        scope: Scope.InstancePerResolution,
                        requiresAsyncInitialization: false),
            });
        }

        [Fact]
        public void CannotRegisterAsUserDefinedConversion()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(B), typeof(A))]
public class Container
{
}

public class A {}
public class B 
{
    public static implicit operator A(B b) => new A();
}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0001: 'B' does not have an identity, implicit reference, or boxing conversion to 'A'.
                // Registration(typeof(B), typeof(A))
                new DiagnosticResult("SI0001", @"Registration(typeof(B), typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorOnModuleRegisteringItself()
        {
            string userSource = @"
using StrongInject;

[ModuleRegistration(typeof(ModuleA))]
public class ModuleA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
            diagnostics.Verify(
                // (5,14): Error SI0009: Registration for 'ModuleA' is recursive.
                // ModuleA
                new DiagnosticResult("SI0009", @"ModuleA", DiagnosticSeverity.Error).WithLocation(5, 14));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorOnRecursiveModuleRegistrations1()
        {
            string userSource = @"
using StrongInject;

[ModuleRegistration(typeof(ModuleB))]
public class ModuleA {}

[ModuleRegistration(typeof(ModuleA))]
public class ModuleB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
            diagnostics.Verify(
                // (5,14): Error SI0009: Registration for 'ModuleA' is recursive.
                // ModuleA
                new DiagnosticResult("SI0009", @"ModuleA", DiagnosticSeverity.Error).WithLocation(5, 14));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorOnRecursiveModuleRegistrations2()
        {
            string userSource = @"
using StrongInject;

[ModuleRegistration(typeof(ModuleB))]
public class ModuleA {}

[ModuleRegistration(typeof(ModuleC))]
public class ModuleB {}

[ModuleRegistration(typeof(ModuleA))]
public class ModuleC {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
            diagnostics.Verify(
                // (5,14): Error SI0009: Registration for 'ModuleA' is recursive.
                // ModuleA
                new DiagnosticResult("SI0009", @"ModuleA", DiagnosticSeverity.Error).WithLocation(5, 14));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorOnRegisteringAbstractClassWithPublicConstructor()
        {
            string userSource = @"
using StrongInject;

[Registration(typeof(A))]
public class Container
{
}

public abstract class A { public A(){} }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var registrations = new RegistrationCalculator(comp, x => diagnostics.Add(x), default).GetRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0010: Cannot register 'A' as it is abstract.
                // Registration(typeof(A))
                new DiagnosticResult("SI0010", @"Registration(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2));
            Assert.Empty(registrations);
        }

        private static Registration Registration(
            INamedTypeSymbol type,
            ITypeSymbol registeredAs,
            Scope scope,
            bool requiresAsyncInitialization,
            IMethodSymbol? constructor = null)
        {
            if (constructor is null)
            {
                constructor = Assert.Single(type.Constructors);
            }

            return new Registration(type, registeredAs, scope, requiresAsyncInitialization, constructor);
        }
    }
}