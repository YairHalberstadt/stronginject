using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using StrongInject.Runtime;
using System.Linq;

namespace StrongInject.Generator
{
    [Generator]
    public class ContainerSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            var cancellationToken = context.CancellationToken;
            var containerAttribute = context.Compilation.GetTypeByMetadataName(typeof(ContainerAttribute).FullName);
            if (containerAttribute is null)
                return;
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
                var containers = syntaxTree.GetRoot(cancellationToken).DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>()
                    .Select(x => (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(x, cancellationToken))
                    .Where(x => x.GetAttributes().Any(x => x.AttributeClass?.Equals(containerAttribute, SymbolEqualityComparer.Default) ?? false));

                foreach (var container in containers)
                {
                    
                };
            }
            context.Compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf()).OfType<ClassDeclarationSyntax>();
        }

        public void Initialize(InitializationContext context)
        {
        }
    }
}
