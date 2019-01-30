using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Grpc.Core;
using Helloworld;

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
            Thread.Sleep(2500);
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => RunWithOptionsAsync(o).Wait());
        }

        static void OnHelloHandler(object sender, GrpcEvent<HelloEvent>.Args args)
        {
            Console.WriteLine("Greeting: " + args.Data.Message);
        }

        static async Task RunWithOptionsAsync(Options o)
        {
            var channel = new Channel($"{o.Host}:{o.Port}", ChannelCredentials.Insecure);

            var helloProxy = new HelloProxy(channel);
            helloProxy.OnHello += OnHelloHandler;

            try
            {
                var response = await helloProxy.SayHelloAsync("Ramon");
                Console.WriteLine("Greeting: " + response);
                Console.WriteLine("Enter 'start', 'stop' or 'quit'");

                string line;
                while ((line = Console.ReadLine()?.ToLowerInvariant()) != "quit")
                {
                    switch (line)
                    {
                        case "start":
                            await helloProxy.StartAsync();
                            break;
                        case "stop":
                            await helloProxy.StopAsync();
                            break;
                        default:
                            Console.WriteLine("???");
                            break;
                    }
                }
            }
            finally
            {
                helloProxy.OnHello -= OnHelloHandler;
            }

            await channel.ShutdownAsync();
        }
    }
}
