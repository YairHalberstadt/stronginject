using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace StrongInject.Generator.Tests.Unit
{
    public abstract class TestBase
    {
        protected static Compilation CreateCompilation(string source, params MetadataReference[] metadataReferences)
            => CSharpCompilation.Create("compilation",
        new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
        metadataReferences.Concat(new[] 
        { 
            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
        }),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        protected static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
            => new CSharpGeneratorDriver(c.SyntaxTrees.First().Options,
                ImmutableArray.Create(generators),
                ImmutableArray<AdditionalText>.Empty);

        protected static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(c, generators).RunFullGeneration(c, out var d, out diagnostics);
            return d;
        }
    }
}
