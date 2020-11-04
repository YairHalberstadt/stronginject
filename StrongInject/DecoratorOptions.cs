using System;

namespace StrongInject
{
    /// <summary>
    /// Provides options to configure a decorator registration
    /// </summary>
    [Flags]
    public enum DecoratorOptions : long
    {
        Default = 0,

        /// <summary>
        /// Dispose the value returned by this decorator.
        /// 
        /// Apply this only if the decorator itself needs disposal.
        /// The decorator should never dispose the underlying instance.
        /// If a decorator factory returns the same instance as was passed in, this must be false.
        /// </summary>
        Dispose = 1L << 1,
    }
}
