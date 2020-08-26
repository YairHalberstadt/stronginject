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

[Register(typeof(A), typeof(IA))]
[Register(typeof(B), Scope.SingleInstance, typeof(B), typeof(IB))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IB {}
public interface IB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("B")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void CalculatesIndirectRegistrations()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB), typeof(ID))]
public class Container
{
}

[Register(typeof(B), typeof(IB))]
public class ModuleA
{
}

[Register(typeof(C), typeof(IC))]
[Register(typeof(D), Scope.SingleInstance, typeof(D), typeof(ID))]
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IC")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("C"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IC"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("D")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void DirectRegistrationsHasPriorityOverIndirect()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[RegisterModule(typeof(Module))]
public class Container
{
}

[Register(typeof(B), typeof(IA))]
public class Module
{
}

public class A : IA {}
public class B : IA {}
public interface IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var moduleRegistrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Module"));
            moduleRegistrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });

            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]
                        {
                            Registration(
                                type: comp.AssertGetTypeByMetadataName("B"),
                                registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                                scope: Scope.InstancePerResolution,
                                requiresInitialization: false)
                        }),
            });
        }

        [Fact]
        public void NoBestTypeIfTwoModulesRegisterSameType()
        {
            string userSource = @"
using StrongInject;

[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB))]
public class Container
{
}

[Register(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Register(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (null, new InstanceSource[]{
                        Registration(
                            type: comp.AssertGetTypeByMetadataName("A"),
                            registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            requiresInitialization: false),
                        Registration(
                            type: comp.AssertGetTypeByMetadataName("B"),
                            registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            requiresInitialization: false),
                    }),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne1()
        {
            string userSource = @"
using StrongInject;

[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB), typeof(IA))]
public class Container
{
}

[Register(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Register(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameTypeButTypeIsExcludedFromOne2()
        {
            string userSource = @"
using StrongInject;

[RegisterModule(typeof(ModuleA), typeof(IA))]
[RegisterModule(typeof(ModuleB))]
public class Container
{
}

[Register(typeof(A), typeof(IA))]
public class ModuleA
{
}

[Register(typeof(B), typeof(IA))]
public class ModuleB
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void NoBestTypeIfModuleRegistersSameTypeTwice()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
[Register(typeof(B), typeof(IA))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (null, new InstanceSource[]{ 
                        Registration(
                            type: comp.AssertGetTypeByMetadataName("A"),
                            registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            requiresInitialization: false),
                        Registration(
                            type: comp.AssertGetTypeByMetadataName("B"),
                            registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            requiresInitialization: false),
                    }),
            });
        }

        [Fact]
        public void NoBestTypeIfModuleRegistersSameTypeTwiceThroughFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A), typeof(IA))]
[RegisterFactory(typeof(B))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IAsyncFactory<IA> { public ValueTask<IA> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();

            var factoryOfIA = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("IA"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (null, new InstanceSource[]
                    {
                        Registration(
                            type: comp.AssertGetTypeByMetadataName("A"),
                            registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            requiresInitialization: false),
                        new FactoryRegistration(
                            factoryType: factoryOfIA,
                            factoryOf: comp.AssertGetTypeByMetadataName("IA"),
                            scope: Scope.InstancePerResolution,
                            isAsync: true)
                    }),
                [factoryOfIA] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfIA,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void ErrorWhenRegisteringOpenGeneric()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A<>))]
[Register(typeof(A<int>), typeof(A<>))]
[Register(typeof(B<>.C))]
public class Container
{
}

public class A<T> {}
public class B<T> {
    public class C {}
}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0011: Unbound Generic Type 'A<>' is invalid in a registration.
                // Registration(typeof(A<>))
                new DiagnosticResult("SI0011", @"Register(typeof(A<>))", DiagnosticSeverity.Error).WithLocation(4, 2),
                // (5,2): Error SI0011: Unbound Generic Type 'A<>' is invalid in a registration.
                // Registration(typeof(A<int>), typeof(A<>))
                new DiagnosticResult("SI0011", @"Register(typeof(A<int>), typeof(A<>))", DiagnosticSeverity.Error).WithLocation(5, 2),
                // (6,2): Error SI0011: Unbound Generic Type 'B<>.C' is invalid in a registration.
                // Registration(typeof(B<>.C))
                new DiagnosticResult("SI0011", @"Register(typeof(B<>.C))", DiagnosticSeverity.Error).WithLocation(6, 2));

            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEmpty();
        }

        [Fact]
        public void NoErrorWhenRegisteringErrorType()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(Invalid))]
