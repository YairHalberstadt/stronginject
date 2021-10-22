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
        /// <param name="scope">The scope of each instance resolved from the method - i.e. how often the method will be called.</param>
        public FactoryAttribute(Scope scope = Scope.InstancePerResolution)
        {
            Scope = scope;
            AsTypes = Array.Empty<Type>();
        }
        
        /// <param name="scope">The scope of each instance resolved from the method - i.e. how often the method will be called.</param>
        /// <param name="asTypes">An optional list of types for this to be used as the factory of.
        /// If left empty it will be used as a factory of the return type.
        /// If not left empty you will have to explicitly register it as the return type if desired.
        /// All types must be supertypes of the return type.</param>
        public FactoryAttribute(Scope scope, params Type[] asTypes)
        {
            Scope = scope;
            AsTypes = asTypes;
        }
        
        /// <param name="asTypes">An optional list of types for this to be used as the factory of.
        /// If left empty it will be used as a factory of the return type.
        /// If not left empty you will have to explicitly register it as the return type if desired.
        /// All types must be supertypes of the return type.</param>
        public FactoryAttribute(params Type[] asTypes)
        {
            AsTypes = asTypes;
        }
        
        public Scope Scope { get; }
        public Type[] AsTypes { get; }
    }
}
