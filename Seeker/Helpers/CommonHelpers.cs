using Seeker.Helpers;
using Seeker.Managers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using Soulseek;
using System;
using System.Threading.Tasks;
using AndroidX.Core.Util;

using Common;
namespace Seeker
{
    public static class CommonHelpers
    {
        public static bool CompleteIncompleteDifferentVolume()
        {
            if (SettingsActivity.UseIncompleteManualFolder() && SeekerState.RootIncompleteDocumentFile != null && SeekerState.RootDocumentFile != null)
            {
                //if(!SeekerState.UseLegacyStorage())
                //{
                //    //this method is only for API29+
                //    //var sm = (SeekerState.ActiveActivityRef.GetSystemService(Context.StorageService) as Android.OS.Storage.StorageManager);
                //    //Android.OS.Storage.StorageVolume sv1 = sm.GetStorageVolume(SeekerState.RootDocumentFile.Uri); //fails if not media store uri
                //    //string uuid1 = sv1.Uuid;
                //    //Android.OS.Storage.StorageVolume sv2 = sm.GetStorageVolume(SeekerState.RootIncompleteDocumentFile.Uri);
                //    //string uuid2 = sv2.Uuid;


                //    string volume1 = FileFilterHelper.GetVolumeName(SeekerState.RootDocumentFile.Uri.LastPathSegment, out _);
                //    string volume2 = FileFilterHelper.GetVolumeName(SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment, out _);

                //    return uuid1 != uuid2;
                //}
                //else
                //{
                try
                {
                    string volume1 = FileFilterHelper.GetVolumeName(SeekerState.RootDocumentFile.Uri.LastPathSegment, false, out bool everything);
                    if (everything)
                    {
                        volume1 = SeekerState.RootDocumentFile.Uri.LastPathSegment;
                    }
                    string volume2 = FileFilterHelper.GetVolumeName(SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment, false, out everything);
                    if (everything)
                    {
                        volume2 = SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment;
                    }
                    return volume1 != volume2;
                }
                catch (Exception e)
                {
                    Logger.Firebase("CompleteIncompleteDifferentVolume failed: " + e.Message + SeekerState.RootDocumentFile?.Uri?.LastPathSegment + " incomplete: " + SeekerState.RootIncompleteDocumentFile?.Uri?.LastPathSegment);
                    return false;
                }
                //}
            }
            else
            {
                return false;
            }
        }


