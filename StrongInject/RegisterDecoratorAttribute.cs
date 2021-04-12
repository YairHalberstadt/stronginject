using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to register a decorator for a type.
    /// When resolving an instance of the type, once the type has been resolved as normal it will be wrapped by an instance of the decorator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterDecoratorAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type to use as a decorator. Must be a subtype of <paramref name="decoratedType"/>.</param>
        /// <param name="decoratedType">The type which will be decorated (wrapped) by <paramref name="type"/>.</param>
        /// <param name="decoratorOptions"></param>
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
