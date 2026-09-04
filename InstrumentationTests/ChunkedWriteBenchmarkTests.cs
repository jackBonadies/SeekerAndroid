using System;
using System.Diagnostics;
using System.Threading;
using Android.App;
using Android.Util;
using Common;
using NUnit.Framework;

namespace InstrumentationTests
{
    /// <summary>
    /// Benchmarks for Memory backed download saving regarding the JNI marshal of the byte[]
    /// </summary>
    [TestFixture]
    public class ChunkedWriteBenchmarkTests
    {
        const string Tag = "SeekerTests";

        private const int PayloadBytes = 48 * 1024 * 1024; // 48 MB

        // Loose limit for chunked as half the payload bytes
        private const long ChunkedPeakBudgetBytes = PayloadBytes / 2;

        private Java.IO.File _testJavaFile;
        private Android.Net.Uri _testFileUri;
        private byte[] _payload;

        [SetUp]
        public void SetUp()
        {
            _testJavaFile = new Java.IO.File(Application.Context.CacheDir, $"bench_{Guid.NewGuid():N}.tmp");
            _testJavaFile.CreateNewFile();
            _testFileUri = Android.Net.Uri.FromFile(_testJavaFile);
            _payload = new byte[PayloadBytes];
        }

        [TearDown]
        public void TearDown()
        {
            _payload = null;
            _testJavaFile?.Delete();
        }

        [Test]
        public void ChunkedWrite_DoesNotAllocateOnTheJavaHeap()
        {
            var result = Measure("chunked", stream =>
                ChunkedStreamWriter.WriteChunked(stream, new ArraySegment<byte>(_payload)));

            Assert.That(_testJavaFile.Length(), Is.EqualTo(PayloadBytes), "chunked write produced the wrong file length");
            Assert.That(result.PeakHeapGrowth, Is.LessThan(ChunkedPeakBudgetBytes),
                $"chunked write grew the Java heap by {Mb(result.PeakHeapGrowth)}MB of a " +
                $"{Mb(PayloadBytes)}MB payload");
        }

        [Test]
        public void SingleWrite_AllocatesTheWholeBufferOnTheJavaHeap()
        {
            // prefix behavior (it copies entire byte[] to JNI)
            var result = Measure("single", stream =>
            {
                stream.Write(_payload, 0, _payload.Length);
                stream.Flush();
            });

            Assert.That(_testJavaFile.Length(), Is.EqualTo(PayloadBytes), "single write produced the wrong file length");
            Assert.That(result.PeakHeapGrowth, Is.GreaterThanOrEqualTo(PayloadBytes),
                $"chunked write grew the Java heap by {Mb(result.PeakHeapGrowth)}MB of a " +
                $"{Mb(PayloadBytes)}MB payload");
            Log.Info(Tag, $"  NOTE: single Write peaked at {Mb(result.PeakHeapGrowth)}MB of Java heap " +
                          $"for a {Mb(PayloadBytes)}MB payload");
        }

        private readonly struct Result
        {
            public Result(long peak, long ms) { PeakHeapGrowth = peak; ElapsedMs = ms; }
            public long PeakHeapGrowth { get; }
            public long ElapsedMs { get; }
        }

        /// <summary>
        /// Runs <paramref name="write"/> against a ContentResolver output stream while sampling
        /// the ART heap, and logs peak growth + elapsed.
        /// </summary>
        private Result Measure(string label, Action<System.IO.Stream> write)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Java.Lang.JavaSystem.Gc();
            Thread.Sleep(100);

            long baseline = UsedJavaHeap();
            long peak = baseline;
            bool sampling = true;

            var sampler = new Thread(() =>
            {
                while (Volatile.Read(ref sampling))
                {
                    long used = UsedJavaHeap();
                    if (used > peak)
                    {
                        peak = used;
                    }
                    Thread.Sleep(1);
                }
            }) { IsBackground = true };
            sampler.Start();

            var sw = Stopwatch.StartNew();
            using (var stream = Application.Context.ContentResolver.OpenOutputStream(_testFileUri))
            {
                write(stream);
            }
            sw.Stop();

            Volatile.Write(ref sampling, false);
            sampler.Join();

            long growth = Math.Max(0, peak - baseline);
            Log.Info(Tag, $"  BENCH {label}: {Mb(PayloadBytes)}MB payload, " +
                          $"peak Java heap +{Mb(growth)}MB, {sw.ElapsedMilliseconds}ms");
            return new Result(growth, sw.ElapsedMilliseconds);
        }

        private static readonly Java.Lang.Runtime JavaRuntime = Java.Lang.Runtime.GetRuntime();

        private static long UsedJavaHeap() => JavaRuntime.TotalMemory() - JavaRuntime.FreeMemory();

        private static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("F1");
    }
}
