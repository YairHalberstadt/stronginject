namespace StrongInject.Generator
{
    internal readonly struct DisposalStyle
    {
        public DisposalStyle(bool isAsync, DisposalStyleDeterminant determinant)
        {
            IsAsync = isAsync;
            Determinant = determinant;
        }

        public bool IsAsync { get; }
        public DisposalStyleDeterminant Determinant { get; }
    }
}
