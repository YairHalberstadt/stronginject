using Microsoft.CodeAnalysis;
using System.Threading;

namespace StrongInject.Generator.Visitors
{
    internal class RequiresUnsafeVisitor : SimpleVisitor
    {
        private bool _requiresUnsafe = false;

        private RequiresUnsafeVisitor(InstanceSourcesScope containerScope, CancellationToken cancellationToken) : base(containerScope, cancellationToken)
        {
        }

        public static bool RequiresUnsafe(ITypeSymbol target, InstanceSourcesScope containerScope, CancellationToken cancellationToken)
        {
            var visitor = new RequiresUnsafeVisitor(containerScope, cancellationToken);
            var state = new State(containerScope);
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
