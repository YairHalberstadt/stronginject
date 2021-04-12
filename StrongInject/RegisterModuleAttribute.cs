using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to import registrations defined on a different type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterModuleAttribute : Attribute
    {
        /// <summary/>
        /// <param name="type">The module to register</param>
        /// <param name="exclusionList">An optional list of types which should not be resolved via this module</param>
        public RegisterModuleAttribute(Type type, params Type[] exclusionList)
        {
            Type = type;
            ExclusionList = exclusionList;
        }

        public Type Type { get; }
        public Type[] ExclusionList { get; }
    }
}
