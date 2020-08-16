using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegisterAttribute : Attribute
    {
        public RegisterAttribute(Type type, params Type[] registerAs) : this(type, Scope.InstancePerResolution, registerAs)
        {
        }

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
}
