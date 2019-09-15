using System;
using System.Threading;

namespace NightCore
{
    /// <summary>ミニマルなロック機構を提供します。</summary>
    public abstract partial class MiniLocker
    {
        /// <summary>クリティカル セクションに入ります。即座にクリティカル セクションに入れない場合、クリティカル セクションに入れるか、タイムアウトするか、キャンセルされるまで処理をブロックします。</summary>
        /// <returns>ロック成功したかどうか。</returns>
        /// <remarks>
        /// <see cref="Enter"/>が<see langword="true"/>を返す場合、クリティカル セクションに入ります。クリティカル セクションを抜ける際に<see cref="Leave"/>を必ず呼び出す必要があります。
        /// <see cref="Enter"/>が<see langword="false"/>を返す場合、クリティカル セクションには入りません。<see cref="Leave"/>を呼び出すことはありません。
        /// </remarks>
        public abstract bool Enter(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>クリティカル セクションを抜けます。</summary>
        /// <remarks>
        /// <see cref="Enter"/>が<see langword="true"/>を返すことによってクリティカル セクションに入った場合、必ず<see cref="Leave"/>を呼び出す必要があります。
        /// <see cref="Enter"/>が<see langword="true"/>を返した場合、<see cref="Leave"/>を呼び出すことはできません。
        /// </remarks>
        public abstract void Leave();
    }
}
