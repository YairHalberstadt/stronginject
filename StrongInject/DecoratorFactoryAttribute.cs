using System;

namespace StrongInject
{
    /// <summary>
    /// Use this to mark a method as a decorater for it's return type.
    /// Exactly one of the parameters must be the same type as the return type.
    /// When resolving an instance of the return type, once the type has been resolved as normal it will be passed as a parameter to the method.
    /// You can mark both normal methods and generic methods with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DecoratorFactoryAttribute : Attribute
    {
        public DecoratorFactoryAttribute(DecoratorOptions decoratorOptions = DecoratorOptions.Default)
        {
            DecoratorOptions = decoratorOptions;
        }

        public DecoratorOptions DecoratorOptions { get; }
    }
}
