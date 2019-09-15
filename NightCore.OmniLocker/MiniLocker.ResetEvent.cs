using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NightCore
{
    partial class MiniLocker
    {
        /// <summary><see cref="ManualResetEventSlim"/>を利用した<see cref="MiniLocker"/>を提供します。</summary>
        public class ResetEvent : MiniLocker
        {
            bool locked;

#pragma warning disable IDE0069
            readonly ManualResetEventSlim ev = new ManualResetEventSlim();
#pragma warning restore IDE0069

            readonly MiniLocker locker = new Spin();

            TimeSpan GetTimeout(TimeSpan timeout, TimeSpan elapsed)
            {
                if (timeout == Timeout.InfiniteTimeSpan)
                    return Timeout.InfiniteTimeSpan;

                var diff = timeout - elapsed;
                if (timeout.Ticks < 0)
                    return TimeSpan.FromTicks(0);
                return diff;
            }

            /// <inheritdoc />
            public override bool Enter(TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                if (timeout == TimeSpan.Zero)
                    return locker.Do(() =>
                    {
                        if (!locked)
                        {
                            locked = true;
                            ev.Reset();
                            return true;
                        }
                        return false;
                    });

                var stopwatch = Stopwatch.StartNew();

                while (true)
                {
                    if (!locker.Do(() =>
                    {
                        var locked = this.locked;
                        this.locked = true;
                        if (!locked)
                        {
                            ev.Reset();
                        }
                        return locked;
                    }))
                        return true;

                    if (cancellationToken.IsCancellationRequested)
                        return false;

                    if (timeout != Timeout.InfiniteTimeSpan && stopwatch.Elapsed >= timeout)
                        return false;

                    ev.Wait(GetTimeout(timeout, stopwatch.Elapsed), cancellationToken);
                }
            }

            /// <inheritdoc />
            public override void Leave()
            {
                locker.Do(() =>
                {
                    locked = false;
                    ev.Set();
                });
            }
        }
    }
}
