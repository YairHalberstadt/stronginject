using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace StrongInject.Generator.Tests.Unit
{
    public static class RoslynExtensions
    {
        public static INamedTypeSymbol AssertGetTypeByMetadataName(this Compilation compilation, string name)
        {
            var type = compilation.GetTypeByMetadataName(name);
            Assert.NotNull(type);
            return type!;
        }

        public static void Verify(
            this IEnumerable<Diagnostic> diagnostics,
            params DiagnosticResult[] expected) => DiagnosticVerifier.VerifyDiagnostics(diagnostics, expected);
    }
}