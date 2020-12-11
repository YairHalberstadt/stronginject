namespace StrongInject.Generator
{
    internal struct Operation
    {
        public Operation(Statement statement, Disposal? disposal)
        {
            Statement = statement;
            Disposal = disposal;
        }

        public Statement Statement { get; }
        public Disposal? Disposal { get; }
    }
}
