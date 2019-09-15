using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NightCore
{
    class Awaiter<T> : INotifyCompletion, IDisposable where T : MiniLocker, new()
    {
        int completed;
        Action? continuation;
        readonly EventWaiter<T> ev = new EventWaiter<T>();
        bool result;
        readonly MiniLocker disposing = new MiniLocker.Spin();

        public void OnCompleted(Action continuation)
        {
            this.continuation += continuation;
            if (completed != 0)
            {
                continuation?.Invoke();
            }
        }

        public void Complete(bool result)
        {
            if (Interlocked.Exchange(ref completed, 1) == 0)
            {
                disposing.Do(() =>
                {
                    ev.ReleaseAll();

                    this.result = result;
                    continuation?.Invoke();
                });
            }
        }

        public bool IsCompleted => completed != 0;

        public void Dispose()
            => Complete(false);

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return disposing.Do(() =>
            {
                if (!IsCompleted)
                    return ev;
                else
                    return null;
            })?.Wait(timeout, cancellationToken) ?? false;
        }

        public bool GetResult()
        {
            Wait(Timeout.InfiniteTimeSpan, CancellationToken.None);
            return result;
        }

        public async Task WaitAsync() => await new Awaitable(this);

        class Awaitable
        {
            readonly Awaiter<T> awaiter;

            public Awaitable(Awaiter<T> awaiter) => this.awaiter = awaiter;

            public Awaiter<T> GetAwaiter() => awaiter;
        }
    }
}
