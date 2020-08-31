using System;

namespace StrongInject.Modules
{
    /// <summary>
    /// Provides a registration for <see cref="Lazy{T}"/>.
    /// </summary>
    public class LazyModule
    {
        [Factory(Scope.InstancePerResolution)] public static Lazy<T> CreateLazy<T>(Func<T> func) => new Lazy<T>(func);
    }
}
