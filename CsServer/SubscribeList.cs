using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace CsServer
{
    public sealed class SubscribeList<TEvent>
    {
        private readonly List<IServerStreamWriter<TEvent>> _subscribeList = new List<IServerStreamWriter<TEvent>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public void Add(IServerStreamWriter<TEvent> serverStreamWriter)
        {
            _lock.EnterWriteLock();
            _subscribeList.Add(serverStreamWriter);
            _lock.ExitWriteLock();
        }

        public void Remove(IServerStreamWriter<TEvent> serverStreamWriter)
        {
            _lock.EnterWriteLock();
            _subscribeList.Remove(serverStreamWriter);
            _lock.ExitWriteLock();
        }

        public async Task SubscribeAsync(IServerStreamWriter<TEvent> responseStream, ServerCallContext context)
        {
            try
            {
                Add(responseStream);
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            finally
            {
                Remove(responseStream);
            }
        }

        public Task EmitAsync(TEvent eventData)
        {
            _lock.EnterReadLock();
            try
            {
                // Don't await here, because we only need the lock during the enumeration of
                // the subscribe list that is done during the "Task.WhenAll" call.
                return Task.WhenAll(_subscribeList.Select(async ssw =>
                {
                    try
                    {
                        await ssw.WriteAsync(eventData).ConfigureAwait(false);
                    }
                    catch
                    {
                        // TODO: CHeck if we can add some decent logging here
                        // Ignore
                    }
                }));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
