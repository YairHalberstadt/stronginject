using System;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    public class App
    {
        private readonly IConsumer<User, Message> _consumer;
        private readonly Cache<string, IProducer<User, Message>> _producerCache;
        private readonly string _targetTopicPrefix;

        public App(IConsumer<User, Message> consumer, Cache<string, IProducer<User, Message>> producerCache, string targetTopicPrefix)
        {
            _consumer = consumer;
            _producerCache = producerCache;
            _targetTopicPrefix = targetTopicPrefix;
        }

        public async Task FanMessagesToRecipients()
        {
            await foreach (var commitableMessage in _consumer.Consume())
            {
                var (key, value) = (commitableMessage.Key, commitableMessage.Value);
                Console.WriteLine($"Forwarding message from {key.Name} to {value.Recipient.Name}");
                await _producerCache.Get(_targetTopicPrefix + value.Recipient.Id).Produce(key, value);
                commitableMessage.Commit();
            }
        }
    }
}
