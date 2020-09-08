using System;
using System.ComponentModel;

namespace StrongInject
{
    [Obsolete("Use RegisterModuleAttribute instead", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModuleRegistrationAttribute : Attribute
    {
    }
}
