using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;
using Lamar;

namespace CsServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var container = Configure(args))
            {
                var server = container.GetInstance<Server>();
                server.Start();

                var ports = string.Join(", ", server.Ports.Select(p => $"{p.Host}:{p.BoundPort}"));
                Console.WriteLine($"Greeter server listening on {ports}");
                await Task.Delay(Timeout.InfiniteTimeSpan);

                await server.ShutdownAsync();
            }
        }

        public static IContainer Configure(string[] args)
        {
            return new Container(c =>
            {
                // Add all service definition implementations
                c.For<ServerServiceDefinition>().Add(ctx => Greeter.BindService(ctx.GetInstance<GreeterImpl>()));

                // Add all the ports that the server is listening on
                c.For<ServerPort>().Add(ctx => new ServerPort("0.0.0.0", 50051, ServerCredentials.Insecure));
                
                // Add Server description
                c.For<Server>().Use(ctx =>
                {
                    var server = new Server();
                    foreach (var service in ctx.GetAllInstances<ServerServiceDefinition>())
                        server.Services.Add(service);
                    foreach (var port in ctx.GetAllInstances<ServerPort>())
                        server.Ports.Add(port);
                    return server;
                });
            });
        }
    }
}
