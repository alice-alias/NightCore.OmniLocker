using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NightCore
{
    /// <summary>
    /// 多目的なロック機構を提供します。
    /// </summary>
    public partial class OmniLocker<T> where T : MiniLocker, new()
    {
        int locked;

        readonly ConcurrentQueue<Awaiter<T>> queue = new ConcurrentQueue<Awaiter<T>>();

        /// <summary>
        /// ロックを行います。即座にロックできない場合、ロックに成功するまで処理をブロックします。
        /// </summary>
        /// <returns>ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。</returns>
        public Context Enter() => Enter(Timeout.InfiniteTimeSpan, CancellationToken.None);

        /// <summary>
        /// ロックを行います。即座にロックできない場合、ロックに成功するか、タイムアウトするまで処理をブロックします。
        /// </summary>
        /// <returns>ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。</returns>
        public Context Enter(TimeSpan timeout) => Enter(timeout, CancellationToken.None);

        /// <summary>
        /// ロックを行います。即座にロックできない場合、ロックに成功するか、キャンセルされるまで処理をブロックします。
        /// </summary>
        /// <returns>ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。</returns>
        public Context Enter(CancellationToken cancellationToken) => Enter(Timeout.InfiniteTimeSpan, cancellationToken);

        /// <summary>
        /// ロックを行います。即座にロックできない場合、ロックに成功するか、タイムアウトするか、キャンセルされるまで処理をブロックします。
        /// </summary>
        /// <returns>ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。</returns>
        public Context Enter(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref locked, 1) == 1)
            {
                using var ev = new Awaiter<T>();

                queue.Enqueue(ev);
                if (!ev.Wait(timeout, cancellationToken))
                {
                    return Context.Failed;
                }
            }

            return new Context(this);
        }

        /// <summary>
        /// ロックを行います。即座にロックできない場合、ロックに成功するまで待機します。
        /// </summary>
        /// <returns>ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。</returns>
        public async Task<Context> EnterAsync()
        {
            if (Interlocked.Exchange(ref locked, 1) == 1)
            {
                var ev = new Awaiter<T>();
                queue.Enqueue(ev);
                await ev.WaitAsync();
            }

            return new Context(this);
        }


        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。ロック要求中にタイムアウトするか、ロック要求中にキャンセルされるか、一連の処理が完了するまでの間、処理はブロックされます。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        /// <param name="timeout">タイムアウト。</param>
        /// <param name="cancellationToken">キャンセル トークン。</param>
        public void Send(Action action, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var context = Enter(timeout, cancellationToken);
            if (context.Succeed)
                action();
        }

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。一連の処理が完了するまでの間、処理はブロックされます。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        public void Send(Action action) => Send(action, Timeout.InfiniteTimeSpan, CancellationToken.None);

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。ロック要求中にタイムアウトするか、一連の処理が完了するまでの間、処理はブロックされます。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        /// <param name="timeout">タイムアウト。</param>
        public void Send(Action action, TimeSpan timeout) => Send(action, timeout, CancellationToken.None);

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。ロック要求中にキャンセルされるか、一連の処理が完了するまでの間、処理はブロックされます。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        /// <param name="cancellationToken">キャンセル トークン。</param>
        public void Send(Action action, CancellationToken cancellationToken) => Send(action, Timeout.InfiniteTimeSpan, cancellationToken);

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。一連の処理が完了するまでの間、待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        public async Task SendAsync(Action action)
        {
            using (var _ = await EnterAsync())
                action();
        }

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。一連の処理が完了するまでの間、待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        public async Task<U> SendAsync<U>(Func<U> action)
        {
            using (var _ = await EnterAsync())
                return action();
        }

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。一連の処理が完了するまでの間、待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する待機可能な処理。</param>
        public async Task SendAsync(Func<Task> action)
        {
            using (var _ = await EnterAsync())
                await action();
        }

        /// <summary>
        /// ロックに成功したときに実行する処理を指定して、ロックを行います。一連の処理が完了するまでの間、待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する待機可能な処理。</param>
        public async Task<U> SendAsync<U>(Func<Task<U>> action)
        {
            using (var _ = await EnterAsync())
                return await action();
        }

        /// <summary>
        /// ロックを行います。即座にロックできない場合、失敗した<see cref="Context.Failed"/>が返ります。
        /// </summary>
        /// <returns>
        /// ロック コンテキスト。<see cref="IDisposable.Dispose" />の呼び出しでロックを開放します。
        /// </returns>
        public Context EnterImmediately()
        {
            if (Interlocked.Exchange(ref locked, 1) == 0)
            {
                return new Context(this);
            }

            return Context.Failed;
        }

        /// <summary>
        /// ロックに成功したときの処理を指定して、ロックを行います。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        /// <param name="failedResult">ロックに失敗したときに返却される値。</param>
        /// <returns>
        /// <paramref name="action"/>が実行された場合、その結果。
        /// <paramref name="action"/>が実行されなかった場合、<paramref name="failedResult"/>。
        /// </returns>
        public U EnterImmediately<U>(Func<U> action, U failedResult)
        {
            using var context = EnterImmediately();

            if (context.Succeed)
                return action();
            else
                return failedResult;
        }

        /// <summary>
        /// ロックに成功したときの処理を指定して、ロックを行います。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する処理。</param>
        /// <returns><paramref name="action"/>が実行された場合、<see langword="true"/>。</returns>
        public bool EnterImmediately(Action action) => EnterImmediately(() => { action(); return true; }, false);

        /// <summary>
        /// ロックに成功したときの処理を指定して、ロックを行います。一連の処理が完了するまでの間待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する待機可能な処理。</param>
        /// <param name="failedResult">ロックに失敗したときに返却される値。</param>
        /// <returns>
        /// <paramref name="action"/>が実行された場合、<see langword="true"/>。
        /// <paramref name="action"/>が実行されなかった場合、<paramref name="failedResult"/>。
        /// </returns>
        public async Task<U> EnterImmediatelyAsync<U>(Func<Task<U>> action, U failedResult)
        {
            using var context = EnterImmediately();
            if (context.Succeed)
                return await action();
            else
                return failedResult;
        }

        /// <summary>
        /// ロックに成功したときの処理を指定して、ロックを行います。一連の処理が完了するまでの間待機します。
        /// </summary>
        /// <param name="action">ロックに成功したときに実行する待機可能な処理。</param>
        /// <returns><paramref name="action"/>が実行された場合、<see langword="true"/>。</returns>
        public Task<bool> EnterImmediatelyAsync(Func<Task> action)
            => EnterImmediatelyAsync(async () => { await action(); return true; }, false);

        bool TryDequeue(out Awaiter<T> ev)
        {
            while (true)
            {
                var result = queue.TryDequeue(out ev);
                if (result && ev.IsCompleted)
                    continue;
                return result;
            }
        }

        void Release()
        {
            if (TryDequeue(out var ev))
            {
                ev.Complete(true);
                ev.Dispose();
            }
            else
            {
                locked = 0;
            }
        }
    }

    /// <summary><see cref="ManualResetEventSlim"/>を利用した<see cref="OmniLocker{T}"/>を提供します。</summary>
    public class OmniLocker : OmniLocker<MiniLocker.ResetEvent> { }
}
