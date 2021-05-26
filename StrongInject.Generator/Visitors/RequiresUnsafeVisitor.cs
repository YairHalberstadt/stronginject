using Microsoft.CodeAnalysis;

namespace StrongInject.Generator.Visitors
{
    internal class RequiresUnsafeVisitor : SimpleVisitor
    {
        private bool _requiresUnsafe = false;

        private RequiresUnsafeVisitor(InstanceSourcesScope containerScope) : base(containerScope)
        {
        }

        public static bool RequiresUnsafe(ITypeSymbol target, InstanceSourcesScope containerScope)
        {
            var visitor = new RequiresUnsafeVisitor(containerScope);
            var state = new State { InstanceSourcesScope = containerScope };
            visitor.VisitCore(visitor.GetInstanceSource(target, state, parameterSymbol: null), state);
            return visitor._requiresUnsafe;
        }

        protected override bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state)
        {
            if (source is null)
                return false;
            if (IsUnsafeType(source.OfType))
            {
                _requiresUnsafe = true;
                ExitFast();
                return false;
            }
            return base.ShouldVisitBeforeUpdateState(source, state);
        }

        private static bool IsUnsafeType(ITypeSymbol type) => type.IsPointerOrFunctionPointer() || type is IArrayTypeSymbol { ElementType: var elementType } && IsUnsafeType(elementType);
    }
}
