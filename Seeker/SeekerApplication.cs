﻿/*
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
using Seeker.Chatroom;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Messages;
using Seeker.Search;
using Seeker.Serialization;
using Seeker.Transfers;
using Seeker.UPnP;
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
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SlskHelp;

namespace Seeker
{
    [Application()]
    public class SeekerApplication : Application
    {
        public static Context ApplicationContext = null;
        public SeekerApplication(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public const bool AUTO_CONNECT_ON = true;
        public static bool LOG_DIAGNOSTICS = false;

        public override void OnCreate()
        {
            base.OnCreate();
            ApplicationContext = this;
#if !IzzySoft
            Firebase.FirebaseApp app = Firebase.FirebaseApp.InitializeApp(this);
            if (app == null)
            {
                MainActivity.crashlyticsEnabled = false;
            }
#endif
            //MainActivity.LogFirebase("testing release");

            this.RegisterActivityLifecycleCallbacks(new ForegroundLifecycleTracker());
            this.RegisterReceiver(new ConnectionReceiver(), new IntentFilter(ConnectivityManager.ConnectivityAction));
            var sharedPrefs = this.GetSharedPreferences("SoulSeekPrefs", 0);
            SeekerState.SharedPreferences = sharedPrefs;

            //SerializationTests.PopulateSharedPreferencesFromFile(this, sharedPrefs);

            RestoreSeekerState(sharedPrefs, this);
            RestoreListeningState();
            UPnpManager.RestoreUpnpState();

            SeekerState.OffsetFromUtcCached = CommonHelpers.GetDateTimeNowSafe().Subtract(DateTime.UtcNow);

            SeekerState.SystemLanguage = LocaleToString(Resources.Configuration.Locale);

            if (HasProperPerAppLanguageSupport())
            {
                if (!SeekerState.LegacyLanguageMigrated)
                {
                    SeekerState.LegacyLanguageMigrated = true;
                    lock (MainActivity.SHARED_PREF_LOCK)
                    {
                        var editor = this.GetSharedPreferences("SoulSeekPrefs", 0).Edit();
                        editor.PutBoolean(KeyConsts.M_LegacyLanguageMigrated, SeekerState.LegacyLanguageMigrated);
                        editor.Commit();
                    }
                    SetLanguage(SeekerState.Language);
                }
            }
            else
            {
                SetLanguageLegacy(SeekerState.Language, false);
            }

            //LogDebug("Default Night Mode: " + AppCompatDelegate.DefaultNightMode); //-100 = night mode unspecified, default on my Pixel 2. also on api22 emulator it is -100.
            //though setting it to -1 does not seem to recreate the activity or have any negative side effects..
            //this does not restart Android.App.Application. so putting it here is a much better place... in MainActivity.OnCreate it would restart the activity every time.
            if (AppCompatDelegate.DefaultNightMode != SeekerState.DayNightMode)
            {
                AppCompatDelegate.DefaultNightMode = SeekerState.DayNightMode;
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

            SeekerApplication.SetNetworkState(this);

            if (SeekerState.SoulseekClient == null)
            {
                //need search response and enqueue download action...
                //SeekerState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(messageTimeout: 30000, enableListener: false, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations:SeekerState.AllowPrivateRoomInvitations)); //Enable Listener is False.  Default is True.
                SeekerState.SoulseekClient = new SoulseekClient(new SoulseekClientOptions(minimumDiagnosticLevel: LOG_DIAGNOSTICS ? Soulseek.Diagnostics.DiagnosticLevel.Debug : Soulseek.Diagnostics.DiagnosticLevel.Info, messageTimeout: 30000, enableListener: SeekerState.ListenerEnabled, autoAcknowledgePrivateMessages: false, acceptPrivateRoomInvitations: SeekerState.AllowPrivateRoomInvitations, listenPort: SeekerState.ListenerPort, userInfoResponseResolver: UserInfoResponseHandler));
                SetDiagnosticState(LOG_DIAGNOSTICS);
                SeekerState.SoulseekClient.UserDataReceived += SoulseekClient_UserDataReceived;
                SeekerState.SoulseekClient.UserStatusChanged += SoulseekClient_UserStatusChanged_Deduplicator;
                SeekerApplication.UserStatusChangedDeDuplicated += SoulseekClient_UserStatusChanged;
                //SeekerState.SoulseekClient.TransferProgressUpdated += Upload_TransferProgressUpdated;
                SeekerState.SoulseekClient.TransferStateChanged += Upload_TransferStateChanged;

                SeekerState.SoulseekClient.TransferProgressUpdated += SoulseekClient_TransferProgressUpdated;
                SeekerState.SoulseekClient.TransferStateChanged += SoulseekClient_TransferStateChanged;

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
                SoulseekClient.ErrorLogHandler += MainActivity.SoulseekClient_ErrorLogHandler;

                SoulseekClient.DebugLogHandler += MainActivity.DebugLogHandler;

                SoulseekClient.DownloadAddedRemovedInternal += SoulseekClient_DownloadAddedRemovedInternal;
                SoulseekClient.UploadAddedRemovedInternal += SoulseekClient_UploadAddedRemovedInternal;
            }

            UPnpManager.Context = this;
            UPnpManager.Instance.SearchAndSetMappingIfRequired();
            SlskHelp.CommonHelpers.STRINGS_KBS = this.Resources.GetString(Resource.String.kilobytes_per_second);
            SlskHelp.CommonHelpers.STRINGS_KHZ = this.Resources.GetString(Resource.String.kilohertz);

            SlskHelp.CommonHelpers.UserListChecker = new UserListChecker();

            //shouldnt we also connect??? TODO TODO


        }

        private void SoulseekClient_ExcludedSearchPhrasesReceived(object sender, IReadOnlyCollection<string> exludedPhrasesList)
        {
            SearchUtil.ExcludedSearchPhrases = exludedPhrasesList;
        }

        public static string GetLegacyLanguageString()
        {
            if (SeekerApplication.HasProperPerAppLanguageSupport())
            {
                var lm = (LocaleManager)Context.GetSystemService(Context.LocaleService);
                LocaleList appLocales = lm.ApplicationLocales;
                if (appLocales.IsEmpty)
                {
                    return SeekerState.FieldLangAuto;
                }
                else
                {
                    Java.Util.Locale locale = appLocales.Get(0);
                    string lang = locale.Language; // ex. fr, uk
                    if (lang == "pt")
                    {
                        return SeekerState.FieldLangPtBr;
                    }
                    return lang;
                }
            }
            else
            {
                return SeekerState.Language;
            }
        }

        /// <summary>
        /// converts say "pt-rBR" to "pt-BR"
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        public static string FormatLocaleFromResourcesToStandard(string locale)
        {
            if (locale.Length == 6 && locale.Contains("-r"))
            {
                return locale.Replace("-r", "-");
            }
            else
            {
                return locale;
            }
        }

        public static bool HasProperPerAppLanguageSupport()
        {
            return (int)Android.OS.Build.VERSION.SdkInt >= 33;
        }

        public static Java.Util.Locale LocaleFromString(string localeString)
        {
            Java.Util.Locale locale = null;
            if (localeString.Contains("-r"))
            {
                var parts = localeString.Replace("-r", "-").Split('-');
                locale = new Java.Util.Locale(parts[0], parts[1]);
            }
            else
            {
                locale = new Java.Util.Locale(localeString);
            }
            return locale;
        }


        public void SetLanguage(string language)
        {
            if (HasProperPerAppLanguageSupport())
            {
                var lm = (LocaleManager)ApplicationContext.GetSystemService(Context.LocaleService);

                if (language == SeekerState.FieldLangAuto)
                {
                    lm.ApplicationLocales = LocaleList.EmptyLocaleList;
                }
                else
                {
                    lm.ApplicationLocales = LocaleList.ForLanguageTags(FormatLocaleFromResourcesToStandard(language));
                }
            }
            else
            {
                SetLanguageLegacy(SeekerState.Language, true);
            }
        }

        public void SetLanguageLegacy(string language, bool changed)
        {
            string localeString = language;
            var res = this.Resources;
            var config = res.Configuration;
            var displayMetrics = res.DisplayMetrics;

            var currentLocale = config.Locale;

            if (LocaleToString(currentLocale) == language)
            {
                return;
            }

            if (language == SeekerState.FieldLangAuto && SeekerState.SystemLanguage == LocaleToString(currentLocale))
            {
                return;
            }


            Java.Util.Locale locale = language != SeekerState.FieldLangAuto ? LocaleFromString(localeString) : LocaleFromString(SeekerState.SystemLanguage);

            Java.Util.Locale.Default = locale;
            config.SetLocale(locale);

            this.BaseContext.Resources.UpdateConfiguration(config, displayMetrics);

            if (changed)
            {
                RecreateActivies();
            }
        }

        public static string LocaleToString(Java.Util.Locale locale)
        {
            //"en" ""
            //"pt" "br"
            if (string.IsNullOrEmpty(locale.Variant))
            {
                return locale.Language;
            }
            else
            {
                return locale.Language + "-r" + locale.Variant.ToUpper();
            }
        }

        public static bool AreLocalesSame(Java.Util.Locale locale1, Java.Util.Locale locale2)
        {
            return LocaleToString(locale1) == LocaleToString(locale2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns>true if changed</returns>
        public static bool SetNetworkState(Context context)
        {
            try
            {
                ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

                if (cm == null) //null if class is not a supported system service.
                {
                    return false;
                }

                if (cm.ActiveNetworkInfo != null && cm.ActiveNetworkInfo.IsConnected)
                {
                    bool oldState = SeekerState.CurrentConnectionIsUnmetered;
                    SeekerState.CurrentConnectionIsUnmetered = IsUnmetered(context, cm);
                    MainActivity.LogDebug("SetNetworkState is metered " + !SeekerState.CurrentConnectionIsUnmetered);
                    return oldState != SeekerState.CurrentConnectionIsUnmetered;
                }
                return false;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("SetNetworkState" + e.Message + e.StackTrace);
                return false;
            }
        }

        private static bool IsUnmetered(Context context, ConnectivityManager cm)
        {

            if (!AndroidX.Core.Net.ConnectivityManagerCompat.IsActiveNetworkMetered(cm)) //api 16
            {
                return true;
            }
            else
            {
                return false;
            }

            //the below can fail if on VPN
            //var capabilities = cm.GetNetworkCapabilities(cm.ActiveNetwork);
            //cm.GetNetworkCapabilities(cm.ActiveNetwork).HasCapability(NetCapability.NotMetered);
            //AndroidX.Core.Net.ConnectivityManagerCompat.IsActiveNetworkMetered(cm);
            //bool isUnmetered = (capabilities != null && capabilities.HasCapability(NetCapability.NotMetered)) ||

        }

        public static void SetDiagnosticState(bool log_diagnostics)
        {
            if (log_diagnostics)
            {
                SeekerState.SoulseekClient.DiagnosticGenerated += SoulseekClient_DiagnosticGenerated;
                AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;
            }
            else
            {
                SeekerState.SoulseekClient.DiagnosticGenerated -= SoulseekClient_DiagnosticGenerated;
                AndroidEnvironment.UnhandledExceptionRaiser -= AndroidEnvironment_UnhandledExceptionRaiser;
            }
        }

        private static void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            //by default e.Handled == false. and this does go on to crash the process (which is good imo, I only want this for logging purposes).
            MainActivity.LogDebug(e.Exception.Message);
            MainActivity.LogDebug(e.Exception.StackTrace);
        }

        private static string CreateMessage(Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            string timestamp = e.Timestamp.ToString("[MM_dd-hh:mm:ss] ");
            string body = null;
            if (e.IncludesException)
            {
                body = e.Message + System.Environment.NewLine + e.Exception.Message + System.Environment.NewLine + e.Exception.StackTrace;
            }
            else
            {
                body = e.Message;
            }
            return timestamp + body;
        }

        public static void AppendMessageToDiagFile(string msg)
        {
            //add the timestamp..
            AppendLineToDiagFile(CreateMessage(msg));
        }

        private static string CreateMessage(string line)
        {
            string timestamp = DateTime.UtcNow.ToString("[MM_dd-hh:mm:ss] ");
            return timestamp + line;
        }


        private static bool diagnosticFilesystemErrorShown = false; //so that we only show it once.
        private static void AppendLineToDiagFile(string line)
        {
            try
            {
                if (SeekerState.DiagnosticTextFile == null)
                {
                    if (SeekerState.RootDocumentFile != null) //i.e. if api > 21 and they set it.
                    {
                        SeekerState.DiagnosticTextFile = SeekerState.RootDocumentFile.FindFile("seeker_diagnostics.txt");
                        if (SeekerState.DiagnosticTextFile == null)
                        {
                            SeekerState.DiagnosticTextFile = SeekerState.RootDocumentFile.CreateFile("text/plain", "seeker_diagnostics");
                            if (SeekerState.DiagnosticTextFile == null)
                            {
                                return;
                            }
                        }
                    }
                    else if (SeekerState.UseLegacyStorage() || !SeekerState.SaveDataDirectoryUriIsFromTree) //if api < 30 and they did not set it. OR api <= 21 and they did set it.
                    {
                        //when the directory is unset.
                        string fullPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        if (!string.IsNullOrEmpty(SeekerState.SaveDataDirectoryUri))
                        {
                            fullPath = Android.Net.Uri.Parse(SeekerState.SaveDataDirectoryUri).Path;
                        }

                        var containingDir = new Java.IO.File(fullPath);

                        var javaDiagFile = new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt");
                        DocumentFile rootDir = DocumentFile.FromFile(new Java.IO.File(fullPath + @"/" + "seeker_diagnostics.txt"));
                        //DocumentFile.FromSingleUri(SeekerState.ActiveActivityRef, Android.Net.Uri.Parse(new Java.IO.File(fullPath).ToURI().ToString()));
                        //SeekerState.DiagnosticTextFile = rootDir;//.FindFile("seeker_diagnostics.txt");
                        if (!javaDiagFile.Exists())
                        {
                            if (containingDir.CanWrite())
                            {
                                bool success = javaDiagFile.CreateNewFile();
                                if (success)
                                {
                                    SeekerState.DiagnosticTextFile = rootDir;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            SeekerState.DiagnosticTextFile = rootDir;
                        }
                    }
                    else //if api >29 and they did not set it. nothing we can do.
                    {
                        return;
                    }
                }

                if (SeekerState.DiagnosticStreamWriter == null)
                {
                    System.IO.Stream outputStream = SeekerApplication.ApplicationContext.ContentResolver.OpenOutputStream(SeekerState.DiagnosticTextFile.Uri, "wa");
                    if (outputStream == null)
                    {
                        return;
                    }
                    SeekerState.DiagnosticStreamWriter = new System.IO.StreamWriter(outputStream);
                    if (SeekerState.DiagnosticStreamWriter == null)
                    {
                        return;
                    }
                }

                SeekerState.DiagnosticStreamWriter.WriteLine(line);
                SeekerState.DiagnosticStreamWriter.Flush();
            }
            catch (Exception ex)
            {
                if (!diagnosticFilesystemErrorShown)
                {
                    MainActivity.LogFirebase("failed to write to diagnostic file " + ex.Message + line + ex.StackTrace);
                    Toast.MakeText(SeekerApplication.ApplicationContext, "Failed to write to diagnostic file.", ToastLength.Long);
                    diagnosticFilesystemErrorShown = true;
                }
            }
        }

        public static void SoulseekClient_DiagnosticGenerated(object sender, Soulseek.Diagnostics.DiagnosticEventArgs e)
        {
            AppendLineToDiagFile(CreateMessage(e));
        }

        /// <summary>
        /// This is the one we should be hooking up to.
        /// This is due to the fact that the server sends us the same user update multiple times if they are of multiple interests.
        /// i.e. if we have Added Them, we are in Chatroom A, B, and C with them, then we get 4 status updates.
        /// </summary>
        public static EventHandler<UserStatusChangedEventArgs> UserStatusChangedDeDuplicated;

        private void SoulseekClient_Disconnected(object sender, SoulseekClientDisconnectedEventArgs e)
        {
            MainActivity.LogDebug("disconnected");
            lock (OurCurrentLoginTaskSyncObject)
            {
                OurCurrentLoginTask = null;
            }
        }

        public static void AcquireTransferLocksAndResetTimer()
        {
            if (MainActivity.CpuKeepAlive_Transfer != null && !MainActivity.CpuKeepAlive_Transfer.IsHeld)
            {
                MainActivity.CpuKeepAlive_Transfer.Acquire();
            }
            if (MainActivity.WifiKeepAlive_Transfer != null && !MainActivity.WifiKeepAlive_Transfer.IsHeld)
            {
                MainActivity.WifiKeepAlive_Transfer.Acquire();
            }

            if (MainActivity.KeepAliveInactivityKillTimer != null)
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop(); //can be null
                MainActivity.KeepAliveInactivityKillTimer.Start(); //reset the timer..
            }
            else
            {
                MainActivity.KeepAliveInactivityKillTimer = new System.Timers.Timer(60 * 1000 * 10); //kill after 10 mins of no activity..
                                                                                                     //remember that this is a fallback. for when foreground service is still running but nothing is happening otherwise.
                MainActivity.KeepAliveInactivityKillTimer.Elapsed += MainActivity.KeepAliveInactivityKillTimerEllapsed;
                MainActivity.KeepAliveInactivityKillTimer.AutoReset = false;
            }
        }

        public static void ReleaseTransferLocksIfServicesComplete()
        {
            //if all transfers are done..
            if (!SeekerState.UploadKeepAliveServiceRunning && !SeekerState.DownloadKeepAliveServiceRunning)
            {
                if (MainActivity.CpuKeepAlive_Transfer != null)
                {
                    MainActivity.CpuKeepAlive_Transfer.Release();
                }
                if (MainActivity.WifiKeepAlive_Transfer != null)
                {
                    MainActivity.WifiKeepAlive_Transfer.Release();
                }
                if (MainActivity.KeepAliveInactivityKillTimer != null)
                {
                    MainActivity.KeepAliveInactivityKillTimer.Stop();
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

        public static bool TransfersDownloadsCompleteStale = false; //whether a dl completes since we have last saved transfers to disk.
        public static DateTime TransfersLastSavedTime = DateTime.MinValue; //whether a dl completes since we have last saved transfers to disk.


        public static volatile int UPLOAD_COUNT = -1; // a hack see below

        private void SoulseekClient_UploadAddedRemovedInternal(object sender, SoulseekClient.TransferAddedRemovedInternalEventArgs e)
        {
            bool abortAll = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.AbortAllWasPressedDebouncer) < 750;
            if (e.Count == 0 || abortAll)
            {
                UPLOAD_COUNT = -1;
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(uploadServiceIntent);
                SeekerState.UploadKeepAliveServiceRunning = false;
            }
            else if (!SeekerState.UploadKeepAliveServiceRunning)
            {
                UPLOAD_COUNT = e.Count;
                Intent uploadServiceIntent = new Intent(this, typeof(UploadForegroundService));
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    bool? isForeground = SeekerState.ActiveActivityRef?.IsResumed();

                    //LogDebug("IsForeground: " + isForeground + " current state: " + this.Lifecycle.CurrentState.ToString()); //REMOVE THIS!!!
                    if (isForeground ?? false)
                    {
                        this.StartService(uploadServiceIntent); //this will throw if the app is in background.
                    }
                    else
                    {
                        //only do this if we absolutely must
                        //this will throw in api 31 if the app is in background. so now it is out of the question.  no way to start foreground service if in background.
                        //this.StartForegroundService(uploadServiceIntent);
                    }
                }
                else
                {
                    //even when targetting and compiling for api 31, old devices can still do this just fine.
                    this.StartService(uploadServiceIntent); //this will throw if the app is in background.
                }
                SeekerState.UploadKeepAliveServiceRunning = true;
            }
            else if (SeekerState.UploadKeepAliveServiceRunning && e.Count != 0)
            {
                UPLOAD_COUNT = e.Count;
                //for two downloads, this notification will go up before the service is started...

                //requires run on ui thread? NOPE
                string msg = string.Empty;
                if (e.Count == 1)
                {
                    msg = string.Format(UploadForegroundService.SingularUploadRemaining, e.Count);
                }
                else
                {
                    msg = string.Format(UploadForegroundService.PluralUploadsRemaining, e.Count);
                }
                var notif = UploadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(UploadForegroundService.NOTIF_ID, notif);
            }
            //});


        }


        public static volatile int DL_COUNT = -1; // a hack see below

        //it works in the case of successfully finished, cancellation token used, etc.
        private void SoulseekClient_DownloadAddedRemovedInternal(object sender, SoulseekClient.TransferAddedRemovedInternalEventArgs e)
        {
            //even with them all going onto same thread here you will still have (ex) a 16 count coming in after a 0 count sometimes.
            //SeekerState.MainActivityRef.RunOnUiThread(()=>
            //{
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal with count:" + e.Count);
            MainActivity.LogDebug("the thread is: " + System.Threading.Thread.CurrentThread.ManagedThreadId);

            bool cancelAndClear = (DateTimeOffset.Now.ToUnixTimeMilliseconds() - SeekerState.CancelAndClearAllWasPressedDebouncer) < 750;
            MainActivity.LogDebug("SoulseekClient_DownloadAddedRemovedInternal cancel and clear:" + cancelAndClear);
            if (e.Count == 0 || cancelAndClear)
            {
                DL_COUNT = -1;
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                MainActivity.LogDebug("Stop Service");
                this.StopService(downloadServiceIntent);
                SeekerState.DownloadKeepAliveServiceRunning = false;
            }
            else if (!SeekerState.DownloadKeepAliveServiceRunning)
            {
                DL_COUNT = e.Count;
                Intent downloadServiceIntent = new Intent(this, typeof(DownloadForegroundService));
                if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    bool? isForeground = SeekerState.ActiveActivityRef?.IsResumed();

                    //LogDebug("IsForeground: " + isForeground + " current state: " + this.Lifecycle.CurrentState.ToString()); //REMOVE THIS!!!
                    if (isForeground ?? false)
                    {
                        this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                    }
                    else
                    {
                        //only do this if we absolutely must
                        //this will throw in api 31 if the app is in background. so now it is out of the question.  no way to start foreground service if in background.
                        //this.StartForegroundService(downloadServiceIntent);
                    }
                }
                else
                {
                    //even when targetting and compiling for api 31, old devices can still do this just fine.
                    this.StartService(downloadServiceIntent); //this will throw if the app is in background.
                }
                SeekerState.DownloadKeepAliveServiceRunning = true;
            }
            else if (SeekerState.DownloadKeepAliveServiceRunning && e.Count != 0)
            {
                DL_COUNT = e.Count;
                //for two downloads, this notification will go up before the service is started...

                //requires run on ui thread? NOPE
                string msg = string.Empty;
                if (e.Count == 1)
                {
                    msg = string.Format(DownloadForegroundService.SingularDownloadRemaining, e.Count);
                }
                else
                {
                    msg = string.Format(DownloadForegroundService.PluralDownloadsRemaining, e.Count);
                }
                var notif = DownloadForegroundService.CreateNotification(this, msg);
                NotificationManager manager = GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Notify(DownloadForegroundService.NOTIF_ID, notif);
            }
            //});
        }


        // TODOORG move to Utils\SpeedLimitHelper
        public static class SpeedLimitHelper
        {

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

            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>(); //we need the double precision bc sometimes 1.1 cast to int will be the same number i.e. (int)(4*1.1)==4
            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> DownloadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();

            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadUserDelays = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();
            public static System.Collections.Concurrent.ConcurrentDictionary<string, double> UploadLastAvgSpeed = new System.Collections.Concurrent.ConcurrentDictionary<string, double>();
            public static Task OurDownloadGoverner(double currentSpeed, string username, CancellationToken cts)
            {
                try
                {
                    if (SeekerState.SpeedLimitDownloadOn)
                    {

                        if (DownloadUserDelays.TryGetValue(username, out double msDelay))
                        {
                            bool exists = DownloadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                            if (exists && currentSpeed == lastAvgSpeed)
                            {
#if DEBUG
                                //System.Console.WriteLine("dont update");
#endif
                                //do not adjust as we have not yet recalculated the average speed
                                return Task.Delay((int)msDelay, cts);
                            }

                            DownloadLastAvgSpeed[username] = currentSpeed;

                            double avgSpeed = currentSpeed;
                            if (!SeekerState.SpeedLimitDownloadIsPerTransfer && DownloadLastAvgSpeed.Count > 1)
                            {

                                //its threadsafe when using linq on concurrent dict itself.
                                avgSpeed = DownloadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
#if DEBUG
                                //System.Console.WriteLine("multiple total speed " + avgSpeed);
#endif
                            }

                            if (avgSpeed > SeekerState.SpeedLimitDownloadBytesSec)
                            {
#if DEBUG
                                //System.Console.WriteLine("speed too high " + currentSpeed + "   " + msDelay);
#endif
                                DownloadUserDelays[username] = msDelay = msDelay * 1.04;

                            }
                            else
                            {
#if DEBUG
                                //System.Console.WriteLine("speed too low " + currentSpeed + "   " + msDelay);
#endif
                                DownloadUserDelays[username] = msDelay = msDelay * 0.96;
                            }

                            return Task.Delay((int)msDelay, cts);
                        }
                        else
                        {
#if DEBUG
                            //System.Console.WriteLine("first time guess");
#endif
                            //first time we need to guess a decent value
                            //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                            double msDelaySeed = 1000 * 16384.0 / SeekerState.SpeedLimitDownloadBytesSec;
                            DownloadUserDelays[username] = msDelaySeed;
                            DownloadLastAvgSpeed[username] = currentSpeed;
                            return Task.Delay((int)msDelaySeed, cts);
                        }

                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }
                catch (Exception ex)
                {
                    MainActivity.LogFirebase("DL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace);
                    return Task.CompletedTask;
                }
            }

            //this is duplicated for speed.
            public static Task OurUploadGoverner(double currentSpeed, string username, CancellationToken cts)
            {
                try
                {
                    if (SeekerState.SpeedLimitUploadOn)
                    {

                        if (UploadUserDelays.TryGetValue(username, out double msDelay))
                        {
                            bool exists = UploadLastAvgSpeed.TryGetValue(username, out double lastAvgSpeed); //this is here in the case of a race condition (due to RemoveUser)
                            if (exists && currentSpeed == lastAvgSpeed)
                            {
#if DEBUG
                                //System.Console.WriteLine("UL dont update");
#endif
                                //do not adjust as we have not yet recalculated the average speed
                                return Task.Delay((int)msDelay, cts);
                            }

                            UploadLastAvgSpeed[username] = currentSpeed;

                            double avgSpeed = currentSpeed;
                            if (!SeekerState.SpeedLimitUploadIsPerTransfer && UploadLastAvgSpeed.Count > 1)
                            {

                                //its threadsafe when using linq on concurrent dict itself.
                                avgSpeed = UploadLastAvgSpeed.Sum((p) => p.Value);//Values.ToArray().Sum();
#if DEBUG
                                //System.Console.WriteLine("UL multiple total speed " + avgSpeed);
#endif
                            }

                            if (avgSpeed > SeekerState.SpeedLimitUploadBytesSec)
                            {
#if DEBUG
                                //System.Console.WriteLine("UL speed too high " + currentSpeed + "   " + msDelay);
#endif
                                UploadUserDelays[username] = msDelay = msDelay * 1.04;

                            }
                            else
                            {
#if DEBUG
                                //System.Console.WriteLine("UL speed too low " + currentSpeed + "   " + msDelay);
#endif
                                UploadUserDelays[username] = msDelay = msDelay * 0.96;
                            }

                            return Task.Delay((int)msDelay, cts);
                        }
                        else
                        {
#if DEBUG
                            //System.Console.WriteLine("UL first time guess");
#endif
                            //first time we need to guess a decent value
                            //wait time if the loop took 0s with buffer size of 16kB i.e. speed = 16kB / (delaytime). (delaytime in ms) = 1000 * 16,384 / (speed in bytes per second).
                            double msDelaySeed = 1000 * 16384.0 / SeekerState.SpeedLimitUploadBytesSec;
                            UploadUserDelays[username] = msDelaySeed;
                            UploadLastAvgSpeed[username] = currentSpeed;
                            return Task.Delay((int)msDelaySeed, cts);
                        }

                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }
                catch (Exception ex)
                {
                    MainActivity.LogFirebase("UL SPEED LIMIT EXCEPTION: " + ex.Message + ex.StackTrace);
                    return Task.CompletedTask;
                }
            }

        }


        // TODOORG move to EventArgs?
        public class ProgressUpdatedUIEventArgs : EventArgs
        {
            public ProgressUpdatedUIEventArgs(TransferItem _ti, bool _wasFailed, bool _fullRefresh, double _percentComplete, double _avgspeedBytes)
            {
                ti = _ti;
                wasFailed = _wasFailed;
                fullRefresh = _fullRefresh;
                percentComplete = _percentComplete;
                avgspeedBytes = _avgspeedBytes;
            }
            public TransferItem ti;
            public bool wasFailed;
            public bool fullRefresh;
            public double percentComplete;
            public double avgspeedBytes;
        }

        public static EventHandler<TransferItem> StateChangedForItem;
        public static EventHandler<int> StateChangedAtIndex;
        public static EventHandler<ProgressUpdatedUIEventArgs> ProgressUpdated;

        private void SoulseekClient_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop();
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }

            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;

            if (!isUpload && e.Transfer.State.HasFlag(TransferStates.UserOffline))
            {
                //user offline.
                TransfersFragment.AddToUserOffline(e.Transfer.Username);
            }

            TransferItem relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, isUpload, out _);
            if (relevantItem == null)
            {
                MainActivity.LogInfoFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
            }
            //TransferItem relevantItem = TransfersFragment.TransferItemManagerDL.GetTransferItemWithIndexFromAll(e.Transfer?.Filename, e.Transfer?.Username, out _);  //upload / download branch here
            if (relevantItem != null)
            {
                //if the incoming transfer is not canclled, i.e. requested, then we replace the state (the user retried).
                if (e.Transfer.State.HasFlag(TransferStates.Cancelled) && relevantItem.State.HasFlag(TransferStates.FallenFromQueue))
                {
                    MainActivity.LogDebug("fallen from queue");
                    //the state is good as is.  do not add cancelled to it, since we used cancelled to mean "user cancelled" i.e. paused.
                    relevantItem.Failed = true;
                    relevantItem.Progress = 100;
                }
                else
                {
                    relevantItem.State = e.Transfer.State;
                }
                relevantItem.IncompleteParentUri = e.IncompleteParentUri;
                if (!relevantItem.State.HasFlag(TransferStates.Requested))
                {
                    relevantItem.InProcessing = true;
                }
                if (relevantItem.State.HasFlag(TransferStates.Succeeded))
                {
                    relevantItem.IncompleteParentUri = null; //not needed anymore.
                }
                //if(relevantItem.Size==-1)
                //{
                //    if(e.Transfer.Size!=0)
                //    {
                //        relevantItem.Size = e.Transfer.Size;
                //    }
                //}
            }
            if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut) || e.Transfer.State.HasFlag(TransferStates.Rejected))
            {
                SeekerApplication.SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                relevantItem.Failed = true;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Queued))
            {
                if (relevantItem == null)
                {
                    return;
                }
                if (!relevantItem.IsUpload())
                {
                    // TODO why is queue length max value
                    if (relevantItem.QueueLength != 0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
                    {
                        MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, relevantItem, null);
                    }
                    else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
                    {
                        MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, true, true, relevantItem, null);
                    }
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Initializing))
            {
                if (relevantItem == null)
                {
                    return;
                }
                //clear queued flag...
                relevantItem.QueueLength = int.MaxValue;
                StateChangedForItem?.Invoke(null, relevantItem);
            }
            else if (e.Transfer.State.HasFlag(TransferStates.Completed))
            {
                SeekerApplication.SpeedLimitHelper.RemoveDownloadUser(e.Transfer.Username);
                if (relevantItem == null)
                {
                    return;
                }
                if (!e.Transfer.State.HasFlag(TransferStates.Cancelled))
                {
                    //clear queued flag...
                    SeekerApplication.TransfersDownloadsCompleteStale = true;
                    TransfersFragment.SaveTransferItems(SeekerState.SharedPreferences, false, 120);
                    relevantItem.Progress = 100;
                    StateChangedForItem?.Invoke(null, relevantItem);
                }
                else //if it does have state cancelled we still want to update UI! (assuming we arent also clearing it)
                {
                    if (!relevantItem.CancelAndClearFlag)
                    {
                        StateChangedForItem?.Invoke(null, relevantItem);
                    }
                }

                if (e.Transfer.State.HasFlag(TransferStates.Succeeded))
                {
                    if (SeekerState.NotifyOnFolderCompleted && !isUpload)
                    {
                        if (TransfersFragment.TransferItemManagerDL.IsFolderNowComplete(relevantItem, false))
                        {
                            //relevantItem.TransferItemExtra // if single then change the notif text.
                            // RetryDL is on completed Succeeded dl?
                            ShowNotificationForCompletedFolder(relevantItem.FolderName, relevantItem.Username);
                        }
                    }
                }
            }
            else
            {
                if (relevantItem == null && (e.Transfer.State == TransferStates.Requested || e.Transfer.State == TransferStates.Aborted))
                {
                    return; //TODO sometimes this can happen too fast.  this is okay thouugh bc it will soon go to another state.
                }
                if (relevantItem == null && e.Transfer.State == TransferStates.InProgress)
                {
                    //THIS SHOULD NOT HAPPEN now that the race condition is resolved....
                    MainActivity.LogFirebase("relevantItem==null. state: " + e.Transfer.State.ToString());
                    return;
                }
                StateChangedForItem?.Invoke(null, relevantItem);
            }
        }

        public static bool OnTransferSizeMismatchFunc(System.IO.Stream fileStream, string fullFilename, string username, long startOffset, long oldSize, long newSize, string incompleteUriString, out System.IO.Stream newStream)
        {
            newStream = null;
            try
            {
                var relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(fullFilename, username, false, out _);
                if (startOffset == 0)
                {
                    // all we need to do is update the size.
                    relevantItem.Size = newSize;
                    MainActivity.LogDebug("updated the size");
                }
                else
                {
                    // we need to truncate the incomplete file and set our progress back to 0.
                    relevantItem.Size = newSize;
                    //fileStream.SetLength(0); //this is not supported. we cannot do seek.
                    //fileStream.Flush();

                    fileStream.Close();
                    bool useDownloadDir = false;
                    if (SeekerState.CreateCompleteAndIncompleteFolders && !SettingsActivity.UseIncompleteManualFolder())
                    {
                        useDownloadDir = true;
                    }

                    var incompleteUri = Android.Net.Uri.Parse(incompleteUriString);

                    // this is the only time we do legacy.
                    bool isLegacyCase = SeekerState.UseLegacyStorage() && (SeekerState.RootDocumentFile == null && useDownloadDir);
                    if (isLegacyCase)
                    {
                        newStream = new System.IO.FileStream(incompleteUri.Path, System.IO.FileMode.Truncate, System.IO.FileAccess.Write, System.IO.FileShare.None);
                    }
                    else
                    {

                        newStream = SeekerState.MainActivityRef.ContentResolver.OpenOutputStream(incompleteUri, "wt");
                    }

                    relevantItem.Progress = 0;
                    MainActivity.LogDebug("truncated the file and updated the size");
                }

            }
            catch (Exception e)
            {
                MainActivity.LogDebug("OnTransferSizeMismatchFunc: " + e.ToString());
                return false;
            }
            return true;
        }


        private void SoulseekClient_TransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        {
            //Its possible to get a nullref here IF the system orientation changes..
            //throttle this maybe...

            //MainActivity.LogDebug("TRANSFER PROGRESS UPDATED"); //this typically happens once every 10 ms or even less and thats in debug mode.  in fact sometimes it happens 4 times in 1 ms.
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop(); //lot of nullref here...
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }
            TransferItem relevantItem = null;
            if (TransfersFragment.TransferItemManagerDL == null)
            {
                MainActivity.LogDebug("transferItems Null " + e.Transfer.Filename);
                return;
            }

            bool isUpload = e.Transfer.Direction == TransferDirection.Upload;
            relevantItem = TransfersFragment.TransferItemManagerWrapped.GetTransferItemWithIndexFromAll(e.Transfer.Filename, e.Transfer.Username, e.Transfer.Direction == TransferDirection.Upload, out _);
            //relevantItem = TransfersFragment.TransferItemManagerDL.GetTransferItem(e.Transfer.Filename);

            if (relevantItem == null)
            {
                //this happens on Clear and Cancel All.
                MainActivity.LogDebug("Relevant Item Null " + e.Transfer.Filename);
                MainActivity.LogDebug("transferItems.IsEmpty " + TransfersFragment.TransferItemManagerDL.IsEmpty());
                return;
            }
            else
            {
                bool fullRefresh = false;
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;
                relevantItem.AvgSpeed = e.Transfer.AverageSpeed;

                // int indexRemoved = -1;
                if (((SeekerState.AutoClearCompleteDownloads && !isUpload) || (SeekerState.AutoClearCompleteUploads && isUpload)) && System.Math.Abs(percentComplete - 100) < .001) //if 100% complete and autoclear //todo: autoclear on upload
                {

                    Action action = new Action(() =>
                    {
                        //int before = TransfersFragment.transferItems.Count;
                        TransfersFragment.UpdateBatchSelectedItemsIfApplicable(relevantItem);
                        TransfersFragment.TransferItemManagerWrapped.Remove(relevantItem);//TODO: shouldnt we do the corresponding Adapter.NotifyRemoveAt. //this one doesnt need cleaning up, its successful..
                        //int after = TransfersFragment.transferItems.Count;
                        //MainActivity.LogDebug("transferItems.Remove(relevantItem): before: " + before + "after: " + after);
                    });
                    if (SeekerState.ActiveActivityRef != null)
                    {
                        SeekerState.ActiveActivityRef?.RunOnUiThread(action);
                    }

                    fullRefresh = true;
                }
                else if (System.Math.Abs(percentComplete - 100) < .001)
                {
                    fullRefresh = true;
                }

                bool wasFailed = false;
                if (percentComplete != 0)
                {
                    wasFailed = false;
                    if (relevantItem.Failed)
                    {
                        wasFailed = true;
                        relevantItem.Failed = false;
                    }

                }

                ProgressUpdated?.Invoke(null, new ProgressUpdatedUIEventArgs(relevantItem, wasFailed, fullRefresh, percentComplete, e.Transfer.AverageSpeed));

            }
        }

        public static string GetVersionString()
        {
            try
            {
                PackageInfo pInfo = SeekerState.ActiveActivityRef.PackageManager.GetPackageInfo(SeekerState.ActiveActivityRef.PackageName, 0);
                return pInfo.VersionName;
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("GetVersionString: " + e.Message);
                return string.Empty;
            }
        }




        public static Task<UserInfo> UserInfoResponseHandler(string uname, IPEndPoint ipEndPoint)
        {
            if (IsUserInIgnoreList(uname))
            {
                return Task.FromResult(new UserInfo(string.Empty, 0, 0, false));
            }
            string bio = SeekerState.UserInfoBio ?? string.Empty;
            byte[] picture = GetUserInfoPicture();
            int uploadSlots = 1;
            int queueLength = 0;
            bool hasFreeSlots = true;
            if (!SeekerState.SharingOn) //in my experience even if someone is sharing nothing they say 1 upload slot and yes free slots.. but idk maybe 0 and no makes more sense??
            {
                uploadSlots = 0;
                queueLength = 0;
                hasFreeSlots = false;
            }

            return Task.FromResult(new UserInfo(bio, picture, uploadSlots, queueLength, hasFreeSlots));
        }

        private static byte[] GetUserInfoPicture()
        {
            if (SeekerState.UserInfoPictureName == null || SeekerState.UserInfoPictureName == string.Empty)
            {
                return null;
            }
            Java.IO.File userInfoPicDir = new Java.IO.File(ApplicationContext.FilesDir, EditUserInfoActivity.USER_INFO_PIC_DIR);
            if (!userInfoPicDir.Exists())
            {
                MainActivity.LogFirebase("!userInfoPicDir.Exists()");
                return null;
            }

            Java.IO.File userInfoPic = new Java.IO.File(userInfoPicDir, SeekerState.UserInfoPictureName);
            if (!userInfoPic.Exists())
            {
                //I could imagine a race condition causing this...
                MainActivity.LogFirebase("!userInfoPic.Exists()");
                return null;
            }
            DocumentFile documentFile = DocumentFile.FromFile(userInfoPic);
            System.IO.Stream imageStream = ApplicationContext.ContentResolver.OpenInputStream(documentFile.Uri);
            byte[] picFile = new byte[imageStream.Length];
            imageStream.Read(picFile, 0, (int)imageStream.Length);
            return picFile;
        }

        private void SoulseekClient_PrivilegedUserListReceived(object sender, IReadOnlyCollection<string> e)
        {
            PrivilegesManager.Instance.SetPrivilegedList(e);
        }

        private void SoulseekClient_ServerInfoReceived(object sender, ServerInfo e)
        {
            if (e.WishlistInterval.HasValue)
            {
                WishlistController.SearchIntervalMilliseconds = e.WishlistInterval.Value;
                WishlistController.Initialize();
            }
            else
            {
                MainActivity.LogDebug("wishlist interval is null");
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
            MainActivity.LogDebug("Prev: " + e.PreviousState.ToString() + " Next: " + e.State.ToString());
            if (e.PreviousState.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Disconnecting))
            {
                MainActivity.LogDebug("!! changing from connected to disconnecting");


                if (e.Exception is KickedFromServerException)
                {
                    MainActivity.LogDebug("Kicked Kicked Kicked");
                    if (SeekerState.ActiveActivityRef != null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.kicked_due_to_other_client), ToastLength.Long).Show(); });
                    }
                    return; //DO NOT RETRY!!! or will do an infinite loop!
                }
                if (e.Exception is System.ObjectDisposedException)
                {
                    return; //DO NOT RETRY!!! we are shutting down
                }



                //this is a "true" connected to disconnected
                ChatroomController.ClearAndCacheJoined();
                MainActivity.LogDebug("disconnected " + DateTime.UtcNow.ToString());
                if (SeekerState.logoutClicked)
                {
                    SeekerState.logoutClicked = false;
                }
                else if (AUTO_CONNECT_ON && SeekerState.currentlyLoggedIn)
                {
                    Thread reconnectRetrier = new Thread(ReconnectSteppedBackOffThreadTask);
                    reconnectRetrier.Start();
                }
            }
            else if (e.PreviousState.HasFlag(SoulseekClientStates.Disconnected))
            {
                MainActivity.LogDebug("!! changing from disconnected to trying to connect");
            }
            else if (e.State.HasFlag(SoulseekClientStates.LoggedIn) && e.State.HasFlag(SoulseekClientStates.Connected))
            {
                MainActivity.LogDebug("!! changing trying to connect to successfully connected");
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
            MainActivity.LogDebug("logged in " + DateTime.UtcNow.ToString());
            MainActivity.LogDebug("Listening State: " + SeekerState.SoulseekClient.GetListeningState());
            if (SeekerState.ListenerEnabled && !SeekerState.SoulseekClient.GetListeningState())
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    MainActivity.LogFirebase("SeekerState.ActiveActivityRef null SoulseekClient_LoggedIn");
                }
                else
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.GetString(Resource.String.port_already_in_use), ToastLength.Short).Show(); //todo is this supposed to be here...
                    });
                }
            }
        }

        private void SoulseekClient_Connected(object sender, EventArgs e)
        {
            //ChatroomController.SetConnectionLapsedMessage(true);
            MainActivity.LogDebug("connected " + DateTime.UtcNow.ToString());

        }

        //private void SoulseekClient_Disconnected(object sender, SoulseekClientDisconnectedEventArgs e)
        //{
        //    ChatroomController.ConnectionLapse.Add(new Tuple<bool,DateTime>(false,DateTime.UtcNow));
        //    MainActivity.LogDebug("disconnected " + DateTime.UtcNow.ToString());
        //    bool AUTO_CONNECT = true;
        //    if(AUTO_CONNECT && SeekerState.currentlyLoggedIn)
        //    {
        //        Thread reconnectRetrier = new Thread(ReconnectExponentialBackOffThreadTask);
        //        reconnectRetrier.Start();
        //    }
        //}

        public static void ShowToast(string msg, ToastLength toastLength)
        {
            if (SeekerState.ActiveActivityRef == null)
            {
                MainActivity.LogDebug("cant show toast, active activity ref is null");
            }
            else
            {
                SeekerState.ActiveActivityRef.RunOnUiThread(() => { Toast.MakeText(SeekerState.ActiveActivityRef, msg, toastLength).Show(); });
            }
        }

        public static string GetString(int resId)
        {
            return SeekerApplication.ApplicationContext.GetString(resId);
        }

        public static bool ShouldWeTryToConnect()
        {
            if (!SeekerState.currentlyLoggedIn)
            {
                //we logged out on purpose
                return false;
            }

            if (SeekerState.SoulseekClient == null)
            {
                //too early
                return false;
            }

            if (SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.Connected) && SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                //already connected
                return false;
            }
            return true;
        }

        /// <summary>
        /// This is the number of seconds after the last try.
        /// </summary>
        private static readonly int[] retrySeconds = new int[MAX_TRIES] { 1, 2, 4, 10, 20 };
        private const int MAX_TRIES = 5;
        // if the reconnect stepped backoff thread is in progress but a change ocurred that makes us
        // want to trigger it immediately, then we can just set this event.
        public static AutoResetEvent ReconnectAutoResetEvent = new AutoResetEvent(false);
        public static volatile bool ReconnectSteppedBackOffThreadIsRunning = false;
        public static void ReconnectSteppedBackOffThreadTask()
        {
            try
            {
                ReconnectSteppedBackOffThreadIsRunning = true;
                for (int i = 0; i < MAX_TRIES; i++)
                {
                    if (!ShouldWeTryToConnect())
                    {
                        return; //our work here is done
                    }

                    bool isDueToAutoReset = ReconnectAutoResetEvent.WaitOne(retrySeconds[i] * 1000);
                    if (isDueToAutoReset)
                    {
                        MainActivity.LogDebug("is woken due to auto reset");
                    }
                    //System.Threading.Thread.Sleep(retrySeconds[i] * 1000); //todo AutoResetEvent or WaitOne(ms) etc.

                    try
                    {
                        //a general note for connecting:
                        //whenever you reconnect if you want the server to tell you the status of users on your user list
                        //you have to re-AddUser them.  This is what SoulSeekQt does (wireshark message code 5 for each user in list).
                        //and what Nicotine does (userlist.server_login()).
                        //and reconnecting means every single time, including toggling from wifi to data / vice versa.
                        Task t = ConnectAndPerformPostConnectTasks(SeekerState.Username, SeekerState.Password);
                        t.Wait();
                        if (t.IsCompletedSuccessfully)
                        {
                            MainActivity.LogDebug("RETRY " + i + "SUCCEEDED");
                            return; //our work here is done
                        }
                    }
                    catch (Exception)
                    {

                    }
                    //if we got here we failed.. so try again shortly...
                    MainActivity.LogDebug("RETRY " + i + "FAILED");
                }
            }
            finally
            {
                ReconnectSteppedBackOffThreadIsRunning = false;
            }
        }

        public static void AddToIgnoreListFeedback(Context c, string username)
        {
            if (SeekerApplication.AddToIgnoreList(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.added_to_ignore), username), ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_to_ignore), username), ToastLength.Short).Show();
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
                    lock (SeekerState.UserList)
                    {
                        foreach (UserListItem item in SeekerState.UserList)
                        {
                            MainActivity.LogDebug("adding user: " + item.Username);
                            SeekerState.SoulseekClient.AddUserAsync(item.Username).ContinueWith(UpdateUserInfo);
                        }
                    }

                    lock (TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                    {
                        foreach (string userDownloadOffline in TransfersFragment.UsersWhereDownloadFailedDueToOffline.Keys)
                        {
                            MainActivity.LogDebug("adding user (due to a download we wanted from them when they were offline): " + userDownloadOffline);
                            SeekerState.SoulseekClient.AddUserAsync(userDownloadOffline).ContinueWith(UpdateUserOfflineDownload);
                        }
                    }

                    //this is if we wanted to change the status earlier but could not. note that when we first login, our status is Online by default.
                    //so no need to change it to online.
                    if (SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.OnlinePending)
                    {
                        //we just did this by logging in...
                        MainActivity.LogDebug("online was pending");
                        SeekerState.PendingStatusChangeToAwayOnline = SeekerState.PendingStatusChange.NothingPending;
                    }
                    else if (((SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.AwayPending || SeekerState.OurCurrentStatusIsAway)))
                    {
                        MainActivity.LogDebug("a change to away was pending / our status is away. lets set it now");

                        if (SeekerState.PendingStatusChangeToAwayOnline == SeekerState.PendingStatusChange.AwayPending)
                        {
                            MainActivity.LogDebug("pending that is....");
                        }
                        else
                        {
                            MainActivity.LogDebug("current that is...");
                        }

                        if (ForegroundLifecycleTracker.NumberOfActiveActivities != 0)
                        {
                            MainActivity.LogDebug("There is a hole in our logic!!! the pendingstatus and/or current status should not be away!!!");
                        }
                        else
                        {
                            MainActivity.SetStatusApi(true);
                        }
                    }

                    //if the number of directories is stale (meaning it changing when we werent logged in and so we could not update the server)
                    //and we have not yet attempted to set up sharing (since after we attempt to set up sharing we will notify the server)
                    //then tell the server here.
                    //this makes it so that we tell the server once when Seeker first launches, and when things change, but not every time
                    //we log in.
                    if (SeekerState.NumberOfSharedDirectoriesIsStale && SeekerState.AttemptedToSetUpSharing)
                    {
                        MainActivity.LogDebug("stale and we already attempted to set up sharing, so lets do it here in post log in.");
                        MainActivity.InformServerOfSharedFiles();
                    }

                    TransfersController.InitializeService();
                }
                catch (Exception e)
                {
                    MainActivity.LogFirebase("PerformPostConnectTasks" + e.Message + e.StackTrace);
                }
            }
        }

        public static Android.Graphics.Drawables.Drawable? GetDrawableFromAttribute(Context c, int attr)
        {
            var typedValue = new TypedValue();
            c.Theme.ResolveAttribute(attr, typedValue, true);
            int drawableRes = (typedValue.ResourceId != 0) ? typedValue.ResourceId : typedValue.Data;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                return c.Resources.GetDrawable(drawableRes, SeekerState.ActiveActivityRef.Theme);
            }
            else
            {
                return c.Resources.GetDrawable(drawableRes);
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
                ProcessPotentialUserOfflineChangedEvent(t.Result.Username, t.Result.Status);
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
                MainActivity.LogDebug("Update User Info Received");
                if (t.IsCompletedSuccessfully)
                {
                    string username = t.Result.Username;
                    MainActivity.LogDebug("Update User Info: " + username + " status: " + t.Result.Status.ToString());
                    if (MainActivity.UserListContainsUser(username))
                    {
                        MainActivity.UserListAddUser(t.Result, t.Result.Status);
                    }


                }
                else if (t.Exception?.InnerException is UserNotFoundException)
                {
                    if (t.Exception.InnerException.Message.Contains("User ") && t.Exception.InnerException.Message.Contains("does not exist"))
                    {
                        string username = t.Exception.InnerException.Message.Split(null)[1];
                        if (MainActivity.UserListContainsUser(username))
                        {
                            MainActivity.UserListSetDoesNotExist(username);
                        }
                    }
                    else
                    {
                        MainActivity.LogFirebase("unexcepted error message - " + t.Exception.InnerException.Message);
                    }
                }
                else
                {
                    //timeout
                    MainActivity.LogFirebase("UpdateUserInfo case 3 " + t.Exception.Message);
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("UpdateUserInfo" + e.Message + e.StackTrace);
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

        public static void SetActivityTheme(Activity a)
        {
            //useless returns the same thing every time
            //int curTheme = a.PackageManager.GetActivityInfo(a.ComponentName, 0).ThemeResource;
            if (a.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
            {
                a.SetTheme(ThemeHelper.ToNightThemeProper(SeekerState.NightModeVarient));
            }
            else
            {
                a.SetTheme(ThemeHelper.ToDayThemeProper(SeekerState.DayModeVarient));
            }
        }


        /// <summary>
        /// Add To User List and save user list to shared prefs.  false if already added
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool AddToIgnoreList(string username)
        {
            //typically its tough to add a user to ignore list from the UI if they are in the User List.
            //but for example if you ignore a user based on their message.
            //User List and Ignore List and mutually exclusive so if you ignore someone, they will be removed from user list.
            if (MainActivity.UserListContainsUser(username))
            {
                MainActivity.UserListRemoveUser(username);
            }

            lock (SeekerState.IgnoreUserList)
            {
                if (SeekerState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; }))
                {
                    return false;
                }
                else
                {
                    SeekerState.IgnoreUserList.Add(new UserListItem(username, UserRole.Ignored));
                }
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_IgnoreUserList, SerializationHelper.SaveUserListToString(SeekerState.IgnoreUserList));
                editor.Commit();
            }
            return true;
        }

        public static void RemoveFromIgnoreListFeedback(Context c, string username)
        {
            if (RemoveFromIgnoreList(username))
            {
                Toast.MakeText(c, string.Format(c.GetString(Resource.String.removed_user_from_ignored_list), username), ToastLength.Short).Show();
            }
            else
            {
                //Toast.MakeText(c, string.Format(c.GetString(Resource.String.already_added_to_ignore), username), ToastLength.Short).Show();
            }
        }

        /// <summary>
        /// Remove From User List and save user list to shared prefs.  false if not found..
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool RemoveFromIgnoreList(string username)
        {
            lock (SeekerState.IgnoreUserList)
            {
                if (!SeekerState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; }))
                {
                    return false;
                }
                else
                {
                    SeekerState.IgnoreUserList = SeekerState.IgnoreUserList.Where(userListItem => { return userListItem.Username != username; }).ToList();
                }
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_IgnoreUserList, SerializationHelper.SaveUserListToString(SeekerState.IgnoreUserList));
                editor.Commit();
            }
            return true;
        }

        public static bool IsUserInIgnoreList(string username)
        {
            lock (SeekerState.IgnoreUserList)
            {
                return SeekerState.IgnoreUserList.Exists(userListItem => { return userListItem.Username == username; });
            }
        }


        // TODOORG
        public class NotifInfo
        {
            public NotifInfo(string firstDir)
            {
                NOTIF_ID_FOR_USER = NotifIdCounter;
                NotifIdCounter++;
                FilesUploadedToUser = 1;
                DirNames = new List<string>
                {
                    firstDir
                };
            }
            public int NOTIF_ID_FOR_USER;
            public int FilesUploadedToUser;
            public List<string> DirNames = new List<string>();
        }

        public static Dictionary<string, NotifInfo> NotificationUploadTracker = new Dictionary<string, NotifInfo>();
        public static int NotifIdCounter = 400;

        /// <summary>
        /// this is for global uploading event handling only.  the tabpageadapter is the one for downloading... and for upload tranferpage specific events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Upload_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            if (e.Transfer == null || e.Transfer.Direction == TransferDirection.Download)
            {
                return;
            }
            if (e.Transfer.State == TransferStates.InProgress)
            {
                MainActivity.LogDebug("transfer state changed to in progress" + e.Transfer.Filename);
                //uploading file to user...
            }
            //if(e.Transfer.State == TransferStates.Completed) //this condition will NEVER be hit.  it is always completed | succeeded
            if (e.Transfer.State.HasFlag(TransferStates.Succeeded)) //todo rethink upload notifications....
            {
                MainActivity.LogDebug("transfer state changed to completed" + e.Transfer.Filename);
                //send notif successfully uploading file to user..
                //e.Transfer.AverageSpeed - speed in bytes/second
                if (e.Transfer.AverageSpeed <= 0 || ((int)(e.Transfer.AverageSpeed)) == 0)
                {
                    MainActivity.LogDebug("avg speed <= 0" + e.Transfer.Filename);
                    return;
                }
                MainActivity.LogDebug("sending avg speed of " + e.Transfer.AverageSpeed.ToString());
                SeekerState.SoulseekClient.SendUploadSpeedAsync((int)(e.Transfer.AverageSpeed));
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.MainActivityRef, MainActivity.UPLOADS_CHANNEL_ID, MainActivity.UPLOADS_CHANNEL_NAME, NotificationImportance.High);
                    NotifInfo notifInfo = null;
                    string directory = Common.Helpers.GetFolderNameFromFile(e.Transfer.Filename.Replace("/", @"\"));
                    if (NotificationUploadTracker.ContainsKey(e.Transfer.Username))
                    {
                        notifInfo = NotificationUploadTracker[e.Transfer.Username];
                        if (!notifInfo.DirNames.Contains(directory))
                        {
                            notifInfo.DirNames.Add(directory);
                        }
                        notifInfo.FilesUploadedToUser++;
                    }
                    else
                    {
                        notifInfo = new NotifInfo(directory);
                        NotificationUploadTracker.Add(e.Transfer.Username, notifInfo);
                    }

                    Notification n = MainActivity.CreateUploadNotification(SeekerState.MainActivityRef, e.Transfer.Username, notifInfo.DirNames, notifInfo.FilesUploadedToUser);
                    NotificationManagerCompat nmc = NotificationManagerCompat.From(SeekerState.MainActivityRef);
                    nmc.Notify(e.Transfer.Username.GetHashCode(), n);
                }
                catch (Exception err)
                {
                    MainActivity.LogFirebase("Upload Noficiation Failed" + err.Message + err.StackTrace);
                }
            }
        }



        /// <summary>
        /// this is for getting additional information (status updates) from already added users 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="userData"></param>
        /// <param name="userStatus"></param>
        private static bool UserListAddIfContainsUser(string username, UserData userData, UserStatus userStatus)
        {
            UserPresence? prevStatus = UserPresence.Offline;
            bool found = false;
            lock (SeekerState.UserList)
            {

                foreach (UserListItem item in SeekerState.UserList)
                {
                    if (item.Username == username)
                    {
                        found = true;
                        if (userData != null)
                        {
                            item.UserData = userData;
                        }
                        if (userStatus != null)
                        {
                            prevStatus = item.UserStatus?.Presence ?? UserPresence.Offline;
                            item.UserStatus = userStatus;
                        }
                        break;
                    }
                }
            }
            //if user was previously offline and now they are not offline, then do the notification.
            //note - this method does not get called when first adding users. which I think is ideal for notifications.
            //if not in our user list, then this is likely a result of GetUserInfo!, so dont do any of this..
            if (found && (!prevStatus.HasValue || prevStatus.Value == UserPresence.Offline && (userStatus != null && userStatus.Presence != UserPresence.Offline)))
            {
                MainActivity.LogDebug("from offline to online " + username);
                if (SeekerState.UserOnlineAlerts != null && SeekerState.UserOnlineAlerts.ContainsKey(username))
                {
                    //show notification.
                    ShowNotificationForUserOnlineAlert(username);
                }
            }
            else
            {
                MainActivity.LogDebug("NOT from offline to online (or not in user list)" + username);
            }
            return found;
        }

        public static View GetViewForSnackbar()
        {
            bool useDownloadDialogFragment = false;
            View v = null;
            if (SeekerState.ActiveActivityRef is MainActivity mar)
            {
                var f = mar.SupportFragmentManager.FindFragmentByTag("tag_download_test");
                //this is the only one we have..  tho obv a more generic way would be to see if s/t is a dialog fragmnet.  but arent a lot of just simple alert dialogs etc dialog fragment?? maybe explicitly checking is the best way.
                if (f != null && f.IsVisible)
                {
                    useDownloadDialogFragment = true;
                    v = f.View;
                }
            }
            if (!useDownloadDialogFragment)
            {
                v = SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            }
            return v;
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
                    MainActivity.LogFirebase("ShowNotificationForUserOnlineAlert failed: " + e.Message + e.StackTrace);
                }
            });
        }


        public const string CHANNEL_ID_FOLDER_ALERT = "Folder Finished Downloading Alerts ID";
        public const string CHANNEL_NAME_FOLDER_ALERT = "Folder Finished Downloading Alerts";
        public const string FromFolderAlert = "FromFolderAlert";
        public const string FromFolderAlertUsername = "FromFolderAlertUsername";
        public const string FromFolderAlertFoldername = "FromFolderAlertFoldername";
        public static void ShowNotificationForCompletedFolder(string foldername, string username)
        {
            SeekerState.ActiveActivityRef.RunOnUiThread(() =>
            {
                try
                {
                    CommonHelpers.CreateNotificationChannel(SeekerState.ActiveActivityRef, CHANNEL_ID_FOLDER_ALERT, CHANNEL_NAME_FOLDER_ALERT, NotificationImportance.High); //only high will "peek"
                    Intent notifIntent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    notifIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront); //otherwise if another activity is in front then this intent will do nothing...
                    notifIntent.PutExtra(FromFolderAlert, 2);
                    notifIntent.PutExtra(FromFolderAlertUsername, username);
                    notifIntent.PutExtra(FromFolderAlertFoldername, foldername);
                    PendingIntent pendingIntent =
                        PendingIntent.GetActivity(SeekerState.ActiveActivityRef, (foldername + username).GetHashCode(), notifIntent, CommonHelpers.AppendMutabilityIfApplicable(PendingIntentFlags.UpdateCurrent, true));
                    Notification n = CommonHelpers.CreateNotification(SeekerState.ActiveActivityRef, pendingIntent, CHANNEL_ID_FOLDER_ALERT, SeekerApplication.GetString(Resource.String.FolderFinishedDownloading), string.Format(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.folder_X_from_user_Y_finished), foldername, username), false);
                    NotificationManagerCompat notificationManager = NotificationManagerCompat.From(SeekerState.ActiveActivityRef);
                    // notificationId is a unique int for each notification that you must define
                    notificationManager.Notify((foldername + username).GetHashCode(), n);
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("ShowNotificationForCompletedFolder failed: " + e.Message + e.StackTrace);
                }
            });
        }

        public static void RestoreSeekerState(ISharedPreferences sharedPreferences, Context c) //the Bundle can be SLOWER than the SHARED PREFERENCES if SHARED PREFERENCES was saved in a different activity.  The best exapmle being DAYNIGHTMODE
        {   //day night mode sets the static, saves to shared preferences the new value, sets appcompat value, which recreates everything and calls restoreSeekerState(bundle) where the bundle was older than shared prefs
            //because saveSeekerState was not called in the meantime...
            if (sharedPreferences != null)
            {
                SeekerState.currentlyLoggedIn = sharedPreferences.GetBoolean(KeyConsts.M_CurrentlyLoggedIn, false);
                SeekerState.Username = sharedPreferences.GetString(KeyConsts.M_Username, "");
                SeekerState.Password = sharedPreferences.GetString(KeyConsts.M_Password, "");
                SeekerState.SaveDataDirectoryUri = sharedPreferences.GetString(KeyConsts.M_SaveDataDirectoryUri, "");
                SeekerState.SaveDataDirectoryUriIsFromTree = sharedPreferences.GetBoolean(KeyConsts.M_SaveDataDirectoryUriIsFromTree, true);
                SeekerState.NumberSearchResults = sharedPreferences.GetInt(KeyConsts.M_NumberSearchResults, MainActivity.DEFAULT_SEARCH_RESULTS);
                SeekerState.DayNightMode = sharedPreferences.GetInt(KeyConsts.M_DayNightMode, (int)AppCompatDelegate.ModeNightFollowSystem);
                SeekerState.Language = sharedPreferences.GetString(KeyConsts.M_Lanuage, SeekerState.FieldLangAuto);
                SeekerState.LegacyLanguageMigrated = sharedPreferences.GetBoolean(KeyConsts.M_LegacyLanguageMigrated, false);
                SeekerState.NightModeVarient = (ThemeHelper.NightThemeType)(sharedPreferences.GetInt(KeyConsts.M_NightVarient, (int)ThemeHelper.NightThemeType.ClassicPurple));
                SeekerState.DayModeVarient = (ThemeHelper.DayThemeType)(sharedPreferences.GetInt(KeyConsts.M_DayVarient, (int)ThemeHelper.DayThemeType.ClassicPurple));
                SeekerState.AutoClearCompleteDownloads = sharedPreferences.GetBoolean(KeyConsts.M_AutoClearComplete, false);
                SeekerState.AutoClearCompleteUploads = sharedPreferences.GetBoolean(KeyConsts.M_AutoClearCompleteUploads, false);
                SeekerState.RememberSearchHistory = sharedPreferences.GetBoolean(KeyConsts.M_RememberSearchHistory, true);
                SeekerState.ShowRecentUsers = sharedPreferences.GetBoolean(KeyConsts.M_RememberUserHistory, true);
                SeekerState.FreeUploadSlotsOnly = sharedPreferences.GetBoolean(KeyConsts.M_OnlyFreeUploadSlots, true);
                SeekerState.HideLockedResultsInBrowse = sharedPreferences.GetBoolean(KeyConsts.M_HideLockedBrowse, true);
                SeekerState.HideLockedResultsInSearch = sharedPreferences.GetBoolean(KeyConsts.M_HideLockedSearch, true);

                SeekerState.TransferViewShowSizes = sharedPreferences.GetBoolean(KeyConsts.M_TransfersShowSizes, true);
                SeekerState.TransferViewShowSpeed = sharedPreferences.GetBoolean(KeyConsts.M_TransfersShowSpeed, true);

                SeekerState.SpeedLimitUploadOn = sharedPreferences.GetBoolean(KeyConsts.M_UploadLimitEnabled, false);
                SeekerState.SpeedLimitDownloadOn = sharedPreferences.GetBoolean(KeyConsts.M_DownloadLimitEnabled, false);
                SeekerState.SpeedLimitUploadIsPerTransfer = sharedPreferences.GetBoolean(KeyConsts.M_UploadPerTransfer, true);
                SeekerState.SpeedLimitDownloadIsPerTransfer = sharedPreferences.GetBoolean(KeyConsts.M_DownloadPerTransfer, true);
                SeekerState.SpeedLimitUploadBytesSec = sharedPreferences.GetInt(KeyConsts.M_UploadSpeedLimitBytes, 4 * 1024 * 1024);
                SeekerState.SpeedLimitDownloadBytesSec = sharedPreferences.GetInt(KeyConsts.M_DownloadSpeedLimitBytes, 4 * 1024 * 1024);

                SeekerState.DisableDownloadToastNotification = sharedPreferences.GetBoolean(KeyConsts.M_DisableToastNotifications, true);
                SeekerState.MemoryBackedDownload = sharedPreferences.GetBoolean(KeyConsts.M_MemoryBackedDownload, false);
                SearchFragment.FilterSticky = sharedPreferences.GetBoolean(KeyConsts.M_FilterSticky, false);
                SearchFragment.FilterStickyString = sharedPreferences.GetString(KeyConsts.M_FilterStickyString, string.Empty);
                SearchFragment.SetSearchResultStyle(sharedPreferences.GetInt(KeyConsts.M_SearchResultStyle, 1));
                SeekerState.UploadSpeed = sharedPreferences.GetInt(KeyConsts.M_UploadSpeed, -1);



                //SerializationHelper.MigrateUploadDirectoryInfoIfApplicable(sharedPreferences, KeyConsts.M_SharedDirectoryInfo_Legacy, KeyConsts.M_SharedDirectoryInfo);
                UploadDirectoryManager.RestoreFromSavedState(sharedPreferences);

                SeekerState.SharingOn = sharedPreferences.GetBoolean(KeyConsts.M_SharingOn, false);
                //SerializationHelper.MigrateUserListIfApplicable(sharedPreferences, KeyConsts.M_UserList_Legacy, KeyConsts.M_UserList);
                SeekerState.UserList = SerializationHelper.RestoreUserListFromString(sharedPreferences.GetString(KeyConsts.M_UserList, string.Empty));

                RestoreRecentUsersManagerFromString(sharedPreferences.GetString(KeyConsts.M_RecentUsersList, string.Empty));
                //SerializationHelper.MigrateUserListIfApplicable(sharedPreferences, KeyConsts.M_IgnoreUserList_Legacy, KeyConsts.M_IgnoreUserList);
                SeekerState.IgnoreUserList = SerializationHelper.RestoreUserListFromString(sharedPreferences.GetString(KeyConsts.M_IgnoreUserList, string.Empty));
                SeekerState.AllowPrivateRoomInvitations = sharedPreferences.GetBoolean(KeyConsts.M_AllowPrivateRooomInvitations, false);
                SeekerState.StartServiceOnStartup = sharedPreferences.GetBoolean(KeyConsts.M_ServiceOnStartup, true);
                SeekerState.NoSubfolderForSingle = sharedPreferences.GetBoolean(KeyConsts.M_NoSubfolderForSingle, false);

                SeekerState.ShowSmartFilters = sharedPreferences.GetBoolean(KeyConsts.M_ShowSmartFilters, false);
                RestoreSmartFilterState(sharedPreferences);

                SeekerState.UserInfoBio = sharedPreferences.GetString(KeyConsts.M_UserInfoBio, string.Empty);
                SeekerState.UserInfoPictureName = sharedPreferences.GetString(KeyConsts.M_UserInfoPicture, string.Empty);

                //SerializationHelper.MigrateUserNotesIfApplicable(sharedPreferences, KeyConsts.M_UserNotes_Legacy, KeyConsts.M_UserNotes);
                SeekerState.UserNotes = SerializationHelper.RestoreUserNotesFromString(sharedPreferences.GetString(KeyConsts.M_UserNotes, string.Empty));
                //SerializationHelper.MigrateOnlineAlertsIfApplicable(sharedPreferences, KeyConsts.M_UserOnlineAlerts_Legacy, KeyConsts.M_UserOnlineAlerts);
                SeekerState.UserOnlineAlerts = SerializationHelper.RestoreUserOnlineAlertsFromString(sharedPreferences.GetString(KeyConsts.M_UserOnlineAlerts, string.Empty));

                SeekerState.AutoAwayOnInactivity = sharedPreferences.GetBoolean(KeyConsts.M_AutoSetAwayOnInactivity, false);
                SeekerState.AutoRetryBackOnline = sharedPreferences.GetBoolean(KeyConsts.M_AutoRetryBackOnline, true);

                SeekerState.NotifyOnFolderCompleted = sharedPreferences.GetBoolean(KeyConsts.M_NotifyFolderComplete, true);
                SeekerState.AllowUploadsOnMetered = sharedPreferences.GetBoolean(KeyConsts.M_AllowUploadsOnMetered, true);

                UserListActivity.UserListSortOrder = (UserListActivity.SortOrder)(sharedPreferences.GetInt(KeyConsts.M_UserListSortOrder, 0));
                SeekerState.DefaultSearchResultSortAlgorithm = (SearchResultSorting)(sharedPreferences.GetInt(KeyConsts.M_DefaultSearchResultSortAlgorithm, 0));

                SimultaneousDownloadsGatekeeper.Initialize(sharedPreferences.GetBoolean(KeyConsts.M_LimitSimultaneousDownloads, false), sharedPreferences.GetInt(KeyConsts.M_MaxSimultaneousLimit, 1));

                //SearchTabHelper.RestoreStateFromSharedPreferencesLegacy();

                //SearchTabHelper.SaveHeadersToSharedPrefs();
                //SearchTabHelper.SaveAllSearchTabsToDisk(c);
                //SearchTabHelper.ConvertLegacyWishlistsIfApplicable(c);
                //SerializationHelper.MigrateHeaderState(sharedPreferences, KeyConsts.M_SearchTabsState_Headers_Legacy, KeyConsts.M_SearchTabsState_Headers);
                SearchTabHelper.RestoreHeadersFromSharedPreferences();
                //SerializationHelper.MigrateWishlistTabs(c);

                //SearchTabHelper.RestoreAllSearchTabsFromDisk(c);

                SettingsActivity.RestoreAdditionalDirectorySettingsFromSharedPreferences();

                ChatroomActivity.ShowStatusesView = sharedPreferences.GetBoolean(KeyConsts.M_ShowStatusesView, true);
                ChatroomActivity.ShowTickerView = sharedPreferences.GetBoolean(KeyConsts.M_ShowTickerView, false);
                ChatroomController.SortChatroomUsersBy = (ChatroomController.SortOrderChatroomUsers)(sharedPreferences.GetInt(KeyConsts.M_RoomUserListSortOrder, 2)); //default is 2 = alphabetical..
                ChatroomController.PutFriendsOnTop = sharedPreferences.GetBoolean(KeyConsts.M_RoomUserListShowFriendsAtTop, false);

                SeekerApplication.LOG_DIAGNOSTICS = sharedPreferences.GetBoolean(KeyConsts.M_LOG_DIAGNOSTICS, false);


                if (TransfersFragment.TransferItemManagerDL == null)
                {
                    TransfersFragment.RestoreDownloadTransferItems(sharedPreferences);
                    TransfersFragment.RestoreUploadTransferItems(sharedPreferences);
                    TransfersFragment.TransferItemManagerWrapped = new TransferItemManagerWrapper(TransfersFragment.TransferItemManagerUploads, TransfersFragment.TransferItemManagerDL);
                }
            }
        }

        public static void RestoreListeningState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                SeekerState.ListenerEnabled = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_ListenerEnabled, true);
                SeekerState.ListenerPort = SeekerState.SharedPreferences.GetInt(KeyConsts.M_ListenerPort, 33939);
                SeekerState.ListenerUPnpEnabled = SeekerState.SharedPreferences.GetBoolean(KeyConsts.M_ListenerUPnpEnabled, true);
            }
        }

        public static void SaveListeningState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_ListenerEnabled, SeekerState.ListenerEnabled);
                editor.PutInt(KeyConsts.M_ListenerPort, SeekerState.ListenerPort);
                editor.PutBoolean(KeyConsts.M_ListenerUPnpEnabled, SeekerState.ListenerUPnpEnabled);
                editor.Commit();
            }
        }

        public static void SaveSpeedLimitState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_DownloadLimitEnabled, SeekerState.SpeedLimitDownloadOn);
                editor.PutBoolean(KeyConsts.M_DownloadPerTransfer, SeekerState.SpeedLimitDownloadIsPerTransfer);
                editor.PutInt(KeyConsts.M_DownloadSpeedLimitBytes, SeekerState.SpeedLimitDownloadBytesSec);

                editor.PutBoolean(KeyConsts.M_UploadLimitEnabled, SeekerState.SpeedLimitUploadOn);
                editor.PutBoolean(KeyConsts.M_UploadPerTransfer, SeekerState.SpeedLimitUploadIsPerTransfer);
                editor.PutInt(KeyConsts.M_UploadSpeedLimitBytes, SeekerState.SpeedLimitUploadBytesSec);

                editor.Commit();
            }
        }



        public static void SetupRecentUserAutoCompleteTextView(AutoCompleteTextView actv, bool forAddingUser = false)
        {
            if (SeekerState.ShowRecentUsers)
            {
                if (forAddingUser)
                {
                    //dont show people that we have already added...
                    var recents = SeekerState.RecentUsersManager.GetRecentUserList();
                    lock (SeekerState.UserList)
                    {
                        foreach (var uli in SeekerState.UserList)
                        {
                            recents.Remove(uli.Username);
                        }
                    }
                    actv.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Resource.Layout.autoSuggestionRow, recents);
                }
                else
                {
                    actv.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Resource.Layout.autoSuggestionRow, SeekerState.RecentUsersManager.GetRecentUserList());
                }
            }
        }

        public static void RestoreSmartFilterState(ISharedPreferences sharedPreferences)
        {
            SeekerState.SmartFilterOptions = new SeekerState.SmartFilterState();
            SeekerState.SmartFilterOptions.KeywordsEnabled = sharedPreferences.GetBoolean(KeyConsts.M_SmartFilter_KeywordsEnabled, true);
            SeekerState.SmartFilterOptions.KeywordsOrder = sharedPreferences.GetInt(KeyConsts.M_SmartFilter_KeywordsOrder, 0);
            SeekerState.SmartFilterOptions.FileTypesEnabled = sharedPreferences.GetBoolean(KeyConsts.M_SmartFilter_TypesEnabled, true);
            SeekerState.SmartFilterOptions.FileTypesOrder = sharedPreferences.GetInt(KeyConsts.M_SmartFilter_TypesOrder, 1);
            SeekerState.SmartFilterOptions.NumFilesEnabled = sharedPreferences.GetBoolean(KeyConsts.M_SmartFilter_CountsEnabled, true);
            SeekerState.SmartFilterOptions.NumFilesOrder = sharedPreferences.GetInt(KeyConsts.M_SmartFilter_CountsOrder, 2);
        }

        public static void SaveSmartFilterState()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutBoolean(KeyConsts.M_SmartFilter_KeywordsEnabled, SeekerState.SmartFilterOptions.KeywordsEnabled);
                editor.PutBoolean(KeyConsts.M_SmartFilter_TypesEnabled, SeekerState.SmartFilterOptions.FileTypesEnabled);
                editor.PutBoolean(KeyConsts.M_SmartFilter_CountsEnabled, SeekerState.SmartFilterOptions.NumFilesEnabled);
                editor.PutInt(KeyConsts.M_SmartFilter_KeywordsOrder, SeekerState.SmartFilterOptions.KeywordsOrder);
                editor.PutInt(KeyConsts.M_SmartFilter_TypesOrder, SeekerState.SmartFilterOptions.FileTypesOrder);
                editor.PutInt(KeyConsts.M_SmartFilter_CountsOrder, SeekerState.SmartFilterOptions.NumFilesOrder);
                editor.Commit();
            }
        }

        public static void RestoreRecentUsersManagerFromString(string xmlRecentUsersList)
        {
            //if empty then this is the first time creating it.  initialize it with our list of added users.
            SeekerState.RecentUsersManager = new RecentUserManager();
            if (xmlRecentUsersList == string.Empty)
            {
                int count = SeekerState.UserList?.Count ?? 0;
                if (count > 0)
                {
                    SeekerState.RecentUsersManager.SetRecentUserList(SeekerState.UserList.Select(uli => uli.Username).ToList());
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
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_RecentUsersList, recentUsersStr);
                editor.Commit();
            }
        }




        /// <summary>
        /// This is from the server after sending it a UserData request.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoulseekClient_UserDataReceived(object sender, UserData e)
        {
            MainActivity.LogDebug("User Data Received: " + e.Username);
            if (e.Username == SeekerState.Username)
            {
                SeekerState.UploadSpeed = e.AverageSpeed; //bytes
            }
            else
            {
                if (SeekerState.UserList == null)
                {
                    MainActivity.LogFirebase("UserList is null on user data receive");
                }
                else
                {
                    UserListAddIfContainsUser(e.Username, e, null);
                }

                RequestedUserInfoHelper.AddIfRequestedUser(e.Username, e, null, null);
            }
        }

        private static string DeduplicateUsername = null;
        private static Soulseek.UserPresence DeduplicateStatus = Soulseek.UserPresence.Offline;
        private void SoulseekClient_UserStatusChanged_Deduplicator(object sender, UserStatusChangedEventArgs e)
        {

            if (DeduplicateUsername == e.Username && DeduplicateStatus == e.Status)
            {
                MainActivity.LogDebug($"throwing away {e.Username} status changed");
                return;
            }
            else
            {
                MainActivity.LogDebug($"handling {e.Username} status changed");
                DeduplicateUsername = e.Username;
                DeduplicateStatus = e.Status;
                SeekerApplication.UserStatusChangedDeDuplicated?.Invoke(sender, e);
            }
        }

        public static void ProcessPotentialUserOfflineChangedEvent(string username, UserPresence status)
        {
            if (status != UserPresence.Offline)
            {
                if (SeekerState.AutoRetryBackOnline)
                {
                    if (TransfersFragment.UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
                    {
                        MainActivity.LogDebug("the user came back who we previously dl from " + username);
                        //retry all failed downloads from them..
                        List<TransferItem> items = TransfersFragment.TransferItemManagerDL.GetTransferItemsFromUser(username, true, true);
                        if (items.Count == 0)
                        {
                            //no offline, then remove this user.
                            lock (TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                            {
                                TransfersFragment.UsersWhereDownloadFailedDueToOffline.Remove(username);
                            }
                        }
                        else
                        {
                            try
                            {
                                TransfersFragment.DownloadRetryAllConditionLogic(false, false, null, true, items);
                            }
                            catch (Exception e)
                            {
                                MainActivity.LogDebug("ProcessPotentialUserOfflineChangedEvent" + e.Message);
                            }
                        }

                    }
                }
            }
        }


        public static EventHandler<string> UserStatusChangedUIEvent;
        private void SoulseekClient_UserStatusChanged(object sender, UserStatusChangedEventArgs e)
        {
            if (e.Username == SeekerState.Username)
            {
                //not sure this will ever happen
            }
            else
            {
                //we get user status changed for those we are in the same room as us
                if (SeekerState.UserList != null)
                {
                    bool found = UserListAddIfContainsUser(e.Username, null, new UserStatus(e.Status, e.IsPrivileged));
                    if (found)
                    {
                        MainActivity.LogDebug("friend status changed " + e.Username);
                        SeekerApplication.UserStatusChangedUIEvent?.Invoke(null, e.Username);
                    }
                }

                ProcessPotentialUserOfflineChangedEvent(e.Username, e.Status);
            }

        }
    }
}