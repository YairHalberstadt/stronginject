using Confluent.Kafka;

namespace StrongInject.Samples.ConsoleApp
{
    public class KafkaModule
    {
        [Factory(Scope.SingleInstance)] public static ISerializer<T?> CreateSerializer<T>() => new JsonSerializer<T>();
        [Factory(Scope.SingleInstance)] public static IDeserializer<T?> CreateDeserializer<T>() => new JsonDeserializer<T>();
        [Factory] public static Confluent.Kafka.IConsumer<TKey, TValue> CreateConfluentConsumer<TKey, TValue>(IDeserializer<TKey> keyDeserializer, IDeserializer<TValue> valueDeserializer, Config config)
        {
            return new ConsumerBuilder<TKey, TValue>(new ConsumerConfig() { BootstrapServers = config.BootstrapServers, GroupId = config.GroupId })
                .SetKeyDeserializer(keyDeserializer)
                .SetValueDeserializer(valueDeserializer)
                .Build();
        }
        [Factory] public static Confluent.Kafka.IProducer<TKey, TValue> CreateConfluentProducer<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, Config config)
        {
            return new ProducerBuilder<TKey, TValue>(new ProducerConfig() { BootstrapServers = config.BootstrapServers })
                .SetKeySerializer(keySerializer)
                .SetValueSerializer(valueSerializer)
                .Build();
        }

        [Factory] public static IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(Confluent.Kafka.IConsumer<TKey, TValue> consumer, Config config) => new KafkaConsumer<TKey, TValue>(consumer, config.ConsumedTopic);
        [Factory] public static IProducer<TKey, TValue> CreateProducer<TKey, TValue>(Confluent.Kafka.IProducer<TKey, TValue> producer, string topic) => new KafkaProducer<TKey, TValue>(producer, topic);
    }
}
