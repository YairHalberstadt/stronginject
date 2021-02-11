using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace StrongInject.Modules
{
    /// <summary>
    /// Provides a registration for <see cref="ImmutableArray{T}"/>.
    /// 
    /// WARNING:
    /// 
    /// This casts the resolved array directly to an <see cref="ImmutableArray{T}"/>.
    /// This is fine if the user hasn't provided a custom registration for T[], since the array will be unique for this dependency.
    /// However if the user does provide a custom registration, it is possible the ImmutableArray will be mutated by someone holding a reference to the original array.
    /// 
    /// If it's possible a custom registration for T[] exists, use <see cref="SafeImmutableArrayModule"/> instead.
    /// </summary>
    public static class UnsafeImmutableArrayModule
    {
        [Factory(Scope.InstancePerDependency)] public static ImmutableArray<T> UnsafeCreateImmutableArray<T>(T[] arr) => Unsafe.As<T[], ImmutableArray<T>>(ref arr);
    }
}
