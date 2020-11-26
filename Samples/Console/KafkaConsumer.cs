using Confluent.Kafka;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public class KafkaConsumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
        private readonly Confluent.Kafka.IConsumer<TKey, TValue> _consumer;
        private readonly string _topic;

        public KafkaConsumer(Confluent.Kafka.IConsumer<TKey, TValue> consumer, string topic)
        {
            _consumer = consumer;
            _topic = topic;
        }

        public async IAsyncEnumerable<ICommitable<TKey, TValue>> Consume()
        {
            _consumer.Subscribe(_topic);

            while (true)
            {
                var result = _consumer.Consume();
                if (result.IsPartitionEOF)
                {
                    await Task.Delay(1000);
                }
                else
                {
                    yield return new Commitable(result, _consumer);
                }
            }
        }

        private class Commitable : ICommitable<TKey, TValue>
        {
            private readonly Confluent.Kafka.IConsumer<TKey, TValue> _consumer;
            private readonly ConsumeResult<TKey, TValue> _consumeResult;

            public Commitable(ConsumeResult<TKey, TValue> consumeResult, Confluent.Kafka.IConsumer<TKey, TValue> consumer)
            {
                _consumeResult = consumeResult;
                _consumer = consumer;
            }

            public TKey Key => _consumeResult.Message.Key;

            public TValue Value => _consumeResult.Message.Value;

            public void Commit()
            {
                _consumer.Commit(_consumeResult);
            }
        }
    }
}
