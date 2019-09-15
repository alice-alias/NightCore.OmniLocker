using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NightCore
{
    partial class OmniLocker<T>
    {
        /// <summary><see cref="OmniLocker"/>のロック コンテキストを表します。</summary>
        public sealed class Context : IDisposable
        {
            readonly OmniLocker<T>? locker;

            internal Context(OmniLocker<T> locker)
            {
                this.locker = locker;
            }

            private Context() { }

            /// <summary>ロックが正常に取得されたかどうか。</summary>
            public bool Succeed => locker != null;

            /// <summary>ロックを解放します。</summary>
            public void Dispose()
            {
                locker?.Release();
            }

            /// <summary>ロックに失敗したコンテキスト。</summary>
            public static Context Failed { get; } = new Context();
        }
    }
}
