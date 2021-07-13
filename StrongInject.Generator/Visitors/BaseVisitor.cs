using Microsoft.CodeAnalysis;
using System;

namespace StrongInject.Generator.Visitors
{
    internal abstract class BaseVisitor<State> : IVisitor<State> where State : struct, BaseVisitor<State>.IState
    {
        private bool _exitFast = false;
        protected void ExitFast() => _exitFast = true;
        public void VisitCore(InstanceSource? source, State state)
        {
            if (!_exitFast && ShouldVisitBeforeUpdateState(source, state) && source is not null)
            {
                UpdateState(source, ref state);
                if (ShouldVisitAfterUpdateState(source, state))
                {
                    source.Visit(this, state);
                    AfterVisit(source, state);
                }
            }
        }
        protected virtual void UpdateState(InstanceSource source, ref State state)
        {
            state.InstanceSourcesScope = state.InstanceSourcesScope.Enter(source);
        }
        protected abstract bool ShouldVisitBeforeUpdateState(InstanceSource? source, State state);
        protected abstract bool ShouldVisitAfterUpdateState(InstanceSource source, State state);
        protected virtual void AfterVisit(InstanceSource source, State state) { }

        protected virtual InstanceSource? GetInstanceSource(ITypeSymbol type, State state, IParameterSymbol? parameterSymbol)
        {
            if (parameterSymbol is not null)
            {
                return state.InstanceSourcesScope.GetParameterSource(parameterSymbol);
            }
            else return state.InstanceSourcesScope[type];
        }

        public interface IState
        {
            InstanceSourcesScope InstanceSourcesScope { get; set; }
        }

        public virtual void Visit(Registration registration, State state)
        {
            foreach (var param in registration.Constructor.Parameters)
            {
                if (_exitFast)
                    return;
                VisitCore(GetInstanceSource(param.Type, state, param), state);
            }
        }

        public virtual void Visit(FactorySource factorySource, State state)
        {
            VisitCore(factorySource.Underlying, state);
        }

        public virtual void Visit(DelegateSource delegateSource, State state)
        {
            VisitCore(GetInstanceSource(delegateSource.ReturnType, state, null), state);
        }

        public virtual void Visit(DelegateParameter delegateParameter, State state)
        {
        }

        public virtual void Visit(FactoryMethod factoryMethod, State state)
        {
            foreach (var param in factoryMethod.Method.Parameters)
            {
                if (_exitFast)
                    return;
                VisitCore(GetInstanceSource(param.Type, state, param), state);
            }
        }

        public virtual void Visit(InstanceFieldOrProperty instanceFieldOrProperty, State state)
        {
        }

        public virtual void Visit(ArraySource arraySource, State state)
        {
            foreach (var item in arraySource.Items)
            {
                if (_exitFast)
                    return;
                VisitCore(item, state);
            }
        }

        public virtual void Visit(WrappedDecoratorInstanceSource wrappedDecoratorInstanceSource, State state)
        {
            var parameters = wrappedDecoratorInstanceSource.Decorator switch
            {
                DecoratorRegistration { Constructor: { Parameters: var prms } } => prms,
                DecoratorFactoryMethod { Method: { Parameters: var prms } } => prms,
                var decoratorSource => throw new NotImplementedException(decoratorSource.GetType().ToString()),
            };
            var decoratedParameterOrdinal = wrappedDecoratorInstanceSource.Decorator.DecoratedParameter;
            foreach (var param in parameters)
            {
                if (_exitFast)
                    return;
                var paramSource = param.Ordinal == decoratedParameterOrdinal
                    ? wrappedDecoratorInstanceSource.Underlying
                    : GetInstanceSource(param.Type, state, param);
                VisitCore(paramSource, state);
            }
        }

        public virtual void Visit(ForwardedInstanceSource forwardedInstanceSource, State state)
        {
            VisitCore(forwardedInstanceSource.Underlying, state);
        }
    }
}
