using Seeker.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    public static class SpeedLimitHelper
    {
        private static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>(); //we need the double precision bc sometimes 1.1 cast to int will be the same number i.e. (int)(4*1.1)==4
        private static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();

        private static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();

        public static void RemoveDownloadUser(string username)
        {
            DownloadUserDelays.TryRemove(username, out _);
            DownloadLastAvgSpeed.TryRemove(username, out _);
        }

        public static void RemoveUploadUser(string username)
        {
            UploadUserDelays.TryRemove(username, out _);
            UploadLastAvgSpeed.TryRemove(username, out _);
        }

        public static async Task<int> OurDownloadGovernor(Soulseek.Transfer transfer, int requestedBytes, CancellationToken cts)
        {
            try
            {
                if (PreferencesState.SpeedLimitDownloadOn)
                {
                    var username = transfer.Username;
                    var averageSpeed = transfer.AverageSpeed;

                    if (DownloadUserDelays.TryGetValue(username, out double msDelay))
                    {
                        bool exists = DownloadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                        if (exists && averageSpeed == lastAvgSpeed)
                        {
                            //do not adjust as we have not yet recalculated the average speed
                            await Task.Delay((int)msDelay, cts);
                            return int.MaxValue;
                        }

                        DownloadLastAvgSpeed[username] = averageSpeed;

                        if (!PreferencesState.SpeedLimitDownloadIsPerTransfer && DownloadLastAvgSpeed.Count > 1)
                        {

                            //its threadsafe when using linq on concurrent dict itself.
                            averageSpeed = DownloadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
                        }

                        if (averageSpeed > PreferencesState.SpeedLimitDownloadBytesSec)
                        {
                            DownloadUserDelays[username] = msDelay = msDelay * 1.04;
                        }
                        else
                        {
                            DownloadUserDelays[username] = msDelay = msDelay * 0.96;
                        }

                        await Task.Delay((int)msDelay, cts);
                        return int.MaxValue;
                    }
                    else
                    {
#if DEBUG
                        //System.Console.WriteLine("first time guess");
#endif
                        //first time we need to guess a decent value
                        //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                        double msDelaySeed = 1000 * 16384.0 / PreferencesState.SpeedLimitDownloadBytesSec;
                        DownloadUserDelays[username] = msDelaySeed;
                        DownloadLastAvgSpeed[username] = averageSpeed;
                        await Task.Delay((int)msDelaySeed, cts);
                        return int.MaxValue;
                    }

                }
                else
                {
                    return int.MaxValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Firebase("DL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace);
                return int.MaxValue;
            }
        }

        //this is duplicated for speed.
        public static async Task<int> OurUploadGovernor(Soulseek.Transfer transfer, int requestedBytes, CancellationToken cts)
        {
            try
            {
                if (PreferencesState.SpeedLimitUploadOn)
                {
                    var username = transfer.Username;
                    var averageSpeed = transfer.AverageSpeed;

                    if (UploadUserDelays.TryGetValue(username, out double msDelay))
                    {
                        bool exists = UploadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                        if (exists && averageSpeed == lastAvgSpeed)
                        {
#if DEBUG
                            //System.Console.WriteLine("UL dont update");
#endif
                            //do not adjust as we have not yet recalculated the average speed
                            await Task.Delay((int)msDelay, cts);
                            return int.MaxValue;
                        }

                        UploadLastAvgSpeed[username] = averageSpeed;

                        double avgSpeed = averageSpeed;
                        if (!PreferencesState.SpeedLimitUploadIsPerTransfer && UploadLastAvgSpeed.Count > 1)
                        {

                            //its threadsafe when using linq on concurrent dict itself.
                            avgSpeed = UploadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
#if DEBUG
                            //System.Console.WriteLine("UL multiple total speed " + avgSpeed);
#endif
                        }

                        if (avgSpeed > PreferencesState.SpeedLimitUploadBytesSec)
                        {
#if DEBUG
                            //System.Console.WriteLine("UL speed too high " + averageSpeed + "   " + msDelay);
#endif
                            UploadUserDelays[username] = msDelay = msDelay * 1.04;

                        }
                        else
                        {
#if DEBUG
                            //System.Console.WriteLine("UL speed too low " + averageSpeed + "   " + msDelay);
#endif
                            UploadUserDelays[username] = msDelay = msDelay * 0.96;
                        }

                        await Task.Delay((int)msDelay, cts);
                        return int.MaxValue;
                    }
                    else
                    {
#if DEBUG
                        //System.Console.WriteLine("UL first time guess");
#endif
                        //first time we need to guess a decent value
                        //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                        double msDelaySeed = 1000 * 16384.0 / PreferencesState.SpeedLimitUploadBytesSec;
                        UploadUserDelays[username] = msDelaySeed;
                        UploadLastAvgSpeed[username] = averageSpeed;
                        await Task.Delay((int)msDelaySeed, cts);
                        return int.MaxValue;
                    }

                }
                else
                {
                    return int.MaxValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Firebase("UL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace); // TODO2026
                return int.MaxValue;
            }
        }

    }
}
