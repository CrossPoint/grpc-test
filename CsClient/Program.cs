using System;
using System.Threading.Tasks;
using CommandLine;
using Grpc.Core;

namespace CsClient
{
    class Program
    {
        public class Options
        {
            [Option('h', "host", Default = "localhost", HelpText = "Hostname or IP address to connect to.")]
            public string Host { get; set; }

            [Option('p', "port", Default = 50051, HelpText = "Port to connect to.")]
            public int Port { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => RunWithOptionsAsync(o).Wait());
        }

        static async Task RunWithOptionsAsync(Options o)
        {
            var channel = new Channel($"{o.Host}:{o.Port}", ChannelCredentials.Insecure);
            var helloProxy = new HelloProxy(channel);

            var response = await helloProxy.SayHelloAsync("Ramon");
            Console.WriteLine("Greeting: " + response);

            using (helloProxy.Subscribe(reply =>
            {
                if (reply != null)
                    Console.WriteLine("Greeting: " + reply.Message);
                else
                    Console.WriteLine("Bye...");
                return Task.CompletedTask;
            }))
            {
                Console.WriteLine("Enter 'start', 'stop' or 'quit'");
                string line;
                while ((line = Console.ReadLine()?.ToLowerInvariant()) != "quit")
                {
                    switch (line)
                    {
                        case "start": await helloProxy.StartAsync(); break;
                        case "stop": await helloProxy.StopAsync(); break;
                        default: Console.WriteLine("???"); break;
                    }
                }
            }

            await channel.ShutdownAsync();
        }
    }
}