[Register(typeof(A<int>), typeof(Invalid))]
[Register(typeof(A<int>), registerAs: new [] { typeof(Invalid) })]
[Register(typeof(A<int>), registerAs: new System.Type[] { typeof(Invalid) })]
[Register(typeof(A<Invalid>))]
[RegisterModule(typeof(Invalid))]
[RegisterModule(typeof(A<>), typeof(Invalid))]
[RegisterModule(typeof(A<>), exclusionList: new [] { typeof(Invalid) })]
[RegisterModule(typeof(A<>), exclusionList: new System.Type[] { typeof(Invalid) })]
public class Container
{
}

public class A<T> {}
public class A<T1, T2> {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            comp.GetDiagnostics().Verify(
                // (4,18): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(4, 18),
                // (5,34): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(5, 34),
                // (6,57): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(6, 55),
                // (7,68): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(7, 66),
                // (8,20): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(8, 20),
                // (9,24): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(9, 24),
                // (10,37): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(10, 37),
                // (11,61): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(11, 61),
                // (12,72): Error CS0246: The type or namespace name 'Invalid' could not be found (are you missing a using directive or an assembly reference?)
                // Invalid
                new DiagnosticResult("CS0246", @"Invalid", DiagnosticSeverity.Error).WithLocation(12, 72));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();

            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEmpty();
        }

        [Fact]
        public void ErrorIfRegisteredTypeIsNotBaseTypeOfRegisteredAs()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))]
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0001: 'A' does not implement 'C<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Register(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'D'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Register(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'IC<string>'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Register(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2),
                // (4,2): Error SI0001: 'A' does not implement 'IE'.
                // Registration(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))
                new DiagnosticResult("SI0001", @"Register(typeof(A), typeof(A), typeof(B), typeof(C<int>), typeof(C<string>), typeof(D), typeof(IA), typeof(IB), typeof(IC<int>), typeof(IC<string>), typeof(ID), typeof(IE))").WithLocation(4, 2));

            var cIntType = comp.AssertGetTypeByMetadataName("C`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var icIntType = comp.AssertGetTypeByMetadataName("IC`1").Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("B")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [cIntType] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: cIntType,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [icIntType] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: icIntType,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("ID")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("ID"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersFactoryTypes()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(B))]
public class Container
{
}

public class A {}
public class B : IAsyncFactory<A> { public ValueTask<A> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (new FactoryRegistration(
                        factoryType: factoryOfA,
                        factoryOf: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        isAsync: true), new InstanceSource[]{}),
                [factoryOfA] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersRequiresInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(C))]
[RegisterFactory(typeof(B))]
public class Container
{
}

public class A {}
public class B : IAsyncFactory<A>, IRequiresAsyncInitialization { public ValueTask<A> CreateAsync() => throw null; public ValueTask InitializeAsync() => throw null; }
public class C : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var factoryOfA = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (new FactoryRegistration(
                        factoryType: factoryOfA,
                        factoryOf: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        isAsync: true), new InstanceSource[]{}),
                [factoryOfA] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: factoryOfA,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true,
                        isAsync: true), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("C")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("C"),
                        registeredAs: comp.AssertGetTypeByMetadataName("C"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true,
                        isAsync: true), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void ErrorIfMultipleConstructors()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B))]
[Register(typeof(C))]
[Register(typeof(D))]
[Register(typeof(E))]
[Register(typeof(F))]
[Register(typeof(G))]
[Register(typeof(H))]
[Register(typeof(I))]
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Equal("(23,39): error CS0111: Type 'G' already defines a member called '.ctor' with the same parameter types", Assert.Single(comp.GetDiagnostics()).ToString());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (6,2): Error SI0006: 'C' has multiple non-default public constructors.
                // Registration(typeof(C))
                new DiagnosticResult("SI0006", @"Register(typeof(C))").WithLocation(6, 2),
                // (9,2): Error SI0006: 'F' has multiple non-default public constructors.
                // Registration(typeof(F))
                new DiagnosticResult("SI0006", @"Register(typeof(F))").WithLocation(9, 2),
                // (11,2): Error SI0005: 'H' does not have any public constructors.
                // Registration(typeof(H))
                new DiagnosticResult("SI0005", @"Register(typeof(H))").WithLocation(11, 2));

            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("B")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("D")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("D"),
                        registeredAs: comp.AssertGetTypeByMetadataName("D"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("E")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("E"),
                        registeredAs: comp.AssertGetTypeByMetadataName("E"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("E").InstanceConstructors.First(x => x.Parameters.Length > 0)), new InstanceSource[] { }),
                [comp.AssertGetTypeByMetadataName("G")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("G"),
                        registeredAs: comp.AssertGetTypeByMetadataName("G"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("G").InstanceConstructors[0]), new InstanceSource[] { }),
                [comp.AssertGetTypeByMetadataName("I")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("I"),
                        registeredAs: comp.AssertGetTypeByMetadataName("I"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false,
                        constructor: comp.AssertGetTypeByMetadataName("I").InstanceConstructors.First(x => x.DeclaredAccessibility == Accessibility.Public)), new InstanceSource[] { }),
            });
        }

        [Fact]
        public void ErrorIfRegisteredOrRegisteredAsTypesAreNotPublic()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
[Register(typeof(B), typeof(IB), typeof(IB<A>))]
[Register(typeof(C<A>))]
[Register(typeof(Outer.Inner))]
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
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0007: 'A' is not public.
                // Registration(typeof(A))
                new DiagnosticResult("SI0007", @"Register(typeof(A))").WithLocation(4, 2),
                // (5,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Register(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(5, 2),
                // (5,2): Error SI0007: 'B' is not public.
                // Registration(typeof(B), typeof(IB), typeof(IB<A>))
                new DiagnosticResult("SI0007", @"Register(typeof(B), typeof(IB), typeof(IB<A>))").WithLocation(5, 2),
                // (6,2): Error SI0007: 'C<A>' is not public.
                // Registration(typeof(C<A>))
                new DiagnosticResult("SI0007", @"Register(typeof(C<A>))").WithLocation(6, 2),
                // (7,2): Error SI0007: 'Outer.Inner' is not public.
                // Registration(typeof(Outer.Inner))
                new DiagnosticResult("SI0007", @"Register(typeof(Outer.Inner))").WithLocation(7, 2));

            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEmpty();
        }

        [Fact]
        public void ParametersRearrangedWithNamedParameters()
        {
            string userSource = @"
using StrongInject;

[Register(registerAs: new[] { typeof(IA) }, type: typeof(A))]
[Register(scope: Scope.SingleInstance, registerAs: new[] { typeof(B), typeof(IB) }, type: typeof(B))]
public class Container
{
}

public class A : IA {}
public interface IA {}
public class B : IB {}
public interface IB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("IB")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IB"),
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName("B")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("B"),
                        registeredAs: comp.AssertGetTypeByMetadataName("B"),
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void CanRegisterNonNamedTypeSymbolViaFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A : IAsyncFactory<int[]> { public ValueTask<int[]> CreateAsync() => throw null; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var intArray = comp.CreateArrayTypeSymbol(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            var factoryOfIntArray = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(intArray);
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [intArray] =
                    (new FactoryRegistration(
                        factoryType: factoryOfIntArray,
                        factoryOf: intArray,
                        scope: Scope.InstancePerResolution,
                        isAsync: true), new InstanceSource[]{}),
                [factoryOfIntArray] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfIntArray,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void NoErrorIfTwoModulesRegisterSameModule()
        {
            string userSource = @"
using StrongInject;

[RegisterModule(typeof(ModuleA))]
[RegisterModule(typeof(ModuleB))]
public class Container
{
}

[RegisterModule(typeof(ModuleC))]
public class ModuleA
{
}

[RegisterModule(typeof(ModuleC))]
public class ModuleB
{
}

[Register(typeof(A))]
public class ModuleC
{
}

public class A {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void ErrorIfStructIsRegisteredAsSingleInstance()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), Scope.SingleInstance, typeof(IA))]
[Register(typeof(B), Scope.SingleInstance)]
public class Container
{
}

public struct A : IA {}
public interface IA {}
public struct B {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0008: 'A' is a struct and cannot have a Single Instance scope.
                // Registration(typeof(A), Scope.SingleInstance, typeof(IA))
                new DiagnosticResult("SI0008", @"Register(typeof(A), Scope.SingleInstance, typeof(IA))").WithLocation(4, 2),
                // (5,2): Error SI0008: 'B' is a struct and cannot have a Single Instance scope.
                // Registration(typeof(B), Scope.SingleInstance)
                new DiagnosticResult("SI0008", @"Register(typeof(B), Scope.SingleInstance)").WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEmpty();
        }

        [Fact]
        public void ErrorIfStructIsRegisteredAsSingleInstanceThroughFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A), Scope.SingleInstance, Scope.SingleInstance)]
public class Container
{
}

public class A : IAsyncFactory<B> { public ValueTask<B> CreateAsync() => throw null; }
public struct B {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Error SI0008: 'B' is a struct and cannot have a Single Instance scope.
                // FactoryRegistration(typeof(A), Scope.SingleInstance, Scope.SingleInstance)
                new DiagnosticResult("SI0008", @"RegisterFactory(typeof(A), Scope.SingleInstance, Scope.SingleInstance)", DiagnosticSeverity.Error).WithLocation(5, 2));
            var factoryOfB = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("B"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [factoryOfB] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfB,
                        scope: Scope.SingleInstance,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void ErrorIfStructIsRegisteredAsSingleInstanceFactory()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A), Scope.SingleInstance, Scope.InstancePerResolution)]
public class Container
{
}

public struct A : IAsyncFactory<B> { public ValueTask<B> CreateAsync() => throw null; }
public struct B {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Error SI0008: 'A' is a struct and cannot have a Single Instance scope.
                // FactoryRegistration(typeof(A), Scope.SingleInstance, Scope.InstancePerResolution)
                new DiagnosticResult("SI0008", @"RegisterFactory(typeof(A), Scope.SingleInstance, Scope.InstancePerResolution)", DiagnosticSeverity.Error).WithLocation(5, 2));
            Assert.Empty(registrations);
        }


        [Fact]
        public void CanRegisterTypeAsCovariantInterfaceItImplements()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA<object>))]
public class Container
{
}

public interface IA<out T> {}
public class A : IA<string> {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var iAOfObject = comp.AssertGetTypeByMetadataName("IA`1").Construct(comp.AssertGetTypeByMetadataName(typeof(object).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [iAOfObject] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: iAOfObject,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void CanRegisterStructAsInterface()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(IA))]
public class Container
{
}

public interface IA {}
public struct A : IA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("IA")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("IA"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void CanRegisterStructAsNullableConversion()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A), typeof(A?))]
public class Container
{
}

