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

        private class CancelOnDispose : IDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public CancellationToken CancellationToken => _cts.Token;

            public void Dispose()
            {
                _cts.Cancel();
            }
        }

        private readonly Greeter.GreeterClient _client;

        public HelloProxy(Channel channel)
        {
            _client = new Greeter.GreeterClient(channel);
        }

        public async Task<string> SayHelloAsync(string name, CancellationToken cancellationToken = default)
        {
            var request = new HelloRequest { Name = name };
            var reply = await _client.SayHelloAsync(request, cancellationToken: cancellationToken).ResponseAsync.ConfigureAwait(false);
            return reply.Message;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _client.StartAsync(Empty, cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _client.StopAsync(Empty, cancellationToken: cancellationToken);
        }

        public AsyncServerStreamingCall<HelloEvent> Subscribe(CancellationToken cancellationToken = default)
        {
            return _client.Subscribe(Empty, cancellationToken: cancellationToken);
        }

        public IDisposable Subscribe(Func<HelloEvent, Task> callback)
        {
            var disposable = new CancelOnDispose();
            var reply = Subscribe(disposable.CancellationToken);
            PushStreamItems(reply.ResponseStream, callback, disposable.CancellationToken);
            return disposable;
        }

        protected async void PushStreamItems<T>(IAsyncStreamReader<T> stream, Func<T, Task> func, CancellationToken cancellationToken) where T : class
        {
            try
            {
                while (await stream.MoveNext(cancellationToken).ConfigureAwait(false))
                    await func(stream.Current).ConfigureAwait(false);
            }
            catch (RpcException exc) when (exc.StatusCode == StatusCode.Cancelled)
            {
                await func(null).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await func(null).ConfigureAwait(false);
            }
        }
    }
}
