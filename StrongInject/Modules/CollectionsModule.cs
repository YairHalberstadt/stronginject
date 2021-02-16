using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace StrongInject.Modules
{
    /// <summary>
    /// Provides registrations for <see cref="IEnumerable{T}"/>, <see cref="IReadOnlyList{T}"/> and <see cref="IReadOnlyCollection{T}"/>.
    /// </summary>
    public static class CollectionsModule
    {
        [Factory(Scope.InstancePerDependency)] public static IEnumerable<T> CreateEnumerable<T>(T[] arr) => arr;
        [Factory(Scope.InstancePerDependency)] public static IReadOnlyList<T> CreateReadOnlyList<T>(T[] arr) => arr;
        [Factory(Scope.InstancePerDependency)] public static IReadOnlyCollection<T> CreateReadOnlyCollection<T>(T[] arr) => arr;
    }
}