public struct A {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => Assert.False(true, x.ToString()), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            var nullableA = comp.AssertGetTypeByMetadataName(typeof(Nullable<>).FullName!).Construct(comp.AssertGetTypeByMetadataName("A"));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [nullableA] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: nullableA,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void CannotRegisterAsUserDefinedConversion()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(B), typeof(A))]
public class Container
{
}

public class A {}
public class B 
{
    public static implicit operator A(B b) => new A();
}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0001: 'B' does not have an identity, implicit reference, or boxing conversion to 'A'.
                // Registration(typeof(B), typeof(A))
                new DiagnosticResult("SI0001", @"Register(typeof(B), typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorOnModuleRegisteringItself()
        {
            string userSource = @"
using StrongInject;

[RegisterModule(typeof(ModuleA))]
public class ModuleA {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
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

[RegisterModule(typeof(ModuleB))]
public class ModuleA {}

[RegisterModule(typeof(ModuleA))]
public class ModuleB {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
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

[RegisterModule(typeof(ModuleB))]
public class ModuleA {}

[RegisterModule(typeof(ModuleC))]
public class ModuleB {}

[RegisterModule(typeof(ModuleA))]
public class ModuleC {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("ModuleA"));
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

[Register(typeof(A))]
public class Container
{
}

public abstract class A { public A(){} }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0010: Cannot register 'A' as it is abstract.
                // Registration(typeof(A))
                new DiagnosticResult("SI0010", @"Register(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2));
            Assert.Empty(registrations);
        }

        [Fact]
        public void ErrorIfFactoryRegistrationIsNotFactory()
        {
            string userSource = @"
using StrongInject;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A {}
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (4,2): Error SI0012: 'A' is registered as a factory but does not implement StrongInject.IAsyncFactory<T>
                // FactoryRegistration(typeof(A))
                new DiagnosticResult("SI0012", @"RegisterFactory(typeof(A))", DiagnosticSeverity.Error).WithLocation(4, 2));
            Assert.Empty(registrations);
        }

        [Fact]
        public void WarnOnSimpleRegistrationOfTypeImplementingFactoryType()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
public class Container
{
}

public class A : IAsyncFactory<int> { public ValueTask<int> CreateAsync() => new ValueTask<int>(0); }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Warning SI1001: 'A' implements 'StrongInject.IAsyncFactory<int>'. Did you mean to use FactoryRegistration instead?
                // Registration(typeof(A))
                new DiagnosticResult("SI1001", @"Register(typeof(A))", DiagnosticSeverity.Warning).WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersInstanceOfIFactoryAsNotAsync()
        {
            string userSource = @"
using StrongInject;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A : IFactory<int> { public int Create() => 0; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            var factoryOfInt = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [factoryOfInt] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfInt,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName(typeof(int).FullName!)] =
                    (new FactoryRegistration(
                        factoryType: factoryOfInt,
                        factoryOf: comp.AssertGetTypeByMetadataName(typeof(int).FullName!),
                        scope: Scope.InstancePerResolution,
                        isAsync: false), new InstanceSource[] { }),
            });
        }

        [Fact]
        public void RegistersInstanceImplementingRequiresInitializationAsNotAsync()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A))]
