using System;

namespace StrongInject
{
    [Obsolete("Use RegisterModuleAttribute instead", error: true)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModuleRegistrationAttribute : Attribute
    {
    }
}
