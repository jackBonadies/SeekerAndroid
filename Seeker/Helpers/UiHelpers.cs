using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Common;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Messages;
using System;
using System.Threading.Tasks;

namespace Seeker
{
    public static class UiHelpers
    {
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
                if (UserListService.Instance.ContainsUser(username)) //if we already have added said user, change title add to remove..
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
                bool isInUserList = UserListService.Instance.ContainsUser(username);
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
            if (!UserListService.Instance.ContainsUser(username))
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
            if (UserListService.Instance.ContainsUser(username))
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

            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(owner);
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
                        UiHelpers._dialogInstance.Dismiss();
                    }
                };
            }

            void eventHandlerOkay(object sender, DialogClickEventArgs e)
            {
                string txt = input.Text;
                if (string.IsNullOrEmpty(txt) && textRequired)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerState.ActiveActivityRef.Resources.GetString(Resource.String.must_type_ticker_text), ToastLength.Short);
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
                        SeekerApplication.Toaster.ShowToast(emptyTextErrorString, ToastLength.Short);
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
                UiHelpers.DoNotEnablePositiveUntilText(_dialogInstance, input);
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
                SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.removed_user), usernameInQuestion), ToastLength.Short);
                UserListService.Instance.RemoveUser(usernameInQuestion);
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
                    UiHelpers.ConfigureSpecialLinks(viewMessage, msg.MessageText, specialMessageType);
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

        public static void ShowSimpleAlertDialog(Context c, int messageResourceString, int actionResourceString)
        {

            void OnCloseClick(object sender, DialogClickEventArgs e)
            {
                (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            }

            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(c);
            //var diag = builder.SetMessage(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.about_body).TrimStart(' '), SeekerApplication.GetVersionString())).SetPositiveButton(Resource.String.close, OnCloseClick).Create();
            var diag = builder.SetMessage(messageResourceString).SetPositiveButton(actionResourceString, OnCloseClick).Create();
            diag.Show();
        }

        public static void ShowEditAddNoteDialog(string username, Action uiUpdateAction = null)
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(SeekerState.ActiveActivityRef);
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
                        CommonHelpers.SaveUserNotes();

                    }
                    else
                    {
                        //we added a note
                        SeekerState.UserNotes[username] = newText;
                        CommonHelpers.SaveUserNotes();
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
                        CommonHelpers.SaveUserNotes();
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

        public static void ShowGivePrilegesDialog(string username)
        {
            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(SeekerState.ActiveActivityRef);
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
                CommonHelpers.GivePrilegesAPI(username, input.Text);
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
    }
}
