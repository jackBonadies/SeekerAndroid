using System;
using System.Diagnostics;

namespace Seeker.Helpers
{
    public readonly struct DebugTimer : IDisposable
    {
#if DEBUG
        private readonly string label;
        private readonly long start;

        private DebugTimer(string label)
        {
            this.label = label;
            start = Stopwatch.GetTimestamp();
        }
#endif

        public static DebugTimer Start(string label)
        {
#if DEBUG
            return new DebugTimer(label);
#else
            return default;
#endif
        }

        public void Dispose()
        {
#if DEBUG
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            Logger.Debug($"[timing] {label}: {ms:F1}ms");
#endif
        }
    }
}
