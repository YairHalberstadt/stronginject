using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RegisterModuleAttribute : Attribute
    {
        public RegisterModuleAttribute(Type type, params Type[] exclusionList)
        {
            Type = type;
            ExclusionList = exclusionList;
        }

        public Type Type { get; }
        public Type[] ExclusionList { get; }
    }
}
