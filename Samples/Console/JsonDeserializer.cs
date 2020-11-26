using Confluent.Kafka;
using System;
using System.Text.Json;

namespace StrongInject.Samples.ConsoleApp
{
    public class JsonDeserializer<T> : IDeserializer<T?>
    {
        public T? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
                return default;
            return JsonSerializer.Deserialize<T>(data);
        }
    }
}
