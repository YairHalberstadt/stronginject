using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrongInject.Generator.Visitors
{
    internal interface IVisitor<TState>
    {
        void Visit(Registration registration, TState state);
        void Visit(FactorySource factorySource, TState state);
        void Visit(DelegateSource delegateSource, TState state);
        void Visit(DelegateParameter delegateParameter, TState state);
        void Visit(FactoryMethod factoryMethod, TState state);
        void Visit(InstanceFieldOrProperty instanceFieldOrProperty, TState state);
        void Visit(ArraySource arraySource, TState state);
        void Visit(WrappedDecoratorInstanceSource wrappedDecoratorInstanceSource, TState state);
        void Visit(ForwardedInstanceSource forwardedInstanceSource, TState state);
    }
}
