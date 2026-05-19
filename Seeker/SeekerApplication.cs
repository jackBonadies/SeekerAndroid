/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using Common;
using Common.Share;
using Seeker.Chatroom;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Messages;
using Seeker.Search;
using Seeker.Services;
using Seeker.Transfers;
using Seeker.UPnP;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.ContactsContract;

namespace Seeker
{
    [Application()]
    public partial class SeekerApplication : Application
    {
        public static Context ApplicationContext = null;

        public static PowerManager.WakeLock CpuKeepAlive_Transfer = null;
        public static Android.Net.Wifi.WifiManager.WifiLock WifiKeepAlive_Transfer = null;
        public static System.Timers.Timer KeepAliveInactivityKillTimer = null;

        public static void KeepAliveInactivityKillTimerEllapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (CpuKeepAlive_Transfer != null)
            {
                CpuKeepAlive_Transfer.Release();
            }
            if (WifiKeepAlive_Transfer != null)
            {
                WifiKeepAlive_Transfer.Release();
            }
            KeepAliveInactivityKillTimer.Stop();
        }
        public SeekerApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private static readonly ConnectionOptions ServerConnectionOptionsWithKeepAlive =
            new ConnectionOptions(configureSocket: ConfigureTcpKeepAlive);

        private static void ConfigureTcpKeepAlive(Socket socket)
        {
            try
            {
                int size = 4;
                byte[] keepAlive = new byte[size * 3];

                // Turn keepalive on
                Buffer.BlockCopy(BitConverter.GetBytes(1U), 0, keepAlive, 0, size);
                // Amount of time without activity before sending a keepalive (3s)
                Buffer.BlockCopy(BitConverter.GetBytes(3000U), 0, keepAlive, size, size);
                // Keepalive interval (2s)
                Buffer.BlockCopy(BitConverter.GetBytes(2000U), 0, keepAlive, size * 2, size);

                socket.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
            }
            catch (Exception)
            {
                // if we can't set keep alive, just continue on.
            }
        }

        public static bool DnsLookupFailed;

        private static async Task<IPAddress> ResolveAddressAsync(string address)
        {
            DnsLookupFailed = false;
            var dnsTask = Dns.GetHostEntryAsync(address);
            var completed = await Task.WhenAny(dnsTask, Task.Delay(3000)).ConfigureAwait(false);

            if (completed == dnsTask && dnsTask.Status == TaskStatus.RanToCompletion)
            {
                return dnsTask.Result.AddressList[0];
            }

            DnsLookupFailed = true;
            return IPAddress.Parse("208.76.170.59");
        }

        public const bool AUTO_CONNECT_ON = true;

