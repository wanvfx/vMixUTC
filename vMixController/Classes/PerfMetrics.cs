using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vMixController.Classes
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;

    namespace vMixController.Classes
    {
        public static class PerfMetrics
        {
            private sealed class Metric
            {
                public long Count;
                public long TotalTicks;
                public long MaxTicks;
                public long Errors;
            }

            private sealed class MeasureScope : IDisposable
            {
                private readonly string _name;
                private readonly Stopwatch _sw;
                private bool _disposed;

                public MeasureScope(string name)
                {
                    _name = name;
                    _sw = Stopwatch.StartNew();
                }

                public void Dispose()
                {
                    if (_disposed)
                        return;
                    _disposed = true;
                    _sw.Stop();
                    Record(_name, _sw.ElapsedTicks);
                }
            }

            private static readonly ConcurrentDictionary<string, Metric> _metrics =
                new ConcurrentDictionary<string, Metric>(StringComparer.Ordinal);

            public static IDisposable Measure(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;
                return new MeasureScope(name);
            }

            public static void Increment(string name, int delta = 1)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                var metric = _metrics.GetOrAdd(name, _ => new Metric());
                Interlocked.Add(ref metric.Count, delta);
            }

            public static void Error(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                var metric = _metrics.GetOrAdd(name, _ => new Metric());
                Interlocked.Increment(ref metric.Errors);
            }

            public static string SnapshotAndReset()
            {
                if (_metrics.IsEmpty)
                    return string.Empty;

                var items = _metrics.ToArray();
                if (items.Length == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                sb.AppendLine("PerfMetrics snapshot:");
                foreach (var item in items.OrderBy(x => x.Key))
                {
                    var key = item.Key;
                    var metric = item.Value;

                    var count = Interlocked.Exchange(ref metric.Count, 0);
                    var ticks = Interlocked.Exchange(ref metric.TotalTicks, 0);
                    var maxTicks = Interlocked.Exchange(ref metric.MaxTicks, 0);
                    var errors = Interlocked.Exchange(ref metric.Errors, 0);

                    if (count == 0 && ticks == 0 && maxTicks == 0 && errors == 0)
                        continue;

                    var totalMs = ticks * 1000.0 / Stopwatch.Frequency;
                    var avgMs = count > 0 ? totalMs / count : 0;
                    var maxMs = maxTicks * 1000.0 / Stopwatch.Frequency;

                    sb.AppendFormat("{0}: count={1}, avg={2:0.###}ms, max={3:0.###}ms, errors={4}",
                        key, count, avgMs, maxMs, errors);
                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd();
            }

            private static void Record(string name, long elapsedTicks)
            {
                var metric = _metrics.GetOrAdd(name, _ => new Metric());
                Interlocked.Increment(ref metric.Count);
                Interlocked.Add(ref metric.TotalTicks, elapsedTicks);

                long initial;
                do
                {
                    initial = Volatile.Read(ref metric.MaxTicks);
                    if (initial >= elapsedTicks)
                        break;
                } while (Interlocked.CompareExchange(ref metric.MaxTicks, elapsedTicks, initial) != initial);
            }
        }
    }

}
