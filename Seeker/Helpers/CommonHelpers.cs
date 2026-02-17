using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Messages;
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

        public static void ConfigureSpecialLinks(TextView textView, string msgText, SimpleHelpers.SpecialMessageType specialMessageType)
        {
            Android.Text.SpannableString messageText = new Android.Text.SpannableString(msgText);
            if (specialMessageType.HasFlag(SimpleHelpers.SpecialMessageType.MagnetLink))
            {
                var matches = SimpleHelpers.MagnetLinkRegex.Matches(msgText);
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
            if (specialMessageType.HasFlag(SimpleHelpers.SpecialMessageType.SlskLink))
            {
                var matches = SimpleHelpers.SlskLinkRegex.Matches(msgText);
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
                if (UserListService.ContainsUser(username)) //if we already have added said user, change title add to remove..
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
                bool isInUserList = UserListService.ContainsUser(username);
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
            if (!UserListService.ContainsUser(username))
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
            if (UserListService.ContainsUser(username))
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


        public static AndroidX.AppCompat.App.AlertDialog _dialogInstance;
        public static void ShowSimpleDialog(
            Activity owner,
            int dialogContentId,
            string title,
            Action<object, string> okayAction,
            string okayString,
            Action<object> cancelAction = null,
            string hint = null,
            string cancelString = null,
            string emptyTextErrorString = null,
            bool textRequired = true)
        {
            if (string.IsNullOrEmpty(cancelString))
            {
                cancelString = SeekerState.ActiveActivityRef.GetString(Resource.String.cancel);
            }

            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(owner, Resource.Style.MyAlertDialogTheme);
            builder.SetTitle(title);

            View viewInflated = LayoutInflater.From(owner).Inflate(dialogContentId, (ViewGroup)owner.FindViewById(Android.Resource.Id.Content).RootView, false);

            EditText input = (EditText)viewInflated.FindViewById<EditText>(Resource.Id.innerEditText);
            if (!string.IsNullOrEmpty(hint))
            {
                input.Hint = hint;
            }

            builder.SetView(viewInflated);

            if (cancelAction == null)
            {
                cancelAction = (object sender) =>
                {
                    if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                    {
                        aDiag.Dismiss();
                    }
                    else
                    {
                        CommonHelpers._dialogInstance.Dismiss();
                    }
                };
            }

            void eventHandlerOkay(object sender, DialogClickEventArgs e)
            {
                string txt = input.Text;
                if (string.IsNullOrEmpty(txt) && textRequired)
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.must_type_ticker_text), ToastLength.Short);
                    //(sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    return;
                }
                okayAction(sender, input.Text);
                _dialogInstance = null;
            }

            void eventHandlerCancel(object sender, DialogClickEventArgs e)
            {
                cancelAction(sender);
                _dialogInstance = null;
            }

            void inputEditorAction(object sender, TextView.EditorActionEventArgs e)
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Send ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //ImeNull if being called due to the enter key being pressed. (MSDN) but ImeNull gets called all the time....
                {
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(owner.FindViewById(Android.Resource.Id.Content).RootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Firebase(ex.Message + " error closing keyboard");
                    }

                    if (string.IsNullOrEmpty(input.Text) && textRequired)
                    {
                        if (string.IsNullOrEmpty(emptyTextErrorString))
                        {
                            emptyTextErrorString = "Input Required";
                        }
                        SeekerApplication.ShowToast(emptyTextErrorString, ToastLength.Short);
                    }
                    else
                    {
                        eventHandlerOkay(sender, null);
                    }
                }
            };

            void inputFocusChange(object sender, View.FocusChangeEventArgs e)
            {
                try
                {
                    SeekerState.ActiveActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
                }
                catch (System.Exception err)
                {
                    Logger.Firebase("simpleDialog_FocusChange" + err.Message);
                }
            }

            input.EditorAction += inputEditorAction;
            input.FocusChange += inputFocusChange;

            builder.SetPositiveButton(okayString, eventHandlerOkay);
            builder.SetNegativeButton(cancelString, eventHandlerCancel);
            // Set up the buttons

            _dialogInstance = builder.Create();

            try
            {
                _dialogInstance.Show();
                CommonHelpers.DoNotEnablePositiveUntilText(_dialogInstance, input);
            }
            catch (WindowManagerBadTokenException e)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("commonDialog WindowManagerBadTokenException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = owner.IsFinishing;
                    Logger.Firebase("commonDialog WindowManagerBadTokenException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
            catch (Exception err)
            {
                if (SeekerState.ActiveActivityRef == null)
                {
                    Logger.Firebase("commonDialogException null activities");
                }
                else
                {
                    bool isCachedMainActivityFinishing = SeekerState.ActiveActivityRef.IsFinishing;
                    bool isOurActivityFinishing = owner.IsFinishing;
                    Logger.Firebase("commonDialogException are we finishing:" + isCachedMainActivityFinishing + isOurActivityFinishing);
                }
            }
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
                UserListService.RemoveUser(usernameInQuestion);
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
            if (SimpleHelpers.IsSpecialMessage(msg.MessageText, out SimpleHelpers.SpecialMessageType specialMessageType))
            {
                if (specialMessageType.HasFlag(SimpleHelpers.SpecialMessageType.SlashMe))
                {
                    viewMessage.Text = SimpleHelpers.ParseSpecialMessage(msg.MessageText);
                    viewMessage.SetTypeface(null, Android.Graphics.TypefaceStyle.Italic);
                }
                else if (specialMessageType.HasFlag(SimpleHelpers.SpecialMessageType.MagnetLink) || specialMessageType.HasFlag(SimpleHelpers.SpecialMessageType.SlskLink))
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
                if (e.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
                {
                    Logger.Firebase("viewUri: " + e.Message + httpUri.ToString());
                    SeekerApplication.ShowToast(string.Format("No application found to handle url \"{0}\".  Please install or enable web browser.", httpUri.ToString()), ToastLength.Long);
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
                            return CommonHelpers.GetAllButLast(lastPathSegmentChild);
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

        public static void SetToolTipText(View v, string tip)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
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
                Toast.MakeText(SeekerState.ActiveActivityRef, notLoggedInToast, ToastLength.Short).Show();
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
                                SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_change_password) + ": " + SeekerApplication.GetString(Resource.String.timeout), ToastLength.Long);
                            }
                            else
                            {
                                Logger.Firebase("Failed to change password" + t.Exception.InnerException.Message);
                                SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.failed_to_change_password), ToastLength.Long);
                            }
                            return;
                        }
                        else
                        {
                            SeekerApplication.ShowToast(SeekerApplication.GetString(Resource.String.password_successfully_updated), ToastLength.Long);
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
            if (!PreferencesState.CurrentlyLoggedIn)
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
                            Logger.Firebase(SeekerState.ActiveActivityRef.GetString(Resource.String.error_give_priv) + t.Exception.InnerException.Message);
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

        public static void ShowReportErrorDialog(Context c, string message)
        {
            Dialog dialog = new Dialog(c);
            dialog.SetContentView(Resource.Layout.error_dialog_layout);

            Button btnReport = dialog.FindViewById(Resource.Id.btnReport) as Button;
            Button btnClose = dialog.FindViewById(Resource.Id.btnClose) as Button;

            void BtnReport_Click(object sender, EventArgs e)
            {
                Intent intent = new Intent(Intent.ActionSendto);
                intent.SetData(Android.Net.Uri.Parse("mailto:"));
                intent.PutExtra(Intent.ExtraEmail, new String[] { "jbonadies6@gmail.com" });
                intent.PutExtra(Intent.ExtraSubject, $"Seeker Bug: {message}");
                intent.PutExtra(Intent.ExtraText, "Please describe the issue here...");
                SeekerState.ActiveActivityRef.StartActivity(Intent.CreateChooser(intent, "Email:"));
                dialog?.Dismiss();
            }

            btnReport.Click += BtnReport_Click;
            btnClose.Click += (object sender, EventArgs e) => { dialog?.Dismiss(); };

            dialog.SetSizeProportional(.9, -1);

            dialog.Show();
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
            PreferencesManager.SaveUserNotes(SerializationHelper.SaveUserNotesToString(SeekerState.UserNotes));
        }


        public static void SaveOnlineAlerts()
        {
            PreferencesManager.SaveUserOnlineAlerts(SerializationHelper.SaveUserOnlineAlertsToString(SeekerState.UserOnlineAlerts));
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
                    Logger.Debug("IME ACTION: " + e.ActionId.ToString());
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
                        Logger.Firebase(ex.Message + " error closing keyboard");
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
