using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegisterFactoryAttribute : Attribute
    {
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