public class Container
{
}

public class A : IRequiresInitialization { public void Initialize() {} }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            var factoryOfInt = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersInstanceOfIFactoryImplementingRequiresInitializationAsNotAsync()
        {
            string userSource = @"
using StrongInject;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A : IFactory<int>, IRequiresInitialization { public int Create() => 0; public void Initialize() {} }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            var factoryOfInt = comp.AssertGetTypeByMetadataName(typeof(IFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [factoryOfInt] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfInt,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName(typeof(int).FullName!)] =
                    (new FactoryRegistration(
                        factoryType: factoryOfInt,
                        factoryOf: comp.AssertGetTypeByMetadataName(typeof(int).FullName!),
                        scope: Scope.InstancePerResolution,
                        isAsync: false), new InstanceSource[] { }),
            });
        }


        [Fact]
        public void RegistersInstanceOfIAsyncFactoryAsAsync()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A : IAsyncFactory<int> { public ValueTask<int> CreateAsync() => default; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            var factoryOfInt = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [factoryOfInt] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfInt,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: false), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName(typeof(int).FullName!)] =
                    (new FactoryRegistration(
                        factoryType: factoryOfInt,
                        factoryOf: comp.AssertGetTypeByMetadataName(typeof(int).FullName!),
                        scope: Scope.InstancePerResolution,
                        isAsync: true), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersInstanceImplementingRequiresAsyncInitializationAsAsync()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
public class Container
{
}

public class A : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true,
                        isAsync: true), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void RegistersInstanceOfIAsyncFactoryImplementingRequiresAsyncInitializationAsAsync()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[RegisterFactory(typeof(A))]
