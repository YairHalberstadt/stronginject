using Confluent.Kafka;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public class KafkaProducer<TKey, TValue> : IProducer<TKey, TValue>
    {
        private readonly Confluent.Kafka.IProducer<TKey, TValue> _producer;
        private readonly string _topic;

        public KafkaProducer(Confluent.Kafka.IProducer<TKey, TValue> producer, string topic)
        {
            _producer = producer;
            _topic = topic;
        }

        public Task Produce(TKey key, TValue value)
        {
            return _producer.ProduceAsync(_topic, new Message<TKey, TValue>() { Key = key, Value = value });
        }
    }
}
