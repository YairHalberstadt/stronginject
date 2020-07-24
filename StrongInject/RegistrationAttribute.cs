using System;

namespace StrongInject
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

        public Type Type { get; }
        public Type[] RegisteredAs { get; }
        public Scope Scope { get; }
    }
}
