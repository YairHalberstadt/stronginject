using System;

namespace StrongInject
{
    [Obsolete("Use RegisterAttribute instead", error: true)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegistrationAttribute : Attribute
    {
    }
}
