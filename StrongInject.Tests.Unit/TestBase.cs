﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public abstract class TestBase
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestBase(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        protected static Compilation CreateCompilation(string source, params MetadataReference[] metadataReferences)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                metadataReferences.Concat(new[] 
                { 
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ConcurrentBag<>).Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
                }),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        protected static GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators)
            => CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options);

        protected Compilation RunGeneratorWithStrongInjectReference(string source, out ImmutableArray<Diagnostic> diagnostics, out ImmutableArray<string> generatedFiles)
        {
            var location = Regex
                .Replace(typeof(IContainer<>).Assembly.Location, "stronginject[/|\\\\].*[/|\\\\]bin[/|\\\\]", "stronginject/StrongInject/bin/")
                .Replace("net5.0", "netstandard2.1");
            var reference = MetadataReference.CreateFromFile(location);
            return RunGenerator(source, out diagnostics, out generatedFiles, reference);
        }
        protected Compilation RunGenerator(string source, out ImmutableArray<Diagnostic> diagnostics, out ImmutableArray<string> generatedFiles, params MetadataReference[] metadataReferences)
        {
            var compilation = CreateCompilation(source, metadataReferences);
            CreateDriver(compilation, new SourceGenerator()).RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var duplicatedDiagnostics);
            diagnostics = duplicatedDiagnostics.Distinct().ToImmutableArray();
            var generatedTrees = updatedCompilation.SyntaxTrees.Where(x => !compilation.SyntaxTrees.Any(y => y.Equals(x))).ToImmutableArray();
            foreach (var generated in generatedTrees)
            {
                _outputHelper.WriteLine($@"{generated.FilePath}:
{generated.GetText()}");
            }
            generatedFiles = generatedTrees.Select(x => x.GetText().ToString()).ToImmutableArray();
            return updatedCompilation;
        }
    }
}
