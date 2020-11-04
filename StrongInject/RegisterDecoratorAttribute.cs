using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegisterDecoratorAttribute : Attribute
    {
        public RegisterDecoratorAttribute(Type type, Type decoratedType, DecoratorOptions decoratorOptions = DecoratorOptions.Default)
        {
            Type = type;
            DecoratedType = decoratedType;
            DecoratorOptions = decoratorOptions;
        }

        public Type Type { get; }
        public Type DecoratedType { get; }
        public DecoratorOptions DecoratorOptions { get; }
    }
}
