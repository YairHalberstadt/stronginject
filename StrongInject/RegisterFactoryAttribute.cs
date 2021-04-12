using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to register a type implementing <see cref="IFactory{T}"/> or <see cref="IAsyncFactory{T}"/> as a factory for T,
    /// meaning T will be resolved by resolving an instance of the factory, and then calling <see cref="IFactory{T}.Create"/> or <see cref="IAsyncFactory{T}.CreateAsync"/>.
    /// 
    /// It's usually simpler to write a factory method and mark it with the <see cref="FactoryAttribute"/>.
    /// Use this only if you need tight control over the lifetime of your factory, or if you want to control disposal of the factory target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterFactoryAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="factoryType">The factory to register. Must implement <see cref="IFactory{T}"/> or <see cref="IAsyncFactory{T}"/>.</param>
        /// <param name="factoryScope">The scope of the factory - i.e. how often should a new factory be created?</param>
        /// <param name="factoryTargetScope">The scope of the factory target - i.e. how often should <see cref="IFactory{T}.Create"/> or <see cref="IAsyncFactory{T}.CreateAsync"/> be called?</param>
        public RegisterFactoryAttribute(Type factoryType, Scope factoryScope = Scope.InstancePerResolution, Scope factoryTargetScope = Scope.InstancePerResolution)
        {
            FactoryType = factoryType;
            FactoryScope = factoryScope;
            FactoryTargetScope = factoryTargetScope;
        }

        public Type FactoryType { get; }
        public Scope FactoryScope { get; }
        public Scope FactoryTargetScope { get; }
    }
}