public class Container
{
}

public class A : IAsyncFactory<int>, IRequiresAsyncInitialization { public ValueTask<int> CreateAsync() => default; public ValueTask InitializeAsync() => default; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify();
            var factoryOfInt = comp.AssertGetTypeByMetadataName(typeof(IAsyncFactory<>).FullName!).Construct(comp.AssertGetTypeByMetadataName(typeof(int).FullName!));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [factoryOfInt] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: factoryOfInt,
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true,
                        isAsync: true), new InstanceSource[]{}),
                [comp.AssertGetTypeByMetadataName(typeof(int).FullName!)] =
                    (new FactoryRegistration(
                        factoryType: factoryOfInt,
                        factoryOf: comp.AssertGetTypeByMetadataName(typeof(int).FullName!),
                        scope: Scope.InstancePerResolution,
                        isAsync: true), new InstanceSource[]{}),
            });
        }

        [Fact]
        public void ErrorIfTypeImplementsRequiresInitializationAndRequiresAsyncInitialization()
        {
            string userSource = @"
using StrongInject;
using System.Threading.Tasks;

[Register(typeof(A))]
public class Container
{
}

public class A : IRequiresInitialization, IRequiresAsyncInitialization { public void Initialize() {} public ValueTask InitializeAsync() => default; }
";
            Compilation comp = CreateCompilation(userSource, MetadataReference.CreateFromFile(typeof(IAsyncContainer<>).Assembly.Location));
            Assert.Empty(comp.GetDiagnostics());
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
            var registrations = new RegistrationCalculator(comp, wellKnownTypes, x => diagnostics.Add(x), default).GetModuleRegistrations(comp.AssertGetTypeByMetadataName("Container"));
            diagnostics.Verify(
                // (5,2): Error SI0013: 'A' implements both IRequiresInitialization and IRequiresAsyncInitialization
                // Registration(typeof(A))
                new DiagnosticResult("SI0013", @"Register(typeof(A))", DiagnosticSeverity.Error).WithLocation(5, 2));
            registrations.ToDictionary(x => x.Key, x => (x.Value.Best, x.Value.Without(x.Value.Best))).Should().BeEquivalentTo(new Dictionary<ITypeSymbol, (InstanceSource?, IEnumerable<InstanceSource>)>
            {
                [comp.AssertGetTypeByMetadataName("A")] =
                    (Registration(
                        type: comp.AssertGetTypeByMetadataName("A"),
                        registeredAs: comp.AssertGetTypeByMetadataName("A"),
                        scope: Scope.InstancePerResolution,
                        requiresInitialization: true,
                        isAsync: true), new InstanceSource[]{}),
            });
        }

        private static Registration Registration(
            INamedTypeSymbol type,
            ITypeSymbol registeredAs,
            Scope scope,
            bool requiresInitialization,
            bool isAsync = false,
            IMethodSymbol? constructor = null)
        {
            if (constructor is null)
            {
                constructor = Assert.Single(type.InstanceConstructors);
            }

            return new Registration(type, registeredAs, scope, requiresInitialization, constructor, isAsync);
        }
    }
}