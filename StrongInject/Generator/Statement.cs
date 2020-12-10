using System.Collections.Immutable;

namespace StrongInject.Generator
{
    internal abstract record Statement();
    internal sealed record DependencyCreationStatement(
        string VariableName,
        InstanceSource Source,
        ImmutableArray<string?> Dependencies) : Statement();
    internal sealed record DelegateCreationStatement(
        string VariableName,
        DelegateSource Source,
        ImmutableArray<(string name, InstanceSource source)> SingleInstanceVariablesCreatedEarly) : Statement()
    {
        public string DisposeActionsName { get; } = "disposeActions_" + VariableName;
    }
    internal sealed record SingleInstanceReferenceStatement(string VariableName, InstanceSource Source) : Statement();
}
