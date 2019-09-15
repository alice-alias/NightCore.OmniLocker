using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NightCore
{
    class EventWaiter<T> where T : MiniLocker, new()
    {
        readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        readonly MiniLocker locker = new MiniLocker.Spin();

        bool released;

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return locker.Do(() =>
            {
                if (!released)
                {
                    var ev = new T();
                    ev.Enter();
                    queue.Enqueue(ev);
                    return ev;
                }
                return null;
            })?.Enter(timeout, cancellationToken) ?? false;
        }

        public void ReleaseOne()
        {
            if (queue.TryDequeue(out var ev))
            {
                locker.Do(() =>
                {
                    ev.Leave();
                    ev.Leave();
                });
            }
        }

        public void ReleaseAll()
        {
            locker.Do(() =>
            {
                released = true;
                foreach (var ev in queue.ToList())
                {
                    ev.Leave();
                    ev.Leave();
                }
            });
        }
    }
}
