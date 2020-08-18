using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class InstanceAttribute : Attribute
    {
    }
}
