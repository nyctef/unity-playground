using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public static class StopwatchExtensions
    {
        public static TimeSpan Lap(this Stopwatch stopwatch)
        {
            var time = stopwatch.Elapsed;
            stopwatch.Stop();
            stopwatch.Start();
            return time;
        }
    }
}
