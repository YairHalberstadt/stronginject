using System.Threading.Tasks;

namespace StrongInject
{
    /// <summary>
    /// A type implementing this interface can be registered as a factory for <typeparamref name="T"/>.
    /// It can be registered either via the <see cref="RegisterFactoryAttribute"/>,
    /// or by marking a field/property with the <see cref="InstanceAttribute"/> and configuring it's <see cref="InstanceAttribute.Options"/>.
    /// 
    /// In general it's easier to use factory methods instead (methods marked with the <see cref="FactoryAttribute"/>.
    /// Only use this if you either need control over the lifetime of the factory,
    /// or you need to control disposal of the created type.
    /// 
    /// For example, you may want to implement this if you would like to cache and reuse instances after they've been released.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFactory<T>
    {
        T Create();

        void Release(T instance)
#if !NETSTANDARD2_0
            => Helpers.Dispose(instance)
#endif
            ;
    }

    /// <summary>
    /// A type implementing this interface can be registered as an async factory for <typeparamref name="T"/>.
    /// It can be registered either via the <see cref="RegisterFactoryAttribute"/>,
    /// or by marking a field/property with the <see cref="InstanceAttribute"/> and configuring it's <see cref="InstanceAttribute.Options"/>.
    /// 
    /// In general it's easier to use factory methods instead (methods marked with the <see cref="FactoryAttribute"/>.
    /// Only use this if you either need control over the lifetime of the factory,
    /// or you need to control disposal of the created type.
    /// 
    /// For example, you may want to implement this if you would like to cache and reuse instances after they've been released.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncFactory<T>
    {
        ValueTask<T> CreateAsync();

        ValueTask ReleaseAsync(T instance)
#if !NETSTANDARD2_0
            => Helpers.DisposeAsync(instance)
#endif
            ;
    }
}
