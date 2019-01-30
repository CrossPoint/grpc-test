using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Helloworld;

namespace CsServer
{
    public class GreeterImpl : Greeter.GreeterBase
    {
        private readonly SubscribeList<HelloEvent> _helloSubscribers = new SubscribeList<HelloEvent>();
        private int _startCount;
        private CancellationTokenSource _cancellationTokenSource;

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}" });
        }

        public override Task<Empty> Start(Empty request, ServerCallContext context)
        {
            lock (_helloSubscribers)
            {
                if (++_startCount == 1)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    StartEmittingEvents(_cancellationTokenSource.Token);
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> Stop(Empty request, ServerCallContext context)
        {
            lock (_helloSubscribers)
            {
                if (_startCount > 0)
                {
                    if (--_startCount == 0)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource = null;
                    }
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task Subscribe(Empty request, IServerStreamWriter<HelloEvent> responseStream, ServerCallContext context)
        {
            return _helloSubscribers.SubscribeAsync(responseStream, context);
        }

        private async void StartEmittingEvents(CancellationToken cancellationToken)
        {
            try
            {
                for (var count=1; !cancellationToken.IsCancellationRequested; ++count)
                {
                    var helloEvent = new HelloEvent {Message = $"Hello #{count}"};
                    Console.WriteLine($"Emitting event: {helloEvent.Message}");
                    await _helloSubscribers.EmitAsync(helloEvent).ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }
}
