using Microsoft.CodeAnalysis;

namespace StrongInject.Generator
{
    internal abstract record Disposal(bool IsAsync)
    {
        internal sealed record IDisposable(string VariableName, bool IsAsync) : Disposal(IsAsync);
        internal sealed record DisposalHelpers(string VariableName, bool IsAsync) : Disposal(IsAsync);
        internal sealed record FactoryDisposal(string VariableName, string FactoryName, bool IsAsync) : Disposal(IsAsync);
        internal sealed record DelegateDisposal(string DisposeActionsName, string DisposeActionsTypeName, bool IsAsync) : Disposal(IsAsync);
    };
}
