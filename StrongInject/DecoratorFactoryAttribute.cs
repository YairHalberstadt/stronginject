using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DecoratorFactoryAttribute : Attribute
    {
        public DecoratorFactoryAttribute(DecoratorOptions decoratorOptions = DecoratorOptions.Default)
        {
            DecoratorOptions = decoratorOptions;
        }

        public DecoratorOptions DecoratorOptions { get; }
    }
}
