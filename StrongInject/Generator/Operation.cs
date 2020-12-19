using System.Collections.Generic;

namespace StrongInject.Generator
{
    internal class Operation
    {
        public Operation(Statement statement, Disposal? disposal, List<Operation> dependencies, AwaitStatement? awaitStatement = null)
        {
            Statement = statement;
            Disposal = disposal;
            Dependencies = dependencies;
            AwaitStatement = awaitStatement;
        }

        public Statement Statement { get; }
        public Disposal? Disposal { get; }
        public AwaitStatement? AwaitStatement { get; }
        public List<Operation> Dependencies { get; }
    }
}
