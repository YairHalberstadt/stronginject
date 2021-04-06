using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class FactoryOfAttribute : Attribute
    {
        public FactoryOfAttribute(Type type, Scope scope = Scope.InstancePerResolution)
        {
            Type = type;
            Scope = scope;
        }
        public Type Type { get; }
        public Scope Scope { get; }
    }
}
