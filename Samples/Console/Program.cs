using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StrongInject.Samples.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var dockerProcess = StartDockerContainers();
            {
                await CreateKafkaTopicAndStartWritingMessagesToIt();

                await using (var container = new Container())
                {
                    await container.RunAsync(x => new ValueTask(x.FanMessagesToRecipients()));
                }
            }
        }

        private static Process StartDockerContainers()
        {
            var processInfo = new ProcessStartInfo("docker-compose", "up");

            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            var process = new Process();
            process.StartInfo = processInfo;
            process.OutputDataReceived += new DataReceivedEventHandler((o, e) => Console.WriteLine($"docker-compose: {e.Data}"));
            process.ErrorDataReceived += new DataReceivedEventHandler((o, e) => Console.WriteLine($"docker-compose: {e.Data}"));

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private static async Task CreateKafkaTopicAndStartWritingMessagesToIt()
        {
            var config = await new JsonConfigLoader().LoadConfig();
            using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = config.BootstrapServers }).Build())
            {
                try
                {
                    await adminClient.CreateTopicsAsync(new TopicSpecification[]
                    {
                            new TopicSpecification { Name = config.ConsumedTopic, ReplicationFactor = 1, NumPartitions = 1 }
                    }, new CreateTopicsOptions { RequestTimeout = TimeSpan.FromMinutes(10)});
                }
                catch (CreateTopicsException e)
                {
                    Console.WriteLine($"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
                }
            }

            var producer = new ProducerBuilder<User, Message>(new ProducerConfig { BootstrapServers = config.BootstrapServers })
                .SetKeySerializer(new JsonSerializer<User>()!)
                .SetValueSerializer(new JsonSerializer<Message>()!)
                .Build();

            _ = Task.Run(async () =>
            {
                var firstNames = new[]
                {
                        "Taylor",
                        "Rosie",
                        "Josie",
                        "Aiden",
                        "Scarlet",
                        "Violet",
                        "Morgan",
                        "Abe",
                        "Shaun",
                        "Atif",
                    };

                var lastNames = new[]
                {
                        "Mcdonald",
                        "Berry",
                        "Fernandez",
                        "Thompson",
                        "Davidson",
                        "Ramirez",
                        "Stevenson",
                        "Burns",
                        "Robinson",
                        "Kirby",
                    };

                var random = new Random();
                var md5 = MD5.Create();

                while (true)
                {
                    var senderKey = random.Next(0, 100);
                    var senderName = firstNames[senderKey / 10] + " " + lastNames[senderKey % 10];
                    var senderId = md5.ComputeHash(Encoding.Default.GetBytes(senderName));
                    var sender = new User
                    {
                        Id = new Guid(senderId),
                        Name = senderName,
                    };

                    var recipientKey = random.Next(0, 100);
                    var recipientName = firstNames[recipientKey / 10] + " " + lastNames[recipientKey % 10];
                    var recipientId = md5.ComputeHash(Encoding.Default.GetBytes(recipientName));
                    var recipient = new User
                    {
                        Id = new Guid(recipientId),
                        Name = recipientName,
                    };

                    var message = new Message
                    {
                        Content = $"Hi {recipientName}, it's {senderName}",
                        Recipient = recipient,
                    };

                    await producer.ProduceAsync(config.ConsumedTopic, new Message<User, Message> { Key = sender, Value = message });
                }
            });
        }
    }
}
