namespace StrongInject.Generator
{
    internal struct Operation
    {
        public Operation(Statement statement, Disposal? disposal, AwaitStatement? awaitStatement = null)
        {
            Statement = statement;
            Disposal = disposal;
            AwaitStatement = awaitStatement;
        }

        public Statement Statement { get; }
        public Disposal? Disposal { get; }
        public AwaitStatement? AwaitStatement { get; }
    }
}
