using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal class Operation
    {
        public Operation(Statement statement, Disposal? disposal, List<Operation> dependencies, bool canDisposeLocally, AwaitStatement? awaitStatement = null)
        {
            Statement = statement;
            Disposal = disposal;
            Dependencies = dependencies;
            AwaitStatement = awaitStatement;
            CanDisposeLocally = canDisposeLocally;
        }

        public Statement Statement { get; }
        public Disposal? Disposal { get; }
        public AwaitStatement? AwaitStatement { get; }
        public bool CanDisposeLocally { get; }
        public List<Operation> Dependencies { get; }
    }

    internal static class OperationExtensions
    {
        public static bool CanDisposeAwaitStatementResultLocally(this Operation operation)
        {
            if (operation.Statement is not AwaitStatement { VariableName: not null })
                return false;
            var originalOperation = operation.Dependencies[0];
            return originalOperation is InitializationStatement ? originalOperation.Dependencies[0].CanDisposeLocally : originalOperation.CanDisposeLocally;
        }
    }
}
