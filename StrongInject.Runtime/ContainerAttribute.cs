using System;

namespace StrongInject.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ContainerAttribute : Attribute
    {
    }
}
