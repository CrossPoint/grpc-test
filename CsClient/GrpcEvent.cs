using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace CsClient
{
    public class GrpcEvent<TEvent> 
    {
        public class Args : EventArgs
        {
            public TEvent Data { get; }
            public Args(TEvent data) => Data = data;
        }

        private int _subscriptions;
        private EventHandler<Args> _event;
        private IDisposable _subscription;
        
        public void Add(EventHandler<Args> handler, Func<CancellationToken, AsyncServerStreamingCall<TEvent>> subscribeFunc)
        {
            _event += handler;
            if (Interlocked.Increment(ref _subscriptions) == 1)           
                _subscription = SubscribeHelper.Subscribe(subscribeFunc, data => _event?.Invoke(this, new Args(data)));
        }
        
        public void Remove(EventHandler<Args> handler)
        {
            _event -= handler;
            if (Interlocked.Decrement(ref _subscriptions) == 0)           
                _subscription.Dispose();
        }
    }
}
