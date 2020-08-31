using System.Collections.Immutable;

namespace StrongInject.Modules
{
    /// <summary>
    /// Provides a registration for <see cref="ImmutableArray{T}"/>.
    /// 
    /// This copies the resolved array into an <see cref="ImmutableArray{T}"/>.
    /// If you require a non-copying implementation for performance, use <see cref="UnsafeImmutableArrayModule"/> instead.
    /// </summary>
    public class SafeImmutableArrayModule
    {
        [Factory(Scope.InstancePerDependency)] public static ImmutableArray<T> CreateImmutableArray<T>(T[] arr) => arr.ToImmutableArray();
    }
}
