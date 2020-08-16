using System;

namespace StrongInject
{
    [Obsolete("Use RegisterFactoryAttribute instead", error: true)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class FactoryRegistrationAttribute : Attribute
    {
    }
}
