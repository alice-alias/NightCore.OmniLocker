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
        /// <summary>空のループでスピンする<see cref="MiniLocker"/>を提供します。</summary>
        public class Spin : MiniLocker
        {
            int locked;

            /// <summary>オーバーライドすると、スピン時に待機する方法を指定できます。</summary>
            protected virtual void Spinning() { }

            /// <inheritdoc />
            public override void Leave()
            {
                locked = 0;
            }

            /// <inheritdoc />
            public override bool Enter(TimeSpan timeout, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                if (timeout == TimeSpan.Zero)
                    return Interlocked.Exchange(ref locked, 1) == 0;

                var stopwatch = Stopwatch.StartNew();

                while (Interlocked.Exchange(ref locked, 1) == 1)
                {
                    if (timeout != Timeout.InfiniteTimeSpan && stopwatch.Elapsed >= timeout)
                        return false;
                    if (cancellationToken.IsCancellationRequested)
                        return false;
                }
                return true;
            }
        }

        /// <summary><see cref="Thread.Yield"/>でスピンする<see cref="MiniLocker"/>を提供します。</summary>
        public class SpinYield : Spin
        {
            /// <inheritdoc />
            protected override void Spinning() => Thread.Yield();
        }

        /// <summary><see cref="Thread.Sleep(int)"/>(0)でスピンする<see cref="MiniLocker"/>を提供します。</summary>
        public class SpinSleep0 : Spin
        {
            /// <inheritdoc />
            protected override void Spinning() => Thread.Sleep(0);
        }

        /// <summary><see cref="Thread.Sleep(int)"/>(1)でスピンする<see cref="MiniLocker"/>を提供します。</summary>
        public class SpinSleep1 : Spin
        {
            /// <inheritdoc />
            protected override void Spinning() => Thread.Sleep(1);
        }
    }
}
