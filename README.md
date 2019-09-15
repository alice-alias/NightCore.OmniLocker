# NightCore.OmniLocker

[![NuGet version](https://badge.fury.io/nu/NightCore.OmniLocker.svg)](https://badge.fury.io/nu/NightCore.OmniLocker)

ロックしたりするやつ

## つかう

https://www.nuget.org/packages/NightCore.OmniLocker

```cs
using NightCore;

class LockDemo {
    private OmniLocker state = new OmniLocker();

    private void Demo1() {
        // ロック完了までブロックする
        using (var context = state.Enter()) {
            // クリティカルセクション...
        }
    }

    private void Demo2() {
        // 即座にロックできない場合は失敗する
        using (var context = state.EnterImmediately()) {
            if (context.Succeed) {
                // クリティカルセクション...
            } else {
                // ロックは失敗した
            }
        }
    }
}
```