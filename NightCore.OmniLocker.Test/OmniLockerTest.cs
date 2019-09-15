using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NightCore.Test
{
    public class OmniLockerTest
    {
        private readonly ITestOutputHelper output;

        public OmniLockerTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact(Timeout = 10)]
        public void EnterTest()
        {
            int i = 0, k = 0;
            var locker = new OmniLocker();

            void action()
            {
                foreach (var _ in Enumerable.Range(0, 10))
                {
                    using (var ctx = locker.Enter())
                    {
                        var j = i;
                        output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: read {j}.");
                        Thread.Sleep(100);
                        i = j + 1;
                        output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: write {j + 1}.");
                        Interlocked.Increment(ref k);
                    }
                }
            }

            var t1 = Task.Run(action);
            var t2 = Task.Run(action);
            var t3 = Task.Run(action);

            t1.Wait();
            t2.Wait();
            t3.Wait();
            
            Assert.Equal(k, i);
        }


        [Fact(Timeout = 1000)]
        public void EnterImmediatelyTest()
        {
            int i = 0, k = 0, skip = 0;
            var locker = new OmniLocker();

            void action()
            {
                foreach (var _ in Enumerable.Range(0, 10))
                {
                    using (var ctx = locker.EnterImmediately())
                    {
                        if (ctx.Succeed)
                        {
                            var j = i;
                            output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: read {j}.");
                            Thread.Sleep(100);
                            i = j + 1;
                            output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: write {j + 1}.");
                        }
                        else
                        {
                            output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: skip.");
                            Interlocked.Increment(ref skip);
                        }
                        Interlocked.Increment(ref k);
                    }
                }
            }

            var t1 = Task.Run(action);
            var t2 = Task.Run(action);
            var t3 = Task.Run(action);

            t1.Wait();
            t2.Wait();
            t3.Wait();

            Assert.Equal(k - skip, i);
        }


        [Fact(Timeout = 1000)]
        public async Task EnterAsyncTest()
        {
            int i = 0, k = 0;
            var locker = new OmniLocker();

            async Task action()
            {
                foreach (var _ in Enumerable.Range(0, 10))
                {
                    using (var ctx = await locker.EnterAsync())
                    {
                        var j = i;
                        output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: read {j}.");
                        Thread.Sleep(100);
                        i = j + 1;
                        output.WriteLine($"{Thread.CurrentThread.ManagedThreadId}: write {j + 1}.");
                        Interlocked.Increment(ref k);
                    }
                }
            }

            var t1 = action();
            var t2 = action();
            var t3 = action();

            await Task.WhenAll(t1, t2, t3);

            Assert.Equal(k, i);
        }
    }
}
