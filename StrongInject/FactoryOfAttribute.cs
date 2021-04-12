using System;

namespace StrongInject
{
    /// <summary>
    /// Use this to mark a generic method as a factory, but only to be used to resolve instances of <see cref="Type"/>.
    /// <see cref="Type"/> can be a concrete type or an open generic.
    /// You can mark a method with this attribute multiple times.
    /// Do not also mark it with the <see cref="FactoryAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class FactoryOfAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type which the method should be used to resolve</param>
        /// <param name="scope">The scope of each instance resolved from the method - i.e. how often the method will be called.</param>
        public FactoryOfAttribute(Type type, Scope scope = Scope.InstancePerResolution)
        {
            Type = type;
            Scope = scope;
        }
        public Type Type { get; }
        public Scope Scope { get; }
    }
}
