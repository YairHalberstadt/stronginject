using System;

namespace StrongInject
{
    /// <summary>
    /// Use this to mark a method as a factory.
    /// The method will be used to resolve instances of its return type.
    /// You can mark both normal methods and generic methods with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class FactoryAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope">The scope of each instance resolved from the method - i.e. how often the method will be called.</param>
        public FactoryAttribute(Scope scope = Scope.InstancePerResolution)
        {
            Scope = scope;
        }
        public Scope Scope { get; }
    }
}
