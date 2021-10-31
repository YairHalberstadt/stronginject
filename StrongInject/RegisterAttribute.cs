using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to register a type with StrongInject.
    /// The type must have either exactly one public constructor, or exactly two public constructors, one of which is parameterless.
    /// By default the type will be registered as an instance of itself. You can modify it to be registered as any of its supertypes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type to be registered</param>
        /// <param name="registerAs">An optional list of types for it to be registered as an instance of.
        /// If left empty it will be registered as itself.
        /// If not left empty you will have to explicitly register it as itself if desired.
        /// All types must be supertypes of <paramref name="type"/></param>
        public RegisterAttribute(Type type, params Type[] registerAs) : this(type, Scope.InstancePerResolution, registerAs)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The type to be registered</param>
        /// <param name="scope">The scope of each instance of <paramref name="type"/> - i.e. how often will a new instance be created.</param>
        /// <param name="registerAs">An optional list of types for it to be registered as an instance of.
        /// If left empty it will be registered as itself.
        /// If not left empty you will have to explicitly register it as itself if desired.
        /// All types must be supertypes of</param>
        public RegisterAttribute(Type type, Scope scope, params Type[] registerAs)
        {
            Type = type;
            RegisterAs = registerAs;
            Scope = scope;
        }

        public Type Type { get; }
        public Type[] RegisterAs { get; }
        public Scope Scope { get; }
    }
    
    /// <summary>
    /// Use this attribute to register <typeparamref name="T"/> with StrongInject.
    /// The type must have either exactly one public constructor, or exactly two public constructors, one of which is parameterless.
    /// The type will be registered as an instance of itself.
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAttribute<T> : Attribute
    {
        /// <param name="scope">The scope of each instance of <typeparamref name="T"/> - i.e. how often will a new instance be created.</param>
        public RegisterAttribute(Scope scope = Scope.InstancePerResolution)
        {
            Scope = scope;
        }
        
        public Scope Scope { get; }
    }
    
    /// <summary>
    /// Use this attribute to register <typeparamref name="TImpl"/> with StrongInject as an instance of <typeparamref name="TService"/>.
    /// The type must have either exactly one public constructor, or exactly two public constructors, one of which is parameterless.
    /// </summary>
    /// <typeparam name="TImpl">The type to register</typeparam>
    /// <typeparam name="TService">The type to register <typeparamref name="TImpl"/> as an instance of</typeparam>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterAttribute<TImpl, TService> : Attribute where TImpl : TService
    {
        /// <param name="scope">The scope of each instance of <typeparamref name="TImpl"/> - i.e. how often will a new instance be created.</param>
        public RegisterAttribute(Scope scope = Scope.InstancePerResolution)
        {
            Scope = scope;
        }
        
        public Scope Scope { get; }
    }
}
