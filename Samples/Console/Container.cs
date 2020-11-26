using System;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{

    [RegisterModule(typeof(KafkaModule))]
    [Register(typeof(JsonConfigLoader), Scope.SingleInstance, typeof(IConfigLoader))]
    public partial class Container : IAsyncContainer<App>
    {
        [Factory] private App CreateApp(IConsumer<User, Message> consumer, Cache<string, IProducer<User, Message>> producerCache, Config config)
            => new App(consumer, producerCache, config.TargetTopicPrefix);

        [Factory(Scope.SingleInstance)] ValueTask<Config> CreateConfig(IConfigLoader configLoader) => configLoader.LoadConfig();

        [Factory] Cache<TKey, TValue> CreateCache<TKey, TValue>(Func<TKey, TValue> factory) where TKey : notnull => new Cache<TKey, TValue>(factory);
    }
}
