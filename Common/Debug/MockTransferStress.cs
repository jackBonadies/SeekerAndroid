#if MOCK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Seeker.Helpers;
using Seeker.Services;

namespace Seeker.Debug
{
    public static class MockTransferStress
    {
        public static int BatchSize = 3;
        public static int ClumpSize = 5;

        public static double CyclesPerMinute = 10;

        public static bool QueuePaused = false;

        public static bool AutoStartOnLaunch = false;

        public static TimeSpan AutoStartDelay = TimeSpan.FromSeconds(15);

        private static int isRunning;
        private static CancellationTokenSource cts;
        private static int cycleCounter;

        public static bool IsRunning => Volatile.Read(ref isRunning) == 1;

        public static bool Start()
        {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                return false;
            }

            var tokenSource = new CancellationTokenSource();
            cts = tokenSource;

            var thread = new Thread(() => RunLoop(tokenSource.Token, TimeSpan.Zero))
            {
                IsBackground = true,
                Name = "MockTransferStress",
            };
            thread.Start();
            return true;
        }

        public static void Stop()
        {
            CancellationTokenSource tokenSource = Interlocked.Exchange(ref cts, null);
            if (tokenSource == null)
            {
                return;
            }
            tokenSource.Cancel();
        }

        public static bool Toggle()
        {
            if (IsRunning)
            {
                Stop();
                return false;
            }
            Start();
            return true;
        }

        public static void StartDelayed()
        {
            if (Interlocked.CompareExchange(ref isRunning, 1, 0) != 0)
            {
                return;
            }
            var tokenSource = new CancellationTokenSource();
            cts = tokenSource;
            var thread = new Thread(() => RunLoop(tokenSource.Token, AutoStartDelay))
            {
                IsBackground = true,
                Name = "MockTransferStress",
            };
            thread.Start();
        }

        private static void RunLoop(CancellationToken token, TimeSpan initialDelay)
        {
            try
            {
                if (initialDelay > TimeSpan.Zero && token.WaitHandle.WaitOne(initialDelay))
                {
                    return;
                }

                while (!token.IsCancellationRequested)
                {
                    int batch = Math.Max(1, BatchSize);
                    int cycle = ++cycleCounter;

                    try
                    {
                        for (int i = 0; i < ClumpSize; i++)
                        {
                            AddBatch(batch, cycle);
                            System.Threading.Thread.Sleep(100);
                        }
                        System.Threading.Thread.Sleep(1000);
                        for (int i = 0; i < ClumpSize; i++)
                        {
                            AddBatch(batch, cycle);
                            System.Threading.Thread.Sleep(100);
                        }
                        System.Threading.Thread.Sleep(1000);
                        for (int i = 0; i < ClumpSize; i++)
                        {
                            //RemoveBatch(batch, cycle);
                            //System.Threading.Thread.Sleep(100);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Debug("MockTransferStress: cycle failed: " + e);
                    }

                    double perMinute = CyclesPerMinute;
                    int delayMs = perMinute <= 0 ? 1000 : (int)Math.Max(1, Math.Round(60000.0 / perMinute));
                    if (token.WaitHandle.WaitOne(delayMs))
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Debug("MockTransferStress: loop died: " + e);
            }
            finally
            {
                Volatile.Write(ref isRunning, 0);
                Logger.Debug("MockTransferStress: stopped");
            }
        }
        private static int number;
        private static int getNumber()
        {
            number++;
            return number;
        }

        private static void AddBatch(int batch, int cycle)
        {
            DownloadService service = DownloadService.Instance;
            if (service == null)
            {
                return;
            }

            string username = "stress_user_" + (cycle % 5);
            var files = new FullFileInfo[batch];
            for (int i = 0; i < batch; i++)
            {
                int number = getNumber();
                files[i] = new FullFileInfo
                {
                    FullFileName = $"stress\\album_{cycle}\\track_{number:D4}.mp3",
                    Size = 1024 * 1024,
                    Depth = 1,
                };
            }

            service.EnqueueFilesFireAndForget(files, QueuePaused, username);
        }

        private static void RemoveBatch(int batch, int cycle)
        {
            TransferItemManager manager = TransferItems.TransferItemManagerDL;
            if (manager == null)
            {
                return;
            }

            if (cycle % 3 == 0)
            {
                FolderItem folder = null;
                lock (manager.AllFolderItems)
                {
                    if (manager.AllFolderItems.Count > 0)
                    {
                        folder = manager.AllFolderItems[0];
                    }
                }
                if (folder != null)
                {
                    manager.ClearAllFromFolder(folder);
                    return;
                }
            }

            List<TransferItem> snapshot;
            lock (manager.AllTransferItems)
            {
                snapshot = manager.AllTransferItems.Take(batch).ToList();
            }
            foreach (TransferItem ti in snapshot)
            {
                manager.Remove(ti);
            }
        }
    }
}
#endif
