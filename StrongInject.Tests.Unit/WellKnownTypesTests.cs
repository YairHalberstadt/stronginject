using FluentAssertions;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public class WellKnownTypesTests : TestBase
    {
        public WellKnownTypesTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        // [Fact]
        // public void TestGetClassAttributes()
        // {
        //     Compilation comp = CreateCompilationWithStrongInjectReference("");
        //     Assert.Empty(comp.GetDiagnostics());
        //     Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
        //
        //     var wellKnowTypeSymbols = wellKnownTypes
        //         .GetType().GetProperties().Select(x => x.GetValue(wellKnownTypes)).Cast<INamedTypeSymbol>();
        //
        //     var attributeUsage = comp.AssertGetTypeByMetadataName("System.AttributeUsageAttribute");
        //
        //     var expectedClassAttributes = wellKnowTypeSymbols.Where(x =>
        //     {
        //         var attribute = x.GetAttributes().SingleOrDefault(x => x.AttributeClass?.Equals(attributeUsage) ?? false);
        //
        //         return attribute is { ConstructorArguments: var args }
        //                && ((AttributeTargets)((int)args[0].Value!)).HasFlag(AttributeTargets.Class);
        //     });
        //     
        //     wellKnownTypes.GetClassAttributes().Select(x => x.Name).Should().BeEquivalentTo(expectedClassAttributes.Select(x => x.Name));
        // }
        //
        // [Fact]
        // public void TestGetMemberAttributes()
        // {
        //     Compilation comp = CreateCompilationWithStrongInjectReference("");
        //     Assert.Empty(comp.GetDiagnostics());
        //     Assert.True(WellKnownTypes.TryCreate(comp, x => Assert.False(true, x.ToString()), out var wellKnownTypes));
        //
        //     var wellKnowTypeSymbols = wellKnownTypes
        //         .GetType().GetProperties().Select(x => x.GetValue(wellKnownTypes)).Cast<INamedTypeSymbol>();
        //
        //     var attributeUsage = comp.AssertGetTypeByMetadataName("System.AttributeUsageAttribute");
        //
        //     var expectedMemberAttributes = wellKnowTypeSymbols.Where(x =>
        //     {
        //         var attribute = x.GetAttributes().SingleOrDefault(x => x.AttributeClass?.Equals(attributeUsage) ?? false);
        //
        //         return attribute is { ConstructorArguments: var args }
        //                && (((AttributeTargets)((int)args[0].Value!)) & (AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)) != 0;
        //     });
        //     
        //     wellKnownTypes.GetMemberAttributes().Select(x => x.Name).Should().BeEquivalentTo(expectedMemberAttributes.Select(x => x.Name));
        // }
    }
}