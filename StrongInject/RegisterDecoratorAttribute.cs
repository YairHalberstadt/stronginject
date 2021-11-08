using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to register a decorator for a type.
    /// When resolving an instance of the type, once the decorated type has been resolved as normal it will be wrapped by an instance of the decorator.
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
    
    /// <summary>
    /// Use this attribute to register a decorator for a type.
    /// When resolving an instance of the type, once the decorated type has been resolved as normal it will be wrapped by an instance of the decorator.
    /// <typeparam name="TDecorator">The type to use as a decorator. Must be a subtype of <typeparamref name="TDecorated"/>.</typeparam>
    /// <typeparam name="TDecorated">The type which will be decorated (wrapped) by <typeparamref name="TDecorator"/>.</typeparam>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterDecoratorAttribute<TDecorator, TDecorated> : Attribute where TDecorator : TDecorated
    {
        public RegisterDecoratorAttribute(DecoratorOptions decoratorOptions = DecoratorOptions.Default)
        {
            DecoratorOptions = decoratorOptions;
        }
        
        public DecoratorOptions DecoratorOptions { get; }
    }
}
