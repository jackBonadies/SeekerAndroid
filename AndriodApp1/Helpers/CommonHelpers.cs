using AndriodApp1.Helpers;
using AndriodApp1.Managers;
using AndriodApp1.Messages;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.DocumentFile.Provider;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AndriodApp1
{
    public static class CommonHelpers
    {
        public static string AvoidLineBreaks(string orig)
        {
            return orig.Replace(' ', '\u00A0').Replace("\\", "\\\u2060");
        }
        /// <summary>
        /// This is necessary since DocumentFile.ListFiles() returns files in an incomprehensible order (not by name, size, modified, inode, etc.)
        /// </summary>
        /// <param name="files"></param>
        public static void SortSlskDirFiles(List<Soulseek.File> files)
        {
            files.Sort((x, y) => x.Filename.CompareTo(y.Filename));
        }

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


                //    string volume1 = MainActivity.GetVolumeName(SeekerState.RootDocumentFile.Uri.LastPathSegment, out _);
                //    string volume2 = MainActivity.GetVolumeName(SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment, out _);

                //    return uuid1 != uuid2;
                //}
                //else
                //{
                try
                {
                    string volume1 = MainActivity.GetVolumeName(SeekerState.RootDocumentFile.Uri.LastPathSegment, false, out bool everything);
                    if (everything)
                    {
                        volume1 = SeekerState.RootDocumentFile.Uri.LastPathSegment;
                    }
                    string volume2 = MainActivity.GetVolumeName(SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment, false, out everything);
                    if (everything)
                    {
                        volume2 = SeekerState.RootIncompleteDocumentFile.Uri.LastPathSegment;
                    }
                    return volume1 != volume2;
                }
                catch (Exception e)
                {
                    MainActivity.LogFirebase("CompleteIncompleteDifferentVolume failed: " + e.Message + SeekerState.RootDocumentFile?.Uri?.LastPathSegment + " incomplete: " + SeekerState.RootIncompleteDocumentFile?.Uri?.LastPathSegment);
                    return false;
                }
                //}
            }
            else
            {
                return false;
            }
        }

        public static string GenerateIncompleteFolderName(string username, string fullFileName, int depth)
        {
            string albumFolderName = null;
            if (depth == 1)
            {
                albumFolderName = CommonHelpers.GetFolderNameFromFile(fullFileName, depth);
            }
            else
            {
                albumFolderName = CommonHelpers.GetFolderNameFromFile(fullFileName, depth);
                albumFolderName = albumFolderName.Replace('\\', '_');
            }
            string incompleteFolderName = username + "_" + albumFolderName;
            //Path.GetInvalidPathChars() doesnt seem like enough bc I still get failures on ''' and '&'
            foreach (char c in System.IO.Path.GetInvalidPathChars().Union(new[] { '&', '\'' }))
            {
                incompleteFolderName = incompleteFolderName.Replace(c, '_');
            }
            return incompleteFolderName;
        }

        public static bool IsFileUri(string uriString)
        {
            if (uriString.StartsWith("file:"))
            {
                return true;
            }
            else if (uriString.StartsWith("content:"))
            {
                return false;
            }
            else
            {
                throw new Exception("IsFileUri failed: " + uriString);
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
                MainActivity.LogFirebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == CommonHelpers.GetDateTimeNowSafe().Date)
            {
                return SeekerState.ActiveActivityRef.GetString(Resource.String.today) + " " + dt.ToString("h:mm:ss tt", cultureInfo); //cultureInfo can be null without issue..
            }
            else
            {
                return dt.ToString("MMM d h:mm:ss tt", cultureInfo);
            }
        }

        [Flags]
        public enum SpecialMessageType : short
        {
            None = 0,
            SlashMe = 1,
            MagnetLink = 2,
            SlskLink = 4,
        }

        /// <summary>
        /// true if '/me ' message
        /// </summary>
        /// <returns>true if special message</returns>
        public static bool IsSpecialMessage(string msg, out SpecialMessageType specialMessageType)
        {
            specialMessageType = SpecialMessageType.None;
            if (string.IsNullOrEmpty(msg))
            {
                return false;
            }
            if (msg.StartsWith(@"/me "))
            {
                specialMessageType = SpecialMessageType.SlashMe;
                return true;
            }
            if (msg.Contains(@"magnet:?xt=urn:"))
            {
                specialMessageType = SpecialMessageType.MagnetLink;
                return true;
            }
            if (msg.Contains(@"slsk://"))
            {
                specialMessageType = SpecialMessageType.SlskLink;
                return true;
            }
            return false;
        }

        private readonly static System.Text.RegularExpressions.Regex MagnetLinkRegex = new System.Text.RegularExpressions.Regex(@"magnet:\?xt=urn:[^ ""]+");
        private readonly static System.Text.RegularExpressions.Regex SlskLinkRegex = new System.Text.RegularExpressions.Regex(@"slsk://[^ ""]+");

        public static void ConfigureSpecialLinks(TextView textView, string msgText, SpecialMessageType specialMessageType)
        {
            Android.Text.SpannableString messageText = new Android.Text.SpannableString(msgText);
            if (specialMessageType.HasFlag(SpecialMessageType.MagnetLink))
            {
                var matches = MagnetLinkRegex.Matches(msgText);
                //add in our spans.
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var m = match as System.Text.RegularExpressions.Match;
                        var ourMagnetSpan = new MagnetLinkClickableSpan(m.Value);
                        messageText.SetSpan(ourMagnetSpan, m.Index, m.Index + m.Length, Android.Text.SpanTypes.InclusiveExclusive);
                    }
                }
            }
            if (specialMessageType.HasFlag(SpecialMessageType.SlskLink))
            {
                var matches = SlskLinkRegex.Matches(msgText);
                //add in our spans.
                if (matches.Count > 0)
                {
                    foreach (var match in matches)
                    {
                        var m = match as System.Text.RegularExpressions.Match;
                        var ourSlskSpan = new SlskLinkClickableSpan(m.Value);
                        messageText.SetSpan(ourSlskSpan, m.Index, m.Index + m.Length, Android.Text.SpanTypes.InclusiveExclusive);
                    }
                }
            }
            textView.MovementMethod = Android.Text.Method.LinkMovementMethod.Instance; //needed for slsk:// not needed for magnet. weird.
            textView.TextFormatted = messageText;
        }

        public static string ParseSpecialMessage(string msg)
        {
            if (IsSpecialMessage(msg, out SpecialMessageType specialMessageType))
            {
                //if slash me dont include other special links, too excessive.
                if (specialMessageType == SpecialMessageType.SlashMe)
                {
                    //"/me goes to the store"
                    //"goes to the store" + style
                    return msg.Substring(4, msg.Length - 4);
                }
                else
                {
                    return msg;
                }
            }
            else
            {
                return msg;

            }
        }

        public static void AddUserNoteMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            string title = null;
            if (SeekerState.UserNotes.ContainsKey(username))
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.edit_note);
            }
            else
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.add_note);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void AddUserOnlineAlertMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            string title = null;
            if (SeekerState.UserOnlineAlerts.ContainsKey(username))
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.remove_online_alert);
            }
            else
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.set_online_alert);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void SetIgnoreUnignoreTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (SeekerApplication.IsUserInIgnoreList(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.ignore_user))
                    {
                        menuItem.SetTitle(Resource.String.remove_from_ignored);
                    }
                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.remove_from_ignored))
                    {
                        menuItem.SetTitle(Resource.String.ignore_user);
                    }
                }
            }
        }

        private static void SetAddRemoveTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (MainActivity.UserListContainsUser(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.add_to_user_list))
                    {
                        menuItem.SetTitle(Resource.String.remove_from_user_list);
                    }
                    else if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.add_user))
                    {
                        menuItem.SetTitle(Resource.String.remove_user);
                    }
                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.remove_from_user_list))
                    {
                        menuItem.SetTitle(Resource.String.add_to_user_list);
                    }
                    else if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.remove_user))
                    {
                        menuItem.SetTitle(Resource.String.add_user);
                    }
                }
            }
        }

        private static void SetAddNoteEditNoteTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (SeekerState.UserNotes.ContainsKey(username)) //if we already have added said user, change title add to remove..
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.add_note))
                    {
                        menuItem.SetTitle(Resource.String.edit_note);
                    }

                }
                else
                {
                    if (menuItem.TitleFormatted.ToString() == SeekerState.ActiveActivityRef.GetString(Resource.String.edit_note))
                    {
                        menuItem.SetTitle(Resource.String.add_note);
                    }
                }
            }
        }

        public static void SetMenuTitles(IMenu menu, string username)
        {
            var menuItem = menu.FindItem(Resource.Id.action_add_to_user_list);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_add_user);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.addUser);
            SetAddRemoveTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_add_note);
            SetAddNoteEditNoteTitle(menuItem, username);
            menuItem = menu.FindItem(Resource.Id.action_ignore);
            SetIgnoreUnignoreTitle(menuItem, username);
        }

        public static void SetIgnoreAddExclusive(IMenu menu, string username)
        {
            // if we added this user as a friend do not show the option to ignore. they must be removed first.
            if (!string.IsNullOrEmpty(username))
            {
                bool isInUserList = MainActivity.UserListContainsUser(username);
                var menuItem = menu.FindItem(Resource.Id.action_ignore);
                menuItem?.SetVisible(!isInUserList);
            }


            // if we have this user in ignore, do not show the option to add as friend.
            if (!string.IsNullOrEmpty(username))
            {
                bool isInIgnoreList = SeekerApplication.IsUserInIgnoreList(username);
                var menuItem = menu.FindItem(Resource.Id.action_add_to_user_list);
                menuItem?.SetVisible(!isInIgnoreList);
                menuItem = menu.FindItem(Resource.Id.action_add_user);
                menuItem?.SetVisible(!isInIgnoreList);
                menuItem = menu.FindItem(Resource.Id.action_add_note);
                menuItem?.SetVisible(!isInIgnoreList);
            }
        }


        public static void AddAddRemoveUserMenuItem(IMenu menu, int i, int j, int k, string username, bool full_title = false)
        {
            string title = null;
            if (!MainActivity.UserListContainsUser(username))
            {
                if (full_title)
                {
                    title = SeekerState.ActiveActivityRef.GetString(Resource.String.add_to_user_list);
                }
                else
                {
                    title = SeekerState.ActiveActivityRef.GetString(Resource.String.add_user);
                }
            }
            else
            {
                if (full_title)
                {
                    title = SeekerState.ActiveActivityRef.GetString(Resource.String.remove_from_user_list);
                }
                else
                {
                    title = SeekerState.ActiveActivityRef.GetString(Resource.String.remove_user);
                }
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static void AddIgnoreUnignoreUserMenuItem(IMenu menu, int i, int j, int k, string username)
        {
            //ignored and added are mutually exclusive.  you cannot have a user be both ignored and added.
            if (MainActivity.UserListContainsUser(username))
            {
                return;
            }
            string title = null;
            if (!SeekerApplication.IsUserInIgnoreList(username))
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.ignore_user);
            }
            else
            {
                title = SeekerState.ActiveActivityRef.GetString(Resource.String.remove_from_ignored);
            }
            if (i != -1)
            {
                menu.Add(i, j, k, title);
            }
            else
            {
                menu.Add(title);
            }
        }

        public static DateTime GetDateTimeNowSafe()
        {
            try
            {
                return DateTime.Now;
            }
            catch (System.TimeZoneNotFoundException)
            {
                return DateTime.UtcNow;
            }
        }

        public static void AddGivePrivilegesIfApplicable(IMenu menu, int indexToUse)
        {
            if (PrivilegesManager.Instance.GetRemainingDays() >= 1)
            {
                if (indexToUse == -1)
                {
                    menu.Add(Resource.String.give_privileges);
                }
                else
                {
                    menu.Add(indexToUse, indexToUse, indexToUse, Resource.String.give_privileges);
                }
            }
        }

        public static PendingIntentFlags AppendMutabilityIfApplicable(PendingIntentFlags existingFlags, bool immutable)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
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

        public static void DoNotEnablePositiveUntilText(AndroidX.AppCompat.App.AlertDialog dialog, EditText input)
        {
            var positiveButton = dialog.GetButton((int)DialogButtonType.Positive);
            // note: this will be null if .Show() has not been called.
            if (positiveButton == null)
            {
                // better to be safe.
                return;
            }
            positiveButton.Enabled = false;

            void Input_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
            {
                if (string.IsNullOrEmpty(input.Text))
                {
                    positiveButton.Enabled = false;
                }
                else
                {
                    positiveButton.Enabled = true;
                }
            }
            input.AfterTextChanged += Input_AfterTextChanged;
        }

        /// <summary>
        /// returns true if found and handled.  a time saver for the more generic context menu items..
        /// </summary>
        /// <returns></returns>
        public static bool HandleCommonContextMenuActions(string contextMenuTitle, string usernameInQuestion, Context activity, View browseSnackView, Action uiUpdateActionNote = null, Action uiUpdateActionAdded_Removed = null, Action uiUpdateActionIgnored_Unignored = null, Action uiUpdateSetResetOnlineAlert = null)
        {
            if (activity == null)
            {
                activity = SeekerState.ActiveActivityRef;
            }
            if (contextMenuTitle == activity.GetString(Resource.String.ignore_user))
            {
                SeekerApplication.AddToIgnoreListFeedback(activity, usernameInQuestion);
                SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_ignored))
            {
                SeekerApplication.RemoveFromIgnoreListFeedback(activity, usernameInQuestion);
                SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.msg_user))
            {
                Intent intentMsg = new Intent(activity, typeof(MessagesActivity));
                intentMsg.AddFlags(ActivityFlags.SingleTop);
                intentMsg.PutExtra(MessageController.FromUserName, usernameInQuestion); //so we can go to this user..
                intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                activity.StartActivity(intentMsg);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.add_to_user_list) ||
                contextMenuTitle == activity.GetString(Resource.String.add_user))
            {
                UserListActivity.AddUserAPI(SeekerState.ActiveActivityRef, usernameInQuestion, uiUpdateActionAdded_Removed);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_user_list) ||
                contextMenuTitle == activity.GetString(Resource.String.remove_user))
            {
                MainActivity.ToastUI_short(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.removed_user), usernameInQuestion));
                MainActivity.UserListRemoveUser(usernameInQuestion);
                SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionAdded_Removed);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.search_user_files))
            {
                SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                SearchTabHelper.SearchTargetChosenUser = usernameInQuestion;
                //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                Intent intent = new Intent(activity, typeof(MainActivity));
                intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                intent.AddFlags(ActivityFlags.SingleTop); //??
                activity.StartActivity(intent);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.browse_user))
            {
                Action<View> action = new Action<View>((v) =>
                {
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                    intent.AddFlags(ActivityFlags.SingleTop); //??
                    activity.StartActivity(intent);
                    //((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                });
                DownloadDialog.RequestFilesApi(usernameInQuestion, browseSnackView, action, null);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.get_user_info))
            {
                RequestedUserInfoHelper.RequestUserInfoApi(usernameInQuestion);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.give_privileges))
            {
                ShowGivePrilegesDialog(usernameInQuestion);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.edit_note) ||
                    contextMenuTitle == activity.GetString(Resource.String.add_note))
            {
                ShowEditAddNoteDialog(usernameInQuestion, uiUpdateActionNote);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.set_online_alert))
            {
                SeekerState.UserOnlineAlerts[usernameInQuestion] = 0;
                CommonHelpers.SaveOnlineAlerts();
                uiUpdateSetResetOnlineAlert();
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_online_alert))
            {
                SeekerState.UserOnlineAlerts.TryRemove(usernameInQuestion, out _);
                CommonHelpers.SaveOnlineAlerts();
                uiUpdateSetResetOnlineAlert();

            }
            return false;
        }

        public static void SetMessageTextView(TextView viewMessage, Message msg)
        {
            if (CommonHelpers.IsSpecialMessage(msg.MessageText, out SpecialMessageType specialMessageType))
            {
                if (specialMessageType.HasFlag(SpecialMessageType.SlashMe))
                {
                    viewMessage.Text = CommonHelpers.ParseSpecialMessage(msg.MessageText);
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Italic);
                }
                else if (specialMessageType.HasFlag(SpecialMessageType.MagnetLink) || specialMessageType.HasFlag(SpecialMessageType.SlskLink))
                {
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
                    CommonHelpers.ConfigureSpecialLinks(viewMessage, msg.MessageText, specialMessageType);
                }
                else
                {
                    //fallback
                    viewMessage.Text = msg.MessageText;
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
                }
            }
            else
            {
                viewMessage.Text = msg.MessageText;
                viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Normal);
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
                MainActivity.LogFirebase("CANNOT GET CURRENT CULTURE: " + e.Message + e.StackTrace);
            }
            if (dt.Date == CommonHelpers.GetDateTimeNowSafe().Date)
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

        public static string GetFileNameFromFile(string filename) //is also used to get the last folder
        {
            int begin = filename.LastIndexOf("\\");
            string clipped = filename.Substring(begin + 1);
            return clipped;
        }

        public static string GetAllButLast(string path) //"raw:\\storage\\emulated\\0\\Download\\Soulseek Complete"
        {
            int end = path.LastIndexOf("\\");
            string clipped = path.Substring(0, end);
            return clipped; //"raw:\\storage\\emulated\\0\\Download"
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
                if (e.Message.Contains(CommonHelpers.NoDocumentOpenTreeToHandle))
                {
                    MainActivity.LogFirebase("viewUri: " + e.Message + httpUri.ToString());
                    SeekerApplication.ShowToast(string.Format("No application found to handle url \"{0}\".  Please install or enable web browser.", httpUri.ToString()), ToastLength.Long);
                }
            }
        }

        public const string NoDocumentOpenTreeToHandle = "No Activity found to handle Intent";

        public static bool IsUploadCompleteOrAborted(TransferStates state)
        {
            return (state.HasFlag(TransferStates.Succeeded) || state.HasFlag(TransferStates.Cancelled) || state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Completed) || state.HasFlag(TransferStates.Rejected));
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
                            MainActivity.LogInfoFirebase("soft msdcase (raw:) : " + lastPathSegmentChild); //should be raw: provider
                            msdCase = true;
                            return String.Empty;
                        }
                        else
                        {
                            return CommonHelpers.GetAllButLast(lastPathSegmentChild);
                        }
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("msdcase: " + lastPathSegmentChild); //should be msd:int
                        msdCase = true;
                        return String.Empty;
                    }
                }
                else
                {
                    MainActivity.LogInfoFirebase("downloads without any files");
                    return dir.Uri.LastPathSegment.Replace('/', '\\');
                }
            }
            else
            {
                return dir.Uri.LastPathSegment.Replace('/', '\\');
            }
        }

        private static string GetUnlockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        public static string SlskLinkClickedData = null;
        public static bool ShowSlskLinkContextMenu = false;

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
                    dirPath = CommonHelpers.GetDirectoryRequestFolderName(fullFilePath);
                }
                else
                {
                    dirPath = fullFilePath;
                }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase("failure to parse: " + linkStringToParse);
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

        private static string GetLockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.LockedFiles.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// This will prepend the lock when applicable..
        /// </summary>
        /// <returns></returns>
        public static string GetFolderNameForSearchResult(SearchResponse item)
        {
            if (item.FileCount > 0)
            {
                return CommonHelpers.GetFolderNameFromFile(GetUnlockedFileName(item));
            }
            else if (item.LockedFileCount > 0)
            {
                return new System.String(Java.Lang.Character.ToChars(0x1F512)) + CommonHelpers.GetFolderNameFromFile(GetLockedFileName(item));
            }
            else
            {
                return "\\Locked\\";
            }
        }

        public static string GetFolderNameFromFile(string filename, int levels = 1)
        {
            try
            {
                int folderCount = 0;
                int index = -1; //-1 is important.  i.e. in the case of Folder\test.mp3, it can be Folder.
                int firstIndex = int.MaxValue;
                for (int i = filename.Length - 1; i >= 0; i--)
                {
                    if (filename[i] == '\\')
                    {
                        folderCount++;
                        if (firstIndex == int.MaxValue)
                        {
                            //strip off the file name
                            firstIndex = i;
                        }
                        if (folderCount == (levels + 1))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                return filename.Substring(index + 1, firstIndex - index - 1);
            }
            catch
            {
                return "";
            }
        }

        public static string GetParentFolderNameFromFile(string filename)
        {
            try
            {
                string parent = filename.Substring(0, filename.LastIndexOf('\\'));
                parent = parent.Substring(0, parent.LastIndexOf('\\'));
                parent = parent.Substring(parent.LastIndexOf('\\') + 1);
                return parent;
            }
            catch
            {
                return "";
            }
        }

        public static string GetTransferSpeedString(double bytesPerSecond)
        {
            if (bytesPerSecond > 1048576) //more than 1MB
            {
                return string.Format("{0:F1}mbs", bytesPerSecond / 1048576.0);
            }
            else
            {
                return string.Format("{0:F1}kbs", bytesPerSecond / 1024.0);
            }
        }

        public static string GetDateTimeSinceAbbrev(DateTime dtThen)
        {
            var dtNow = CommonHelpers.GetDateTimeNowSafe(); //2.5 microseconds
            if (dtNow.Day == dtThen.Day)
            {
                //if on same day then show time. 24 hour time? maybe option to change?
                //ex. 2:45, 20:34
                //hh:mm
                return dtThen.ToString("H:mm");
            }
            else if (dtNow.Year == dtThen.Year)
            {
                //if different day but same year show month day
                //ex. Jan 4
                return dtThen.ToString("MMM d"); // d = 7 or 17.
            }
            else
            {
                //if different year show full.
                //ex. Dec 30 2021
                return dtThen.ToString("MMM d yyyy");
            }
        }

        public static string GetSubHeaderText(SearchResponse searchResponse)
        {
            int numFiles = 0;
            long totalBytes = -1;
            if (SeekerState.HideLockedResultsInSearch)
            {
                numFiles = searchResponse.FileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size);
            }
            else
            {
                numFiles = searchResponse.FileCount + searchResponse.LockedFileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size) + searchResponse.LockedFiles.Sum(f => f.Size);
            }

            //if total bytes greater than 1GB 
            string sizeString = GetHumanReadableSize(totalBytes);

            var filesWithLength = searchResponse.Files.Where(f => f.Length.HasValue);
            if (!SeekerState.HideLockedResultsInSearch)
            {
                filesWithLength = filesWithLength.Concat(searchResponse.LockedFiles.Where(f => f.Length.HasValue));
            }
            string timeString = null;
            if (filesWithLength.Count() > 0)
            {
                //translate length into human readable
                timeString = GetHumanReadableTime(filesWithLength.Sum(f => f.Length.Value));
            }
            if (timeString == null)
            {
                return string.Format("{0} files • {1}", numFiles, sizeString);
            }
            else
            {
                return string.Format("{0} files • {1} • {2}", numFiles, sizeString, timeString);
            }


        }

        public static string GetSizeLengthAttrString(Soulseek.File f)
        {

            string sizeString = string.Format("{0:0.##} mb", f.Size / (1024.0 * 1024.0));
            string lengthString = f.Length.HasValue ? GetHumanReadableTime(f.Length.Value) : null;
            string attrString = GetHumanReadableAttributesForSingleItem(f);
            if (attrString == null && lengthString == null)
            {
                return sizeString;
            }
            else if (attrString == null)
            {
                return String.Format("{0} • {1}", sizeString, lengthString);
            }
            else if (lengthString == null)
            {
                return String.Format("{0} • {1}", sizeString, attrString);
            }
            else
            {
                return String.Format("{0} • {1} • {2}", sizeString, lengthString, attrString);
            }
        }


        public static string GetHumanReadableAttributesForSingleItem(Soulseek.File f)
        {

            int bitRate = -1;
            int bitDepth = -1;
            double sampleRate = double.NaN;
            foreach (var attr in f.Attributes)
            {
                switch (attr.Type)
                {
                    case FileAttributeType.BitRate:
                        bitRate = attr.Value;
                        break;
                    case FileAttributeType.BitDepth:
                        bitDepth = attr.Value;
                        break;
                    case FileAttributeType.SampleRate:
                        sampleRate = attr.Value / 1000.0;
                        break;
                }
            }
            if (bitRate == -1 && bitDepth == -1 && double.IsNaN(sampleRate))
            {
                return null; //nothing to add
            }
            else if (bitDepth != -1 && !double.IsNaN(sampleRate))
            {
                return bitDepth + ", " + sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ;
            }
            else if (!double.IsNaN(sampleRate))
            {
                return sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ;
            }
            else if (bitRate != -1)
            {
                return bitRate + SlskHelp.CommonHelpers.STRINGS_KBS;
            }
            else
            {
                return null;
            }
        }

        public static string GetHumanReadableSize(long totalBytes)
        {
            if (totalBytes > 1024 * 1024 * 1024)
            {
                return string.Format("{0:0.##} gb", totalBytes / (1024.0 * 1024.0 * 1024.0));
            }
            else
            {
                return string.Format("{0:0.##} mb", totalBytes / (1024.0 * 1024.0));
            }
        }


        public static string GetHumanReadableTime(int totalSeconds)
        {
            int sec = totalSeconds % 60;
            int minutes = (totalSeconds % 3600) / 60;
            int hours = (totalSeconds / 3600);
            if (minutes == 0 && hours == 0 && sec == 0)
            {
                return null;
            }
            else if (minutes == 0 && hours == 0)
            {
                return string.Format("{0}s", sec);
            }
            else if (hours == 0)
            {
                return string.Format("{0}m{1}s", minutes, sec);
            }
            else
            {
                return string.Format("{0}h{1}m{2}s", hours, minutes, sec);
            }
        }




        /// <summary>
        /// Get all BUT the filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetDirectoryRequestFolderName(string filename)
        {
            try
            {
                int end = filename.LastIndexOf("\\");
                string clipped = filename.Substring(0, end);
                return clipped;
            }
            catch
            {
                return "";
            }
        }

        public static void CreateNotificationChannel(Context c, string id, string name, Android.App.NotificationImportance importance = Android.App.NotificationImportance.Low)
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
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

        public static void SetToolTipText(View v, string tip)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                v.TooltipText = tip; //api26+ otherwise crash...
            }
            else
            {
                AndroidX.AppCompat.Widget.TooltipCompat.SetTooltipText(v, tip);
            }
        }


        public static Notification CreateNotification(Context context, PendingIntent pendingIntent, string channelID, string titleText, string contentText, bool setOnlyAlertOnce = true, bool forForegroundService = false, bool shutdownAction = false)
        {
            //no such method takes args CHANNEL_ID in API 25. API 26 = 8.0 which requires channel ID.
            //a "channel" is a category in the UI to the end user.


            //here we use the non compat notif builder as we want the special SetForegroundServiceBehavior method to prevent the new 10 second foreground notification delay.
            Notification notification = null;
            if ((int)Android.OS.Build.VERSION.SdkInt >= 31 && forForegroundService)
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
                if (shutdownAction && (int)Android.OS.Build.VERSION.SdkInt >= 21)
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
                MainActivity.ToastUI(Resource.String.error_days_entered_no_parse);
                return false;
            }
            if (numDaysInt <= 0)
            {
                MainActivity.ToastUI(Resource.String.error_days_entered_not_positive);
                return false;
            }
            if (PrivilegesManager.Instance.GetRemainingDays() < numDaysInt)
            {
                MainActivity.ToastUI(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.error_insufficient_days), numDaysInt));
                return false;
            }
            if (!SeekerState.currentlyLoggedIn)
            {
                Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.must_be_logged_in_to_give_privileges, ToastLength.Short).Show();
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
                        SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SeekerState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
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
            SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.sending__), ToastLength.Short);
            SeekerState.SoulseekClient.GrantUserPrivilegesAsync(username, numDaysInt).ContinueWith(new Action<Task>
                ((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if (t.Exception.InnerException is TimeoutException)
                        {
                            SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                        }
                        else
                        {
                            MainActivity.LogFirebase(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + t.Exception.InnerException.Message);
                            SeekerApplication.ShowToast(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv), ToastLength.Long);
                        }
                        return;
                    }
                    else
                    {
                        //now there is a chance the user does not exist or something happens.  in which case our days will be incorrect...
                        PrivilegesManager.Instance.SubtractDays(numDaysInt);

                        SeekerApplication.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.give_priv_success), numDaysInt, username), ToastLength.Long);

                        //it could be a good idea to then GET privileges to see if it actually went through... but I think this is good enough...
                        //in the rare case that it fails they do get a message so they can figure it out
                    }
                }));
        }

        public static void ShowEditAddNoteDialog(string username, Action uiUpdateAction = null)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SeekerState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.note_title), username));
            View viewInflated = LayoutInflater.From(SeekerState.ActiveActivityRef).Inflate(Resource.Layout.user_note_dialog, (ViewGroup)SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.editUserNote);

            string existingNote = null;
            SeekerState.UserNotes.TryGetValue(username, out existingNote);
            if (existingNote != null)
            {
                input.Text = existingNote;
            }


            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //in my testing the only "bad" input we can get is "0" or a number greater than what you have.  
                //you cannot input '.' or negative even with physical keyboard, etc.
                string newText = input.Text;
                bool isEmpty = string.IsNullOrEmpty(newText);
                bool wasEmpty = string.IsNullOrEmpty(existingNote);
                bool addedOrRemoved = isEmpty != wasEmpty;
                if (addedOrRemoved)
                {
                    //either we cleared an existing note or added a new note
                    if (!wasEmpty && isEmpty)
                    {
                        //we removed the note
                        SeekerState.UserNotes.TryRemove(username, out _);
                        SaveUserNotes();

                    }
                    else
                    {
                        //we added a note
                        SeekerState.UserNotes[username] = newText;
                        SaveUserNotes();
                    }
                    if (uiUpdateAction != null)
                    {
                        SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateAction);
                    }

                }
                else if (isEmpty && wasEmpty)
                {
                    //nothing was there and nothing is there now
                    return;
                }
                else //something was there and is there now
                {
                    if (newText == existingNote)
                    {
                        return;
                    }
                    else
                    {
                        //update note and save prefs..
                        SeekerState.UserNotes[username] = newText;
                        SaveUserNotes();
                    }
                }

            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            });

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }

        public static void SaveUserNotes()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserNotes, SerializationHelper.SaveUserNotesToString(SeekerState.UserNotes));
                editor.Commit();
            }
        }


        public static void SaveOnlineAlerts()
        {
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SeekerState.SharedPreferences.Edit();
                editor.PutString(KeyConsts.M_UserOnlineAlerts, SerializationHelper.SaveUserOnlineAlertsToString(SeekerState.UserOnlineAlerts));
                editor.Commit();
            }
        }


        public static void ShowGivePrilegesDialog(string username)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(SeekerState.ActiveActivityRef, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.give_to_), username));
            View viewInflated = LayoutInflater.From(SeekerState.ActiveActivityRef).Inflate(Resource.Layout.give_privileges_layout, (ViewGroup)SeekerState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), false);
            // Set up the input
            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.givePrivilegesEditText);

            // Specify the type of input expected; this, for example, sets the input as a password, and will mask the text
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                //in my testing the only "bad" input we can get is "0" or a number greater than what you have.  
                //you cannot input '.' or negative even with physical keyboard, etc.
                GivePrilegesAPI(username, input.Text);
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Send || //in this case it is Send (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search)
                {
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.MainActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(SeekerState.ActiveActivityRef.Window.DecorView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    //Do the Browse Logic...
                    eventHandler(sender, null);
                }
            };

            input.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.send, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }
    }

}