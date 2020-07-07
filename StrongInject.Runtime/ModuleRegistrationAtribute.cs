using System;

namespace StrongInject.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModuleRegistrationAttribute : Attribute
    {
        public ModuleRegistrationAttribute(Type type, params Type[] exclusionList)
        {
            Type = type;
            ExclusionList = exclusionList;
        }

        public Type Type { get; }
        public Type[] ExclusionList { get; }
    }
}
