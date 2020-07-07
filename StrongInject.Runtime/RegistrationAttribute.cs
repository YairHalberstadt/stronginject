using System;

namespace StrongInject.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegistrationAttribute : Attribute
    {
        public RegistrationAttribute(Type type, params Type[] registeredAs) : this(type, Lifetime.InstancePerDependency, registeredAs)
        {
        }

        public RegistrationAttribute(Type type, Lifetime lifetime, params Type[] registeredAs)
        {
            Type = type;
            RegisteredAs = registeredAs;
            Lifetime = lifetime;
        }

        public Type Type { get; }
        public Type[] RegisteredAs { get; }
        public Lifetime Lifetime { get; }
    }
}
