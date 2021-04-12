using System;

namespace StrongInject
{
    /// <summary>
    /// Use this attribute to register a field or property, so it can be used in resolution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class InstanceAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options">Options to configure how the field/property should be registered.
        /// See the documentation on <see cref="StrongInject.Options"/>.</param>
        public InstanceAttribute(Options options = Options.Default)
        {
            Options = options;
        }

        public Options Options { get; }
    }
}
