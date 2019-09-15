using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace NightCore.Test
{
    public class OmniLockerBenchmark
    {
        private readonly ITestOutputHelper output;

        public OmniLockerBenchmark(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void EnterBenchmark()
        {
            var duration = TimeSpan.FromSeconds(5);
            var locker = new OmniLocker();

            int count = 0;

            void increment(ref int i)
            {
                var sw = Stopwatch.StartNew();

                while (sw.Elapsed < duration)
                {
                    using (var locked = locker.Enter())
                        i++;
                }
            }

            var list = new List<Thread>();

            for (int i = 0; i < 4; i++)
                list.Add(new Thread(new ThreadStart(() => increment(ref count))));

            foreach (var t in list)
                t.Start();

            foreach (var t in list)
                t.Join();

            output.WriteLine($"{count} iteration in {duration}.");

            Assert.True(true);
        }
    }
}
