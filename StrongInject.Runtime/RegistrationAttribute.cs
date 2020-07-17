using System;

namespace StrongInject.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegistrationAttribute : Attribute
    {
        public RegistrationAttribute(Type type, params Type[] registeredAs) : this(type, Scope.InstancePerResolution, registeredAs)
        {
        }

        public RegistrationAttribute(Type type, Scope scope, params Type[] registeredAs)
        {
            Type = type;
            RegisteredAs = registeredAs;
            Scope = scope;
        }

        public RegistrationAttribute(Type type, Scope scope, Scope factoryTargetScope, params Type[] registeredAs)
        {
            Type = type;
            RegisteredAs = registeredAs;
            Scope = scope;
            FactoryScope = factoryTargetScope;
        }

        public Type Type { get; }
        public Type[] RegisteredAs { get; }
        public Scope Scope { get; }
        public Scope FactoryScope { get; }
    }
}
