using System;
using System.Threading;
using System.Threading.Tasks;

using Mono.Nat.Logging;

namespace Mono.Nat
{
    static class AsyncExtensions
    {
        static Logger Log { get; } = Logger.Create ();

        class SemaphoreSlimDisposable : IDisposable
        {
            SemaphoreSlim Semaphore;

            public SemaphoreSlimDisposable (SemaphoreSlim semaphore)
            {
                Semaphore = semaphore;
            }

            public void Dispose ()
            {
                Semaphore?.Release ();
                Semaphore = null;
            }
        }

        public static async Task<IDisposable> EnterAsync (this SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync (token);
            return new SemaphoreSlimDisposable (semaphore);
        }
    }
}