        public static AndroidToaster Toaster { get; private set; }
        public override void OnCreate()
        {
            base.OnCreate();
            ApplicationContext = this;
            Toaster = new AndroidToaster();
            Services.SessionService.Instance = new Services.SessionService();
            Services.ReconnectService.Instance = new Services.ReconnectService();
            Services.FileSystemService.Instance = new Services.FileSystemService();

            var loggerBackend = new AndroidLoggerBackend();
#if !IzzySoft
            Firebase.FirebaseApp app = Firebase.FirebaseApp.InitializeApp(this);
            if (app == null)
            {
                loggerBackend.CrashlyticsEnabled = false;
            }
#endif
            Logger.Backend = loggerBackend;
            MicroTagReader.Instance = new MicroTagReader(loggerBackend);

            Services.DownloadService.Instance = new Services.DownloadService(Toaster, Services.FileSystemService.Instance, Services.SessionService.Instance, new Services.MainThreadRunner(), () => SeekerState.SoulseekClient, loggerBackend, new Services.AndroidNetworkStatus());
            Services.UserInfoPictureCacheService.Instance = new Services.UserInfoPictureCacheService();

#if DEBUG
            Android.OS.StrictMode.SetThreadPolicy(
                new Android.OS.StrictMode.ThreadPolicy.Builder()
                    .DetectDiskReads()
                    .DetectDiskWrites()
                    .DetectNetwork()
                    .DetectCustomSlowCalls()
                    .PenaltyLog()
                    .PenaltyFlashScreen()
                    .PenaltyDeathOnNetwork()
                    .Build());

            Android.OS.StrictMode.SetVmPolicy(
                new Android.OS.StrictMode.VmPolicy.Builder()
                    .DetectLeakedClosableObjects()
                    .DetectLeakedSqlLiteObjects()
                    .DetectActivityLeaks()
                    .PenaltyLog()
                    .Build());
#endif

            this.RegisterActivityLifecycleCallbacks(new ForegroundLifecycleTracker());
            this.RegisterReceiver(new ConnectionReceiver(), new IntentFilter(ConnectivityManager.ConnectivityAction));
            var sharedPrefs = this.GetSharedPreferences(Constants.SharedPrefFile, 0);
            SeekerState.SharedPreferences = sharedPrefs;

            //SerializationTests.PopulateSharedPreferencesFromFile(this, sharedPrefs);

            RestoreSeekerState(sharedPrefs, this);
            InitializeDocumentFiles(this);
            PreferencesManager.RestoreListeningStateLocked();
            UPnpManager.RestoreUpnpState();

            SeekerState.OffsetFromUtcCached = SimpleHelpers.GetDateTimeNowSafe().Subtract(DateTime.UtcNow);

            SeekerState.SystemLanguage = LocaleHelper.LocaleToString(Resources.Configuration.Locale);

            if (LocaleHelper.HasProperPerAppLanguageSupport())
            {
                if (!PreferencesState.LegacyLanguageMigrated)
                {
                    PreferencesState.LegacyLanguageMigrated = true;
                    PreferencesManager.SaveLegacyLanguageMigrated();
                    LocaleHelper.SetLanguage(PreferencesState.Language);
                }
            }
            else
            {
                LocaleHelper.SetLanguageLegacy(PreferencesState.Language, false);
            }

            //LogDebug("Default Night Mode: " + AppCompatDelegate.DefaultNightMode); //-100 = night mode unspecified, default on my Pixel 2. also on api22 emulator it is -100.
            //though setting it to -1 does not seem to recreate the activity or have any negative side effects..
            //this does not restart Android.App.Application. so putting it here is a much better place... in MainActivity.OnCreate it would restart the activity every time.
            if (AppCompatDelegate.DefaultNightMode != PreferencesState.DayNightMode)
            {
                AppCompatDelegate.DefaultNightMode = PreferencesState.DayNightMode;
            }

            //SeekerState.SharedPreferences = sharedPrefs;

            if (SeekerKeepAliveService.CpuKeepAlive_FullService == null)
            {
                SeekerKeepAliveService.CpuKeepAlive_FullService = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Keep Alive Service Cpu");
            }

            if (SeekerKeepAliveService.WifiKeepAlive_FullService == null)
            {
                SeekerKeepAliveService.WifiKeepAlive_FullService = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Keep Alive Service Wifi");
            }

            try
            {
                if (CpuKeepAlive_Transfer == null)
                {
                    CpuKeepAlive_Transfer = ((PowerManager)this.GetSystemService(Context.PowerService)).NewWakeLock(WakeLockFlags.Partial, "Seeker Download CPU_Keep_Alive");
                    CpuKeepAlive_Transfer.SetReferenceCounted(false);
                }
                if (WifiKeepAlive_Transfer == null)
                {
                    WifiKeepAlive_Transfer = ((Android.Net.Wifi.WifiManager)this.GetSystemService(Context.WifiService)).CreateWifiLock(Android.Net.WifiMode.FullHighPerf, "Seeker Download Wifi_Keep_Alive");
                    WifiKeepAlive_Transfer.SetReferenceCounted(false);
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("error init keepalives: " + e.Message + e.StackTrace);
            }
            if (KeepAliveInactivityKillTimer == null)
            {
                KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                        //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                KeepAliveInactivityKillTimer.Elapsed += KeepAliveInactivityKillTimerEllapsed;
                KeepAliveInactivityKillTimer.AutoReset = false;
            }

            NetworkStateService.SetNetworkState(this);

            //need search response and enqueue download action...
            //SeekerState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(messageTimeout: 30000, enableListener: false, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations:PreferencesState.AllowPrivateRoomInvitations)); //Enable Listener is False.  Default is True.
            #if DEBUG
                var _capRoot = Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
                if (_capRoot != null)
                {
                    Seeker.Debug.SearchCaptureStore.Configure(
                        System.IO.Path.Combine(_capRoot, "search_captures"),
                        Seeker.Debug.DebugSecrets.SeekerEncryptKey);
                }
            #endif
            #if MOCK
                SeekerState.SoulseekClient = new MockSoulseekClient();
                SharingService.TurnOnSharing();
            #else
                SeekerState.SoulseekClient = new SoulseekClient(
                    128,
                    new SoulseekClientOptions(
                        minimumDiagnosticLevel: PreferencesState.LogDiagnostics ? Soulseek.Diagnostics.DiagnosticLevel.Debug : Soulseek.Diagnostics.DiagnosticLevel.Info,
                        messageTimeout: 30000,
                        enableListener: PreferencesState.ListenerEnabled,
                        autoAcknowledgePrivateMessages: false,
                        acceptPrivateRoomInvitations: PreferencesState.AllowPrivateRoomInvitations,
                        listenPort: PreferencesState.ListenerPort,
                        maximumConcurrentDownloads: PreferencesState.LimitSimultaneousDownloads ? PreferencesState.MaxSimultaneousLimit : int.MaxValue,
                        maximumConcurrentSearches: 5,
                        serverConnectionOptions: ServerConnectionOptionsWithKeepAlive,
                        addressResolver: ResolveAddressAsync,
                        userInfoResolver: UserInfoResponder.HandleRequest));
            #endif
            SetDiagnosticState(PreferencesState.LogDiagnostics);
            SeekerState.SoulseekClient.UserStatisticsChanged += SoulseekClient_UserDataReceived;
            SeekerState.SoulseekClient.UserStatusChanged += UserStatusDeduplicator.Instance.OnUserStatusChanged;
            UserStatusDeduplicator.Instance.Deduplicated += SoulseekClient_UserStatusChanged;
            //SeekerState.SoulseekClient.TransferProgressUpdated += Upload_TransferProgressUpdated;
            Seeker.Transfers.TransferEventRouter.Wire(SeekerState.SoulseekClient);

            SeekerState.SoulseekClient.Connected += SoulseekClient_Connected;
            SeekerState.SoulseekClient.StateChanged += SoulseekClient_StateChanged;
            SeekerState.SoulseekClient.LoggedIn += SoulseekClient_LoggedIn;
            SeekerState.SoulseekClient.Disconnected += SoulseekClient_Disconnected;
            SeekerState.SoulseekClient.ServerInfoReceived += SoulseekClient_ServerInfoReceived;
            SeekerState.BrowseResponseReceived += BrowseFragment.SeekerState_BrowseResponseReceived;

            SeekerState.SoulseekClient.PrivilegedUserListReceived += SoulseekClient_PrivilegedUserListReceived;
            SeekerState.SoulseekClient.ExcludedSearchPhrasesReceived += SoulseekClient_ExcludedSearchPhrasesReceived;

            MessageController.Initialize();
            ChatroomController.Initialize();


            SoulseekClient.OnTransferSizeMismatchFunc = OnTransferSizeMismatchFunc;
            #if DEBUG
            SoulseekClient.ErrorLogHandler += SoulseekClient_ErrorLogHandler;
            SoulseekClient.DebugLogHandler += DebugLogHandler;
            #endif


            UPnpManager.Context = this;
            UPnpManager.Instance.SearchAndSetMappingIfRequired();
            SimpleHelpers.STRINGS_KBS = this.Resources.GetString(Resource.String.kilobytes_per_second);
            SimpleHelpers.STRINGS_KHZ = this.Resources.GetString(Resource.String.kilohertz);

            SimpleHelpers.UserListService = UserListService.Instance;
        }

        private static bool CheckDirectoryForWritePermission(Context context, Android.Net.Uri chosenUri, bool directoryUriFromTree, string logContext)
        {
            bool canWrite = false;
            try
            {
                if (!directoryUriFromTree)
                {
                    canWrite = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path)).CanWrite();
                }
                else
                {
                    canWrite = DocumentFile.FromTreeUri(context, chosenUri).CanWrite();
                }
            }
            catch (Exception e)
            {
                if (chosenUri != null)
                {
                    Logger.Firebase($"{logContext} DocumentFile.FromTreeUri failed with URI: " + chosenUri.ToString() + " " + e.Message + " scheme " + chosenUri.Scheme);
                }
                else
                {
                    Logger.Firebase($"{logContext} DocumentFile.FromTreeUri failed with null URI");
                }
            }
            if (!canWrite)
            {
                Logger.Firebase($"canWrite = false for {logContext} Uri: " + chosenUri.ToString());
            }
            return canWrite;
        }

        // Runs once per process in OnCreate. Sets SeekerState.RootDocumentFile and
        // RootIncompleteDocumentFile. For non-legacy, leaves RootDocumentFile null if the
        // download directory permission has been revoked; MainActivity checks for null and
        // shows the re-selection dialog.
        private static void InitializeDocumentFiles(Context context)
        {
            if (PlatformInfo.UseLegacyStorage())
            {
                if (!string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                {
                    var chosenUri = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, chosenUri, PreferencesState.SaveDataDirectoryUriIsFromTree, "legacy download"))
                    {
                        SeekerState.RootDocumentFile = SeekerState.OpenRootFile(context, chosenUri);
                    }
                }
                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    var chosenUri = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, chosenUri, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree, "legacy incomplete"))
                    {
                        SeekerState.RootIncompleteDocumentFile = SeekerState.OpenRootFile(context, chosenUri);
                    }
                }
            }
            else
            {
                Android.Net.Uri res = string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri)
                    ? Android.Net.Uri.Parse(SeekerState.DefaultMusicUri)
                    : Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);

                if (CheckDirectoryForWritePermission(context, res, PreferencesState.SaveDataDirectoryUriIsFromTree, "download"))
                {
                    SeekerState.RootDocumentFile = PreferencesState.SaveDataDirectoryUriIsFromTree
                        ? DocumentFile.FromTreeUri(context, res)
                        : DocumentFile.FromFile(new Java.IO.File(res.Path));
                }
                // else: RootDocumentFile stays null — MainActivity will detect this and show the re-selection dialog

                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    var incompleteRes = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, incompleteRes, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree, "incomplete"))
                    {
                        SeekerState.RootIncompleteDocumentFile = (!PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree)
                            ? DocumentFile.FromFile(new Java.IO.File(incompleteRes.Path))
                            : DocumentFile.FromTreeUri(context, incompleteRes);
                    }
                }
            }
        }

        private void SoulseekClient_ExcludedSearchPhrasesReceived(object sender, IReadOnlyCollection<string> exludedPhrasesList)
        {
            SearchUtil.ExcludedSearchPhrases = exludedPhrasesList;
        }


        public static void SetDiagnosticState(bool log_diagnostics)
        {
            if (log_diagnostics)
            {
                DiagnosticFileWriter.Subscribe();
                AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            }
            else
            {
                DiagnosticFileWriter.Unsubscribe();
                AndroidEnvironment.UnhandledExceptionRaiser -= AndroidEnvironment_UnhandledExceptionRaiser;
            }
        }

        public static void DebugLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            Logger.Debug(e.Message);
        }

        public static void SoulseekClient_ErrorLogHandler(object sender, SoulseekClient.ErrorLogEventArgs e)
        {
            if (e?.Message != null)
            {
                if (e.Message.Contains("Operation timed out"))
                {
                    //this happens to me all the time and it is literally fine
                    return;
                }
            }
            Logger.Firebase(e.Message);
        }

        private static void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            //by default e.Handled == false. and this does go on to crash the process (which is good imo, I only want this for logging purposes).
            Logger.Debug(e.Exception.Message);
            Logger.Debug(e.Exception.StackTrace);
        }

        private void SoulseekClient_Disconnected(object sender, SoulseekClientDisconnectedEventArgs e)
        {
            Logger.Debug("disconnected");
            lock (OurCurrentLoginTaskSyncObject)
            {
                OurCurrentLoginTask = null;
            }
        }

        public static void AcquireTransferLocksAndResetTimer()
        {
            if (CpuKeepAlive_Transfer != null && !CpuKeepAlive_Transfer.IsHeld)
            {
                CpuKeepAlive_Transfer.Acquire();
            }
            if (WifiKeepAlive_Transfer != null && !WifiKeepAlive_Transfer.IsHeld)
            {
                WifiKeepAlive_Transfer.Acquire();
            }

            if (KeepAliveInactivityKillTimer != null)
            {
                KeepAliveInactivityKillTimer.Stop(); //can be null
                KeepAliveInactivityKillTimer.Start(); //reset the timer..
            }
            else
            {
                KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                                     //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                KeepAliveInactivityKillTimer.Elapsed += KeepAliveInactivityKillTimerEllapsed;
                KeepAliveInactivityKillTimer.AutoReset = false;
            }
        }

        public static void ReleaseTransferLocksIfServicesComplete()
        {
            //if all transfers are done..
            if (!SeekerState.UploadKeepAliveServiceRunning && !SeekerState.DownloadKeepAliveServiceRunning)
            {
                if (CpuKeepAlive_Transfer != null)
                {
                    CpuKeepAlive_Transfer.Release();
                }
                if (WifiKeepAlive_Transfer != null)
                {
                    WifiKeepAlive_Transfer.Release();
                }
                if (KeepAliveInactivityKillTimer != null)
                {
                    KeepAliveInactivityKillTimer.Stop();
                }
            }
        }

        public const string ACTION_SHUTDOWN = "SeekerApplication_AppShutDown";

        public static bool IsShuttingDown(Intent intent)
        {
            if (intent?.Action == null)
            {
                return false;
            }
            if (intent.Action == ACTION_SHUTDOWN)
            {
                return true;
            }
            return false;
        }



        internal static int _activeUploadCount = 0;
        public static int ActiveUploadCount => _activeUploadCount;

        internal static int _activeDownloadCount = 0;
        public static int ActiveDownloadCount => _activeDownloadCount;

        internal static void NotifyUploadCountChanged(int count)
        {
            if (Android.App.Application.Context is SeekerApplication app)
            {
                app.OnUploadCountChanged(count);
            }
        }

        internal static void NotifyDownloadCountChanged(int count)
        {
            if (Android.App.Application.Context is SeekerApplication app)
            {
                app.OnDownloadCountChanged(count);
            }
        }

        public static void KickKeepAliveTimer()
        {
            try
            {
                KeepAliveInactivityKillTimer?.Stop();
                KeepAliveInactivityKillTimer?.Start();
            }
            catch (System.Exception err)
            {
                Logger.Firebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }
        }

        private void OnUploadCountChanged(int count)
        {
            bool abortAll = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.AbortAllWasPressedDebouncer) < 750;
            if (count <= 0 || abortAll)
            {
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                Logger.Debug("Stop Service");
                this.StopService(uploadServiceIntent);
                SeekerState.UploadKeepAliveServiceRunning = false;
            }
            else if (!SeekerState.UploadKeepAliveServiceRunning)
            {
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    bool? isForeground = SeekerState.ActiveActivityRef?.IsResumed();
                    if (isForeground ?? false)
                    {
                        this.StartService(uploadServiceIntent);
                    }
                }
                else
                {
                    this.StartService(uploadServiceIntent);
                }
                SeekerState.UploadKeepAliveServiceRunning = true;
            }
            else
            {
                string msg = count == 1
                    ? string.Format(UploadForegroundService.SingularUploadRemaining, count)
                    : string.Format(UploadForegroundService.PluralUploadsRemaining, count);
                var notif = UploadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(UploadForegroundService.NOTIF_ID, notif);
            }
        }

        private void OnDownloadCountChanged(int count)
        {
            bool cancelAndClear = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.CancelAndClearAllWasPressedDebouncer) < 750;
            if (count <= 0 || cancelAndClear)
            {
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                Logger.Debug("Stop Service");
                this.StopService(downloadServiceIntent);
                SeekerState.DownloadKeepAliveServiceRunning = false;
            }
            else if (!SeekerState.DownloadKeepAliveServiceRunning)
            {
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    bool? isForeground = SeekerState.ActiveActivityRef?.IsResumed();
                    if (isForeground ?? false)
                    {
                        this.StartService(downloadServiceIntent);
                    }
                }
                else
                {
                    this.StartService(downloadServiceIntent);
                }
                SeekerState.DownloadKeepAliveServiceRunning = true;
            }
            else
            {
                string msg = count == 1
                    ? string.Format(DownloadForegroundService.SingularDownloadRemaining, count)
                    : string.Format(DownloadForegroundService.PluralDownloadsRemaining, count);
                var notif = DownloadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(DownloadForegroundService.NOTIF_ID, notif);
            }
        }




        public static bool OnTransferSizeMismatchFunc(System.IO.Stream fileStream, string fullFilename, string username, long startOffset, long oldSize, long newSize, string incompleteUriString, out System.IO.Stream newStream)
        {
            newStream = null;
            try
            {
                var relevantItem = TransferItems.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(fullFilename, username, false, out _);
                if (startOffset == 0)
                {
                    // all we need to do is update the size.
                    relevantItem.Size = newSize;
                    Logger.Debug("updated the size");
                }
                else
                {
                    // we need to truncate the incomplete file and set our progress back to 0.
                    relevantItem.Size = newSize;
                    //fileStream.SetLength(0); //this is not supported. we cannot do seek.
                    //fileStream.Flush();

                    fileStream.Close();
                    bool useDownloadDir = false;
                    if (PreferencesState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
                    {
                        useDownloadDir = true;
                    }

                    var incompleteUri = Android.Net.Uri.Parse(incompleteUriString);

                    // this is the only time we do legacy.
                    bool isLegacyCase = PlatformInfo.UseLegacyStorage() && (SeekerState.RootDocumentFile == null && useDownloadDir);
                    if (isLegacyCase)
                    {
                        newStream = new System.IO.FileStream(incompleteUri.Path, System.IO.FileMode.Truncate, System.IO.FileAccess.Write, System.IO.FileShare.None);
                    }
                    else
                    {
                        newStream = SeekerState.ActiveActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wt");
                    }

                    relevantItem.Progress = 0;
                    Logger.Debug("truncated the file and updated the size");
                }

            }
            catch (Exception e)
            {
                Logger.Debug("OnTransferSizeMismatchFunc: " + e.ToString());
                return false;
            }
            return true;
        }


        private void SoulseekClient_PrivilegedUserListReceived(object sender, IReadOnlyCollection<string> privilegedUsers)
        {
            PrivilegesManager.Instance.SetPrivilegedList(privilegedUsers);
        }

        private void SoulseekClient_ServerInfoReceived(object sender, ServerInfo e)
        {
            if (e.WishlistInterval.HasValue)
            {
                WishlistController.SearchIntervalMilliseconds = e.WishlistInterval.Value * 1000;
                WishlistController.Initialize();
            }
            else
            {
                Logger.Debug("wishlist interval is null");
            }
        }

        //I only care about the Connected, LoggedIn to Disconnecting case.  
        //the next case is Disconnecting to Disconnected.

        //then for failed retries you get
        //disconnected to connecting
        //connecting to disconnecting
        //disconnecting to disconnected.
        //so be wary of the disconnected event...

        private void SoulseekClient_StateChanged(object sender, SoulseekClientStateChangedEventArgs e)
        {
            Logger.Debug("Prev: " + e.PreviousState.ToString() + " Next: " + e.State.ToString());
            if (e.PreviousState.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Disconnecting))
            {
                Logger.Debug("!! changing from connected to disconnecting");


                if (e.Exception is KickedFromServerException)
                {
                    Logger.Debug("Kicked Kicked Kicked");
                    if (SeekerState.ActiveActivityRef != null)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.kicked_due_to_other_client), ToastLength.Long);
                    }
                    return; //DO NOT RETRY!!! or will do an infinite loop!
                }
                if (e.Exception is System.ObjectDisposedException)
                {
                    return; //DO NOT RETRY!!! we are shutting down
                }



                //this is a "true" connected to disconnected
                ChatroomController.ClearAndCacheJoined();
                Logger.Debug("disconnected " + DateTime.UtcNow.ToString());
                if (e.Message == LoginFragment.LogoutMessage)
                {
                    // User intentionally logged out — do not reconnect
                }
                else if (AUTO_CONNECT_ON && PreferencesState.CurrentlyLoggedIn)
                {
                    ReconnectService.Instance.Start();
                }
            }
            else if (e.PreviousState.HasFlag(SoulseekClientStates.Disconnected))
            {
                Logger.Debug("!! changing from disconnected to trying to connect");
            }
            else if (e.State.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Connected))
            {
                Logger.Debug("!! changing trying to connect to successfully connected");
            }
        }

        private void SoulseekClient_LoggedIn(object sender, EventArgs e)
        {
            lock (OurCurrentLoginTaskSyncObject)
            {
                OurCurrentLoginTask = null;
            }
            ChatroomController.JoinAutoJoinRoomsAndPreviousJoined();
            //ChatroomController.ConnectionLapse.Add(new Tuple<bool, DateTime>(true, DateTime.UtcNow)); //just testing obv remove later...
            Logger.Debug("logged in " + DateTime.UtcNow.ToString());
            Logger.Debug("Listening State: " + SeekerState.SoulseekClient.GetListeningState());
            if (PreferencesState.ListenerEnabled && !SeekerState.SoulseekClient.GetListeningState())
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("SeekerState.ActiveActivityRef null SoulseekClient_LoggedIn");
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.port_already_in_use), ToastLength.Short); //todo is this supposed to be here...
                }
            }
        }

        private void SoulseekClient_Connected(object sender, EventArgs e)
        {
            //ChatroomController.SetConnectionLapsedMessage(true);
            Logger.Debug("connected " + DateTime.UtcNow.ToString());

        }

        public static string GetString(int resId)
        {
            return SeekerApplication.ApplicationContext.GetString(resId);
        }

        public static void SetUpLoginContinueWith(Task t)
        {
            if (t == null)
            {
                return;
            }
            if (SharedFileService.MeetsSharingConditions())
            {

                Action<Task> getAndSetLoggedInInfoAction = new Action<Task>((Task t) =>
                {
                    //we want to
                    //UpdateStatus ??
                    //inform server if we are sharing..
                    //get our upload speed..
                    if (t.Status == TaskStatus.Faulted || t.IsFaulted || t.IsCanceled)
                    {
                        return;
                    }
                    SharedFileService.InformServerOfSharedFiles();
                    SeekerState.SoulseekClient.GetUserStatisticsAsync(PreferencesState.Username);
                });
                t.ContinueWith(getAndSetLoggedInInfoAction);
            }
        }

        public static Task OurCurrentLoginTask = null;
        public static object OurCurrentLoginTaskSyncObject = new object();
        public static Task ConnectAndPerformPostConnectTasks(string username, string password)
        {
            Task t = SeekerState.SoulseekClient.ConnectAsync(username, password);
            OurCurrentLoginTask = t;
            t.ContinueWith(PerformPostConnectTasks);
            return t;
        }

        public static void PerformPostConnectTasks(Task t)
        {
            if (t.IsCompletedSuccessfully)
            {
                try
                {
                    lock (CommonState.UserList)
                    {
                        foreach (UserListItem item in CommonState.UserList)
                        {
                            Logger.Debug("adding user: " + item.Username);
                            SeekerState.SoulseekClient.WatchUserAsync(item.Username).ContinueWith(UpdateUserInfo);
                        }
                    }

                    lock (TransferState.UsersWhereDownloadFailedDueToOffline)
                    {
                        foreach (string userDownloadOffline in TransferState.UsersWhereDownloadFailedDueToOffline.Keys)
                        {
                            Logger.Debug("adding user (due to a download we wanted from them when they were offline): " + userDownloadOffline);
                            SeekerState.SoulseekClient.WatchUserAsync(userDownloadOffline).ContinueWith(UpdateUserOfflineDownload);
                        }
                    }

                    //this is if we wanted to change the status earlier but could not. note that when we first login, our status is Online by default.
                    //so no need to change it to online.
                    if (SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.OnlinePending)
                    {
                        //we just did this by logging in...
                        Logger.Debug("online was pending");
                        SeekerState.PendingStatusChangeToAwayOnline = SeekerState.PendingStatusChange.NothingPending;
                    }
                    else if (((SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.AwayPending || SeekerState.OurCurrentStatusIsAway)))
                    {
                        Logger.Debug("a change to away was pending / our status is away. lets set it now");

                        if (SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.AwayPending)
                        {
                            Logger.Debug("pending that is....");
                        }
                        else
                        {
                            Logger.Debug("current that is...");
                        }

                        if (ForegroundLifecycleTracker.NumberOfActiveActivities != 0)
                        {
                            Logger.Debug("There is a hole in our logic!!! the pendingstatus and/or current status should not be away!!!");
                        }
                        else
                        {
                            SessionService.Instance.SetStatusApi(true);
                        }
                    }

                    //if the number of directories is stale (meaning it changing when we werent logged in and so we could not update the server)
                    //and we have not yet attempted to set up sharing (since after we attempt to set up sharing we will notify the server)
                    //then tell the server here.
                    //this makes it so that we tell the server once when Seeker first launches, and when things change, but not every time
                    //we log in.
                    if (SharedFileService.NumberOfSharedDirectoriesIsStale && SharedFileService.AttemptedToSetUpSharing)
                    {
                        Logger.Debug("stale and we already attempted to set up sharing, so lets do it here in post log in.");
                        SharedFileService.InformServerOfSharedFiles();
                    }

                    TransfersController.InitializeService();
                }
                catch (Exception e)
                {
                    Logger.Firebase("PerformPostConnectTasks" + e.Message + e.StackTrace);
                }
            }
        }


        /// <summary>
        /// UserStatusChanged will not get called until an actual change. hence this call..
        /// </summary>
        /// <param name="t"></param>
        private static void UpdateUserOfflineDownload(Task<UserData> t)
        {
            if (t.IsCompletedSuccessfully)
            {
                Seeker.Services.DownloadService.Instance.RetryDownloadsIfUserBackOnline(t.Result.Username, t.Result.Status);
            }
        }

        /// <summary>
        /// UserStatusChanged will not get called until an actual change. hence this call..
        /// </summary>
        /// <param name="t"></param>
        private static void UpdateUserInfo(Task<UserData> t)
        {
            try
            {
                Logger.Debug("Update User Info Received");
                if (t.IsCompletedSuccessfully)
                {
                    string username = t.Result.Username;
                    Logger.Debug("Update User Info: " + username + " status: " + t.Result.Status.ToString());
                    if (UserListService.Instance.ContainsUser(username))
                    {
                        UserListService.Instance.AddUser(t.Result, t.Result.Status);
                    }


                }
                else if (t.Exception?.InnerException is UserNotFoundException)
                {
                    if (t.Exception.InnerException.Message.Contains("User ") && t.Exception.InnerException.Message.Contains("does not exist"))
                    {
                        string username = t.Exception.InnerException.Message.Split(null)[1];
                        if (UserListService.Instance.ContainsUser(username))
                        {
                            UserListService.Instance.SetDoesNotExist(username);
                        }
                    }
                    else
                    {
                        Logger.Firebase("unexcepted error message - " + t.Exception.InnerException.Message);
                    }
                }
                else
                {
                    //timeout
                    Logger.Firebase("UpdateUserInfo case 3 " + t.Exception.Message);
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("UpdateUserInfo" + e.Message + e.StackTrace);
            }
        }

        public static List<WeakReference<ThemeableActivity>> Activities = new List<WeakReference<ThemeableActivity>>();

        public static void RecreateActivies()
        {
            foreach (var weakRef in Activities)
            {
                if (weakRef.TryGetTarget(out var themeableActivity))
                {
                    themeableActivity.Recreate();
                }
            }
        }

        public const string CHANNEL_ID_USER_ONLINE = "User Online Alerts ID";
        public const string CHANNEL_NAME_USER_ONLINE = "User Online Alerts";
        public const string FromUserOnlineAlert = "FromUserOnlineAlert";
        public static void ShowNotificationForUserOnlineAlert(string username)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID_USER_ONLINE, CHANNEL_NAME_USER_ONLINE, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(UserListActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop);
                    notifIntent.PutExtra(FromUserOnlineAlert, true);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, username.GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID_USER_ONLINE, "User Online", string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.user_X_is_now_online), username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify(username.GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    Logger.Firebase("ShowNotificationForUserOnlineAlert failed: " + e.Message + e.StackTrace);
                }
            });
        }


        public const string CHANNEL_ID_FOLDER_ALERT = "Folder Finished Downloading Alerts ID";
        public const string CHANNEL_NAME_FOLDER_ALERT = "Folder Finished Downloading Alerts";

        public static void ShowNotificationForCompletedFolder(string foldername, string username)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID_FOLDER_ALERT, CHANNEL_NAME_FOLDER_ALERT, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront); //otherwise if another activity is in front then this intent will do nothing...
                    notifIntent.PutExtra(MainActivity.FolderAlertExtra, true);
                    notifIntent.PutExtra(MainActivity.FolderAlertUsernameExtra, username);
                    notifIntent.PutExtra(MainActivity.FolderAlertFoldernameExtra, foldername);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, (foldername + username).GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID_FOLDER_ALERT, SeekerApplication.GetString(Resource.String.FolderFinishedDownloading), string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.folder_X_from_user_Y_finished), foldername, username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify((foldername + username).GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    Logger.Firebase("ShowNotificationForCompletedFolder failed: " + e.Message + e.StackTrace);
                }
            });
        }

        public static void RestoreSeekerState(ISharedPreferences sharedPreferences, Context c)
        {
            if (sharedPreferences != null)
            {
                // Restore all pure-data preferences via PreferencesManager
                PreferencesManager.RestoreAll(sharedPreferences);
                PreferencesManager.RestoreListeningState(sharedPreferences);

                // Side-effect restores that depend on Android APIs
                UploadDirectoryManager.RestoreFromSavedState(sharedPreferences);

                CommonState.UserList = SerializationHelper.RestoreUserListFromString(sharedPreferences.GetString(KeyConsts.M_UserList, string.Empty));
                RestoreRecentUsersManagerFromString(sharedPreferences.GetString(KeyConsts.M_RecentUsersList, string.Empty));
                CommonState.IgnoreUserList = SerializationHelper.RestoreUserListFromString(sharedPreferences.GetString(KeyConsts.M_IgnoreUserList, string.Empty));

                SeekerState.UserNotes = SerializationHelper.RestoreUserNotesFromString(sharedPreferences.GetString(KeyConsts.M_UserNotes, string.Empty));
                SeekerState.UserOnlineAlerts = SerializationHelper.RestoreUserOnlineAlertsFromString(sharedPreferences.GetString(KeyConsts.M_UserOnlineAlerts, string.Empty));

                SearchTabHelper.RestoreHeadersFromSharedPreferences();
                SettingsActivity.RestoreAdditionalDirectorySettingsFromSharedPreferences();

                if (TransferItems.TransferItemManagerDL == null)
                {
                    TransferPersistenceWrapper.RestoreDownloadTransferItems(sharedPreferences);
                    TransferPersistenceWrapper.RestoreUploadTransferItems(sharedPreferences);
                    TransferItems.TransferItemManagerWrapped = new TransferItemManagerWrapper(TransferItems.TransferItemManagerUploads, TransferItems.TransferItemManagerDL, TransferCleanup.PerformCleanupItem);
                }
            }
        }

        public static void RestoreSmartFilterState(ISharedPreferences sharedPreferences)
        {
            PreferencesManager.RestoreSmartFilterState(sharedPreferences);
        }

        public static void SaveSmartFilterState()
        {
            PreferencesManager.SaveSmartFilterState();
        }

        public static void RestoreRecentUsersManagerFromString(string xmlRecentUsersList)
        {
            //if empty then this is the first time creating it.  initialize it with our list of added users.
            SeekerState.RecentUsersManager = new RecentUserManager();
            if (xmlRecentUsersList == string.Empty)
            {
                int count = CommonState.UserList?.Count ?? 0;
                if (count > 0)
                {
                    SeekerState.RecentUsersManager.SetRecentUserList(CommonState.UserList.Select(uli => uli.Username).ToList());
                }
                else
                {
                    SeekerState.RecentUsersManager.SetRecentUserList(new List<string>());
                }
            }
            else
            {
                List<string> recentUsers = new List<string>();
                using (var stream = new System.IO.StringReader(xmlRecentUsersList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(recentUsers.GetType()); //this happens too often not allowing new things to be properly stored..
                    SeekerState.RecentUsersManager.SetRecentUserList(serializer.Deserialize(stream) as List<string>);
                }
            }
        }

        public static void SaveRecentUsers()
        {
            string recentUsersStr;
            List<string> recentUsers = SeekerState.RecentUsersManager.GetRecentUserList();
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(recentUsers.GetType());
                serializer.Serialize(writer, recentUsers);
                recentUsersStr = writer.ToString();
            }
            PreferencesManager.SaveRecentUsers(recentUsersStr);
        }

        /// <summary>
        /// This is from the server after sending it a UserData request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoulseekClient_UserDataReceived(object sender, UserStatistics e)
        {
            Logger.Debug("User Data Received: " + e.Username);
            var userData = SimpleHelpers.UserStatisticsToUserData(e);
            if (e.Username == PreferencesState.Username)
            {
                PreferencesState.UploadSpeed = e.AverageSpeed; //bytes
            }
            else
            {
                if (CommonState.UserList == null)
                {
                    Logger.Firebase("UserList is null on user data receive");
                }
                else
                {
                    UserListService.Instance.UpdateExistingUser(e.Username, userData, null, out _);
                }

                RequestedUserInfoHelper.AddIfRequestedUser(e.Username, userData, null, null);
            }
        }

        public static EventHandler<string> UserStatusChangedUIEvent;
        private void SoulseekClient_UserStatusChanged(object sender, UserStatus e)
        {
            if (e.Username == PreferencesState.Username)
            {
                //not sure this will ever happen
                return;
            }
            //we get user status changed for those we are in the same room as us
            if (CommonState.UserList != null)
            {
                var status = new UserStatus(e.Username, e.Presence, e.IsPrivileged);
                bool found = UserListService.Instance.UpdateExistingUser(e.Username, null, status, out bool cameOnline);
                if (found)
                {
                    Logger.Debug("friend status changed " + e.Username);
                    SeekerApplication.UserStatusChangedUIEvent?.Invoke(null, e.Username);
                    if (cameOnline && SeekerState.UserOnlineAlerts != null && SeekerState.UserOnlineAlerts.ContainsKey(e.Username))
                    {
                        ShowNotificationForUserOnlineAlert(e.Username);
                    }
                }
            }

            Seeker.Services.DownloadService.Instance.RetryDownloadsIfUserBackOnline(e.Username, e.Presence);
        }
    }
}