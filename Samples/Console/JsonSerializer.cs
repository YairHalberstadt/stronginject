using Confluent.Kafka;
using System;
using System.Text.Json;

namespace StrongInject.Samples.ConsoleApp
{
    public class JsonSerializer<T> : ISerializer<T?>
    {
        public byte[] Serialize(T? data, SerializationContext context)
        {
            return data is null ? Array.Empty<byte>() : JsonSerializer.SerializeToUtf8Bytes(data);
        }
    }
}
