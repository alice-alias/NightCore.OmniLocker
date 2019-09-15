using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace NightCore.Test
{
    public class MiniLockerBenchmark
    {
        private readonly ITestOutputHelper output;

        public MiniLockerBenchmark(ITestOutputHelper output)
        {
            this.output = output;
        }

        delegate void IncrementCallback(ref int i);

        int Benchmark(IncrementCallback increment, int threadCount, TimeSpan duration)
        {
            int count = 0;

            void inc(ref int i)
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed < duration)
                    increment(ref i);
            }

            var list = new List<Thread>();

            for (int i = 0; i < threadCount; i++)
                list.Add(new Thread(new ThreadStart(() => inc(ref count))));

            foreach (var t in list)
                t.Start();

            foreach (var t in list)
                t.Join();

            return count;
        }

        [Fact]
        public void LockBenchmark()
        {
            var locker = new object();
            var duration = TimeSpan.FromSeconds(1);
            void increment(ref int i)
            {
                lock (locker)
                {
                    i++;
                }
            }

            var count = Benchmark(increment, 4, duration);

            output.WriteLine($"{count} iteration in {duration}.");

            Assert.True(true);
        }

        void SpinBenchmarkBase<T>() where T : MiniLocker, new()
        {
            var locker = new T();

            var duration = TimeSpan.FromSeconds(1);
            void increment(ref int i)
            {
                locker.Enter(Timeout.InfiniteTimeSpan, CancellationToken.None);
                try
                {
                    i++;
                }
                finally
                {
                    locker.Leave();
                }
            }

            var count = Benchmark(increment, 4, duration);

            output.WriteLine($"{count} iteration in {duration}.");

            Assert.True(true);
        }


        [Fact]
        public void SpinBenchmark() => SpinBenchmarkBase<MiniLocker.Spin>();

        [Fact]
        public void SpinYieldBenchmark() => SpinBenchmarkBase<MiniLocker.SpinYield>();

        [Fact]
        public void SpinSleep0Benchmark() => SpinBenchmarkBase<MiniLocker.SpinSleep0>();

        [Fact]
        public void SpinSleep1Benchmark() => SpinBenchmarkBase<MiniLocker.SpinSleep1>();

        [Fact]
        public void ResetEventBenchmark() => SpinBenchmarkBase<MiniLocker.ResetEvent>();
    }
}
