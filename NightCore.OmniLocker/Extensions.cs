using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NightCore
{
    static class Extensions
    {
        public static T Do<T>(this MiniLocker @this, Func<T> action)
        {
            @this.Enter();
            try
            {
                return action();
            }
            finally
            {
                @this.Leave();
            }
        }

        public static void Do(this MiniLocker @this, Action action)
            => @this.Do(() => { action(); return 0; });

        public static void Enter(this MiniLocker @this)
            => @this.Enter(Timeout.InfiniteTimeSpan, CancellationToken.None);
    }
}