        public static string GetNiceDateTime(DateTime dt)
        {
            System.Globalization.CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception e)
            {
                Logger.Firebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == SimpleHelpers.GetDateTimeNowSafe().Date)
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.today) + " " + dt.ToString("h:mm:ss tt", cultureInfo); //cultureInfo can be null without issue..
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        public static PendingIntentFlags AppendMutabilityIfApplicable(PendingIntentFlags existingFlags, bool immutable)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                if (immutable)
                {
                    return existingFlags | PendingIntentFlags.Immutable;
                }
                else
                {
                    return existingFlags | PendingIntentFlags.Mutable;
                }
            }
            else
            {
                //immutable flag was only introduced in 23 so if less than that we always need to OR with mutable (or we can just leave it alone). (remember mutable is the default)
                return existingFlags;
            }
        }

        public static string GetNiceDateTimeGroupChat(DateTime dt)
        {
            System.Globalization.CultureInfo cultureInfo = null;
            try
            {
                cultureInfo = System.Globalization.CultureInfo.CurrentCulture;
            }
            catch (Exception e)
            {
                Logger.Firebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == SimpleHelpers.GetDateTimeNowSafe().Date)
            {
                return dt.ToString("h:mm:ss tt", cultureInfo); //this is the only difference...
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        public static void CopyTextToClipboard(Activity a, string txt)
        {
            var clipboardManager = a.GetSystemService(Context.ClipboardService) as ClipboardManager;
            ClipData clip = ClipData.NewPlainText("simple text", txt);
            clipboardManager.PrimaryClip = clip;
        }

        //this is a helper for this issue:
        //var name1 = df.CreateFile("audio/m4a", "name1").Name;
        //var name2 = df.CreateFile("audio/x-m4a", "name2").Name;
        //  are both extensionless....
        public static DocumentFile CreateMediaFile(DocumentFile parent, string name)
        {
            if (CommonHelpers.GetMimeTypeFromFilename(name) == M4A_MIME)
            {
                return parent.CreateFile(CommonHelpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .m4a extension for us..
            }
            else if (CommonHelpers.GetMimeTypeFromFilename(name) == APE_MIME)
            {
                return parent.CreateFile(CommonHelpers.GetMimeTypeFromFilename(name), name); //we use just name since it will not add the .ape extension for us..
            }
            else if (CommonHelpers.GetMimeTypeFromFilename(name) == null)
            {
                //a null mimetype is fine, it just defaults to application/octet-stream
                return parent.CreateFile(null, name); //we use just name since it will not add the extension for us..
            }
            else
            {
                return parent.CreateFile(CommonHelpers.GetMimeTypeFromFilename(name), System.IO.Path.GetFileNameWithoutExtension(name));
            }
        }



        //examples..
        //Helpers.GetMimeTypeFromFilename("x.flac");//"audio/flac"
        //Helpers.GetMimeTypeFromFilename("x.mp3"); //"audio/mpeg"
        //Helpers.GetMimeTypeFromFilename("x.wmv"); //"video/x-ms-wmv"
        //Helpers.GetMimeTypeFromFilename("x.wma"); // good
        //Helpers.GetMimeTypeFromFilename("x.png"); //"image/png"
        //THIS FAILS MISERABLY FOR M4A FILES. it regards them as mp3, causing both android and windows foobar to deem them corrupted and refuse to play them!
        //[seeker] .wma === audio/x-ms-wma
        //[seeker] .flac === audio/flac
        //[seeker] .aac === audio/aac
        //[seeker] .m4a === audio/mpeg  --- miserable failure should be audio/m4a or audio/x-m4a
        //[seeker] .mp3 === audio/mpeg
        //[seeker] .oga === audio/ogg
        //[seeker] .ogg === audio/ogg
        //[seeker] .opus === audio/ogg
        //[seeker] .wav === audio/x-wav
        //[seeker] .mp4 === video/mp4

        //other problematic -
        //        ".alac", -> null
        //        ".ape",  -> null  // audio/x-ape
        //        ".m4p" //aac with apple drm. similar to the drm free m4a. audio/m4p not mp4 which is reported. I am not sure...
        public const string M4A_MIME = "audio/m4a";
        public const string APE_MIME = "audio/x-ape";
        public static string GetMimeTypeFromFilename(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename).ToLower();
            string mimeType = @"audio/mpeg"; //default
            if (ext != null && ext != string.Empty)
            {
                switch (ext)
                {
                    case ".ape":
                        mimeType = APE_MIME;
                        break;
                    case ".m4a":
                        mimeType = M4A_MIME;
                        break;
                    default:
                        ext = ext.TrimStart('.');
                        mimeType = Android.Webkit.MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext);
                        break;
                }

            }
            return mimeType;
        }

        public static void ViewUri(Android.Net.Uri httpUri, Context c)
        {
            try
            {
                Intent intent = new Intent(Intent.ActionView, httpUri);
                c.StartActivity(intent);
            }
            catch (Exception e)
            {
                if (e.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                {
                    Logger.Firebase("viewUri: " + e.Message + httpUri.ToString());
                    SeekerApplication.Toaster.ShowToast(string.Format("No application found to handle url \"{0}\".  Please install or enable web browser.", httpUri.ToString()), ToastLength.Long);
                }
            }
        }

        public static string GetLastPathSegmentWithSpecialCaseProtection(DocumentFile dir, out bool msdCase)
        {
            msdCase = false;
            if (dir.Uri.LastPathSegment == "downloads")
            {
                var dfs = dir.ListFiles();
                if (dfs.Length > 0)
                {
                    //if last path segment is downloads then its likely that this is the "com.android.providers.downloads.documents" authority rather than the "com.android.externalstorage.documents" authority
                    //on android 10 (reproducible on emulator), the providers.downloads.documents authority does not give any kind of paths.  The last encoded path will always be msd:uniquenumber and so is useless
                    //as far as a presentable name is concerned.

                    string lastPathSegmentChild = dfs[0].Uri.LastPathSegment.Replace('/', '\\');
                    //last path segment child will be "raw:/storage/emulated/0/Download/Soulseek Incomplete" for the reasonable case and "msd:24" for the bad case
                    if (lastPathSegmentChild.Contains("\\"))
                    {
                        if (lastPathSegmentChild.StartsWith("raw:")) //scheme says "content" even though it starts with "raw:"
                        {
                            Logger.InfoFirebase("soft msdcase (raw:) : " + lastPathSegmentChild); //should be raw: provider
                            msdCase = true;
                            return String.Empty;
                        }
                        else
                        {
                            return SimpleHelpers.GetAllButLast(lastPathSegmentChild);
                        }
                    }
                    else
                    {
                        Logger.InfoFirebase("msdcase: " + lastPathSegmentChild); //should be msd:int
                        msdCase = true;
                        return String.Empty;
                    }
                }
                else
                {
                    Logger.InfoFirebase("downloads without any files");
                    return dir.Uri.LastPathSegment.Replace('/', '\\');
                }
            }
            else
            {
                return dir.Uri.LastPathSegment.Replace('/', '\\');
            }
        }

        /// <summary>
        /// returns false if unable to parse
        /// </summary>
        /// <param name="linkStringToParse"></param>
        /// <param name="username"></param>
        /// <param name="dirPath"></param>
        /// <param name="fullFilePath"></param>
        /// <param name="isFile"></param>
        /// <returns></returns>
        public static bool ParseSlskLinkString(string linkStringToParse, out string username, out string dirPath, out string fullFilePath, out bool isFile)
        {
            try
            {
                if (linkStringToParse.EndsWith('/'))
                {
                    isFile = false;
                }
                else
                {
                    isFile = true;
                }

                linkStringToParse = linkStringToParse.Substring(7);
                linkStringToParse = Android.Net.Uri.Decode(linkStringToParse);
                username = linkStringToParse.Substring(0, linkStringToParse.IndexOf('/'));
                fullFilePath = linkStringToParse.Substring(linkStringToParse.IndexOf('/') + 1).TrimEnd('/').Replace('/', '\\');
                if (isFile)
                {
                    dirPath = SimpleHelpers.GetDirectoryRequestFolderName(fullFilePath);
                }
                else
                {
                    dirPath = fullFilePath;
                }
            }
            catch (Exception e)
            {
                Logger.Firebase("failure to parse: " + linkStringToParse);
                username = dirPath = fullFilePath = null;
                isFile = false;
                return false;
            }
            return true;
        }

        public static string CreateSlskLink(bool isDirectory, string fullFileOrFolderName, string username)
        {
            string link = username + "/" + fullFileOrFolderName.Replace("\\", "/");
            if (isDirectory)
            {
                link = link + "/";
            }
            return "slsk://" + Android.Net.Uri.Encode(link, "/");
        }

        public static void CreateNotificationChannel(Context c, string id, string name, Android.App.NotificationImportance importance = Android.App.NotificationImportance.Low)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                NotificationChannel serviceChannel = new NotificationChannel(
                        id,
                        name,
                        importance
                );
                NotificationManager manager = c.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.CreateNotificationChannel(serviceChannel);
            }
        }

        public static Notification CreateNotification(Context context, PendingIntent pendingIntent, string channelID, string titleText, string contentText, bool setOnlyAlertOnce = true, bool forForegroundService = false, bool shutdownAction = false)
        {
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.


            //here we use the non compat notif builder as we want the special SetForegroundServiceBehavior method to prevent the new 10 second foreground notification delay.
            Notification notification = null;
            if (OperatingSystem.IsAndroidVersionAtLeast(31) && forForegroundService)
            {
                var builder = new Notification.Builder(context, channelID)
                          .SetContentTitle(titleText)
                          .SetContentText(contentText)
                          .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                          .SetContentIntent(pendingIntent)
                          .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                          .SetForegroundServiceBehavior((int)(Android.App.NotificationForegroundService.Immediate)) //new for api 31+
                          .SetTicker(titleText);
                if (shutdownAction)
                {
                    Intent intent3 = new Intent(context, typeof(CloseActivity));
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    var pi = PendingIntent.GetActivity(context, 7618, intent3, PendingIntentFlags.Immutable);

                    Notification.Action replyAction = new Notification.Action.Builder(Resource.Drawable.ic_cancel_black_24dp, "Shutdown", pi).Build();
                    builder.AddAction(replyAction);
                }
                notification = builder.Build();
            }
            else
            {
                var builder = new NotificationCompat.Builder(context, channelID)
                          .SetContentTitle(titleText)
                          .SetContentText(contentText)
                          .SetSmallIcon(Resource.Drawable.ic_stat_soulseekicontransparent)
                          .SetContentIntent(pendingIntent)
                          .SetOnlyAlertOnce(setOnlyAlertOnce) //maybe
                          .SetTicker(titleText);
                //for < 21 it is possible (must use png icon instead of xml) but the icon does look great
                //  and it doesnt clear from recents..
                if (shutdownAction && OperatingSystem.IsAndroidVersionAtLeast(21))
                {
                    Intent intent3 = new Intent(context, typeof(CloseActivity));
                    intent3.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
                    var pi = PendingIntent.GetActivity(context, 7618, intent3, 0);
                    NotificationCompat.Action replyAction = new NotificationCompat.Action.Builder(Resource.Drawable.ic_cancel_black_24dp, "Shutdown", pi).Build();
                    builder.AddAction(replyAction);
                }
                notification = builder.Build();
            }
            return notification;

        }

        /// <summary>
        /// TODO everything should probably use this wrapper
        /// </summary>
        /// <returns></returns>
        public static bool PerformConnectionRequiredAction(Action action, string notLoggedInToast = null)
        {
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                if(string.IsNullOrEmpty(notLoggedInToast))
                {
                    notLoggedInToast = SeekerState.ActiveActivityRef.GetString(Resource.String.must_be_logged_in_generic);
                }
                SeekerApplication.Toaster.ShowToast(notLoggedInToast, ToastLength.Short);
                return false;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return false; //if we get here we already did a toast message.
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { action(); });
                }));
                return true;
            }
            else
            {
                action();
                return true;
            }
        }

        public static void ChangePasswordLogic(string newPassword)
        {
            SeekerState.SoulseekClient.ChangePasswordAsync(newPassword).ContinueWith(new Action<Task>
                ((Task t) =>
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        if (t.IsFaulted)
                        {
                            if (t.Exception.InnerException is TimeoutException)
                            {
                                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_change_password) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                            }
                            else
                            {
                                Logger.Firebase("Failed to change password" + t.Exception.InnerException.Message);
                                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_change_password), ToastLength.Long);
                            }
                            return;
                        }
                        else
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.password_successfully_updated), ToastLength.Long);
                            PreferencesState.Password = newPassword;
                            PreferencesManager.SavePassword();
                        }
                    });
                }
                ));

        }

        /// <summary>
        /// Since this is always called by the UI it handles showing toasts etc.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="numDays"></param>
        /// <returns>false if operation could not be attempted, true if successfully met prereqs and was attempted</returns>
        public static bool GivePrilegesAPI(string username, string numDays)
        {
            int numDaysInt = int.MinValue;
            if (!int.TryParse(numDays, out numDaysInt))
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_days_entered_no_parse), ToastLength.Long);
                return false;
            }
            if (numDaysInt <= 0)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.error_days_entered_not_positive), ToastLength.Long);
                return false;
            }
            if (PrivilegesManager.Instance.GetRemainingDays() < numDaysInt)
            {
                SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.error_insufficient_days), numDaysInt), ToastLength.Long);
                return false;
            }
            if (!PreferencesState.CurrentlyLoggedIn)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_in_to_give_privileges), ToastLength.Short);
                return false;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SeekerState.ActiveActivityRef, false, out t))
                {
                    return false; //if we get here we already did a toast message.
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                        return;
                    }
                    SeekerState.ActiveActivityRef.RunOnUiThread(() => { GivePrivilegesLogic(username, numDaysInt); });
                }));
                return true;
            }
            else
            {
                GivePrivilegesLogic(username, numDaysInt);
                return true;
            }
        }


        private static void GivePrivilegesLogic(string username, int numDaysInt)
        {
            SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.sending__), ToastLength.Short);
            SeekerState.SoulseekClient.GrantUserPrivilegesAsync(username, numDaysInt).ContinueWith(new Action<Task>
                ((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception.InnerException is TimeoutException)
                        {
                            SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                        }
                        else
                        {
                            Logger.Firebase(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + t.Exception.InnerException.Message);
                            SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv), ToastLength.Long);
                        }
                        return;
                    }
                    else
                    {
                        //now there is a chance the user does not exist or something happens.  in which case our days will be incorrect...
                        PrivilegesManager.Instance.SubtractDays(numDaysInt);

                        SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.give_priv_success), numDaysInt, username), ToastLength.Long);

                        //it could be a good idea to then GET privileges to see if it actually went through... but I think this is good enough...
                        //in the rare case that it fails they do get a message so they can figure it out
                    }
                }));
        }

        public static void SaveUserNotes()
        {
            PreferencesManager.SaveUserNotes(SerializationHelper.SaveUserNotesToString(SeekerState.UserNotes));
        }


        public static void SaveOnlineAlerts()
        {
            PreferencesManager.SaveUserOnlineAlerts(SerializationHelper.SaveUserOnlineAlertsToString(SeekerState.UserOnlineAlerts));
        }


        public static void SaveToDisk(Context c, byte[] data, Java.IO.File dir, string name)
        {
            using (Java.IO.File fileForOurInternalStorage = new Java.IO.File(dir, name))
            {
                // Atomic file guarantees file integrity by ensuring that a file has been completely written and sync'd
                //   to disk before renaming it to the original file.
                var atomicFile = new AtomicFile(fileForOurInternalStorage);
                var fileStream = atomicFile.StartWrite();
                fileStream.Write(data, 0, data.Length);
                atomicFile.FinishWrite(fileStream);
            }
        }
    }

}
