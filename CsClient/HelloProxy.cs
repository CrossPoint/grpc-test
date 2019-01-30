using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace CsClient
{
    public class HelloProxy
    {
        private static readonly Empty Empty = new Empty();

        private readonly Greeter.GreeterClient _client;
        private readonly GrpcEvent<HelloEvent> _onHello = new GrpcEvent<HelloEvent>();

        public HelloProxy(Channel channel)
        {
            _client = new Greeter.GreeterClient(channel);
        }

        public event EventHandler<GrpcEvent<HelloEvent>.Args> OnHello
        {
            add => _onHello.Add(value, ct => _client.Subscribe(Empty, cancellationToken: ct));
            remove => _onHello.Remove(value);
        }

        public async Task<string> SayHelloAsync(string name, CancellationToken cancellationToken = default)
        {
            var request = new HelloRequest { Name = name };
            var reply = await _client.SayHelloAsync(request, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
            return reply.Message;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
            => await _client.StartAsync(Empty, cancellationToken: cancellationToken);

        public async Task StopAsync(CancellationToken cancellationToken = default)
            => await _client.StopAsync(Empty, cancellationToken: cancellationToken);
    }
}
