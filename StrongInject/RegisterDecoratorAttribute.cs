using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegisterDecoratorAttribute : Attribute
    {
        public RegisterDecoratorAttribute(Type type, Type decoratedType) => (Type, DecoratedType) = (type, decoratedType);

        public Type Type { get; }
        public Type DecoratedType { get; }
    }
}
