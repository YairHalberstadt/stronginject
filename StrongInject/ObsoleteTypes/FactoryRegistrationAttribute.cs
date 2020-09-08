using System;
using System.ComponentModel;

namespace StrongInject
{
    [Obsolete("Use RegisterFactoryAttribute instead", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class FactoryRegistrationAttribute : Attribute
    {
    }
}
