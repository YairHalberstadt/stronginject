using System;

namespace StrongInject
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class InstanceAttribute : Attribute
    {
        public InstanceAttribute(Options options = Options.Default)
        {
            Options = options;
        }

        public Options Options { get; }
    }
}
