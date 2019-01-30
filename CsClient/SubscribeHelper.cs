using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace CsClient
{
    public static class SubscribeHelper
    {
        private class CancelOnDispose : IDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            public CancellationToken CancellationToken => _cts.Token;
            public void Dispose() => _cts.Cancel();
        }

        public static IDisposable Subscribe<T>(Func<CancellationToken, AsyncServerStreamingCall<T>> subscribeFunc, Action<T> callback)
        {
            var disposable = new CancelOnDispose();
            var reply = subscribeFunc(disposable.CancellationToken);
            reply.ResponseStream.PushStreamItems(callback, disposable.CancellationToken);
            return disposable;
        }

        public static IDisposable Subscribe<T>(Func<CancellationToken, AsyncServerStreamingCall<T>> subscribeFunc, Func<T, Task> callback)
        {
            var disposable = new CancelOnDispose();
            var reply = subscribeFunc(disposable.CancellationToken);
            reply.ResponseStream.PushStreamItems(callback, disposable.CancellationToken);
            return disposable;
        }

        public static async void PushStreamItems<T>(this IAsyncStreamReader<T> stream, Action<T> action, CancellationToken cancellationToken = default)
        {
            try
            {
                while (await stream.MoveNext(cancellationToken).ConfigureAwait(false))
                    action(stream.Current);
            }
            catch (RpcException exc) when (exc.StatusCode == StatusCode.Cancelled)
            {
                // Done
            }
            catch (OperationCanceledException)
            {
                // Done
            }
        }

        public static async void PushStreamItems<T>(this IAsyncStreamReader<T> stream, Func<T, Task> func, CancellationToken cancellationToken = default)
        {
            try
            {
                while (await stream.MoveNext(cancellationToken).ConfigureAwait(false))
                    await func(stream.Current).ConfigureAwait(false);
            }
            catch (RpcException exc) when (exc.StatusCode == StatusCode.Cancelled)
            {
                // Done
            }
            catch (OperationCanceledException)
            {
                // Done
            }
        }
    }
}
