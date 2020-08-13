using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class FactoryAttribute : Attribute
    {
        public FactoryAttribute(Scope scope = Scope.InstancePerResolution)
        {
            Scope = scope;
        }
        public Scope Scope { get; }
    }
}
