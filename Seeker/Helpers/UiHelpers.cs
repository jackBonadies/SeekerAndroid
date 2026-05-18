using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using Common;
using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Messages;
using Soulseek;
using System;
using System.Threading.Tasks;

namespace Seeker
{
    public static class UiHelpers
    {
        public static Color GetColorFromAttribute(Context c, int attr, Resources.Theme overrideTheme = null)
        {
            var typedValue = new TypedValue();
            if (overrideTheme != null)
            {
                overrideTheme.ResolveAttribute(attr, typedValue, true);
            }
            else
            {
                c.Theme.ResolveAttribute(attr, typedValue, true);
            }

            if (typedValue.ResourceId == 0)
            {
                return GetColorFromInteger(typedValue.Data);
            }
            else
            {
                return GetColorFromInteger(ContextCompat.GetColor(c, typedValue.ResourceId));
            }
        }

        public static Android.Graphics.Drawables.Drawable? GetDrawableFromAttribute(Context c, int attr)
        {
            var typedValue = new TypedValue();
            c.Theme.ResolveAttribute(attr, typedValue, true);
            int drawableRes = (typedValue.ResourceId != 0) ? typedValue.ResourceId : typedValue.Data;
            return c.Resources.GetDrawable(drawableRes, SeekerState.ActiveActivityRef.Theme);
        }

        public static Color GetColorFromInteger(int color)
        {
            return Color.Argb(Color.GetAlphaComponent(color), Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static void SetTextColor(TextView tv, Context c)
        {
            tv.SetTextColor(GetColorFromAttribute(c, Resource.Attribute.cellTextColor));
        }

        public static void SetActivityWindowSoftInputMode(SoftInput mode)
        {
            try
            {
                SeekerState.ActiveActivityRef?.Window?.SetSoftInputMode(mode);
            }
            catch (System.Exception err)
            {
                Logger.Firebase("FocusChange_SoftInputMode " + err.Message);
            }
        }

        public static SpannableStringBuilder BuildTickerSpan(RoomTicker ticker, Android.Content.Context context)
        {
            var builder = new SpannableStringBuilder();
            if (string.IsNullOrEmpty(ticker.Username))
            {
                builder.Append(ticker.Message);
                builder.SetSpan(new StyleSpan(TypefaceStyle.Italic), 0, builder.Length(), SpanTypes.InclusiveExclusive);
            }
            else
            {
                builder.Append(ticker.Message);
                var messageEnd = builder.Length();
                builder.Append(" —\u2060" + ticker.Username);
                var mutedColor = UiHelpers.GetColorFromAttribute(context, Resource.Attribute.cellTextColorSubdued);
                builder.SetSpan(new StyleSpan(TypefaceStyle.Italic), messageEnd, builder.Length(), SpanTypes.ExclusiveExclusive);
                builder.SetSpan(new ForegroundColorSpan(mutedColor), messageEnd, builder.Length(), SpanTypes.ExclusiveExclusive);
            }
            return builder;
        }


        public static void OnFocusAdjustNothing(object sender, View.FocusChangeEventArgs e)
        {
            SetActivityWindowSoftInputMode(SoftInput.AdjustNothing);
        }

        public static void OnFocusAdjustResize(object sender, View.FocusChangeEventArgs e)
        {
            SetActivityWindowSoftInputMode(SoftInput.AdjustResize);
        }

        public static void HideSoftKeyboard(View anchor)
        {
            if (anchor == null)
            {
                return;
            }
            try
            {
                var imm = (Android.Views.InputMethods.InputMethodManager)SeekerState.ActiveActivityRef?.GetSystemService(Context.InputMethodService);
                imm?.HideSoftInputFromWindow(anchor.WindowToken, 0);
            }
            catch (System.Exception ex)
            {
                Logger.Firebase(ex.Message + " error closing keyboard");
            }
        }

        public static bool IsImeCommitAction(Android.Views.InputMethods.ImeAction action)
        {
            return action == Android.Views.InputMethods.ImeAction.Done
                || action == Android.Views.InputMethods.ImeAction.Go
                || action == Android.Views.InputMethods.ImeAction.Next
                || action == Android.Views.InputMethods.ImeAction.Send
                || action == Android.Views.InputMethods.ImeAction.Search;
        }

        public static System.EventHandler<TextView.EditorActionEventArgs> MakeDialogEditorAction(
            View keyboardAnchor,
            System.EventHandler<DialogClickEventArgs> onCommit)
        {
            return (sender, e) =>
            {
                if (!IsImeCommitAction(e.ActionId))
                {
                    return;
                }
                Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                HideSoftKeyboard(keyboardAnchor);
                onCommit(sender, null);
            };
        }

        public static System.EventHandler<TextView.KeyEventArgs> MakeDialogKeyPressAction(
            View keyboardAnchor,
            System.EventHandler<DialogClickEventArgs> onCommit)
        {
            return (sender, e) =>
            {
                if (e.Event != null && e.Event.Action == KeyEventActions.Up && e.Event.KeyCode == Keycode.Enter)
                {
                    Logger.Debug("keypress: " + e.Event.KeyCode.ToString());
                    HideSoftKeyboard(keyboardAnchor);
                    onCommit(sender, null);
                }
                else
                {
                    e.Handled = false;
                }
            };
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

        public static void ShowActionSheetDialogSafe(AndroidX.Fragment.App.FragmentManager fm, Seeker.Helpers.ActionSheet.ActionSheetConfig config)
        {
            if (fm == null || fm.IsStateSaved || fm.IsDestroyed)
            {
                Logger.Firebase($"Not safe to show ActionSheetDialog - null {fm == null} {fm?.IsStateSaved} {fm?.IsDestroyed}");
                return;
            }
            Seeker.Helpers.ActionSheet.ActionSheetDialog.PendingConfig = config;
            new Seeker.Helpers.ActionSheet.ActionSheetDialog().Show(fm, "actionSheet");
        }

        public static void ShowCopyMessageTextPopup(View anchor, Message msg, GravityFlags gravity)
        {
            anchor.PerformHapticFeedback(FeedbackConstants.LongPress);

            var ctx = new AndroidX.AppCompat.View.ContextThemeWrapper(anchor.Context, Resource.Style.AppPopupOverlay);
            var popup = new AndroidX.AppCompat.Widget.PopupMenu(ctx, anchor, (int)gravity);
            popup.Inflate(Resource.Menu.message_long_press_popup);
            popup.SetForceShowIcon(true);
            popup.MenuItemClick += (s, args) =>
            {
                if (args.Item.ItemId == Resource.Id.action_copy_text)
                {
                    CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, msg.MessageText);
                }
            };
            popup.Show();
        }

        public static void SetIgnoreUnignoreTitle(IMenuItem menuItem, string username)
        {
            if (menuItem != null && !string.IsNullOrEmpty(username))
            {
                if (UserListService.Instance.IsUserInIgnoreList(username)) //if we already have added said user, change title add to remove..
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
                bool isInIgnoreList = UserListService.Instance.IsUserInIgnoreList(username);
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
                if (!IsImeCommitAction(e.ActionId))
                {
                    return;
                }
                Logger.Debug("IME ACTION: " + e.ActionId.ToString());
                HideSoftKeyboard(owner.FindViewById(Android.Resource.Id.Content)?.RootView);

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

            input.EditorAction += inputEditorAction;
            input.FocusChange += OnFocusAdjustNothing;

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
        public static bool HandleCommonContextMenuActions(string contextMenuTitle, string usernameInQuestion, Context activity, View browseSnackView, Action uiUpdateActionAdded_Removed = null, Action uiUpdateActionIgnored_Unignored = null, Action uiUpdateActionNote = null)
        {
            if (activity == null)
            {
                activity = SeekerState.ActiveActivityRef;
            }
            if (contextMenuTitle == activity.GetString(Resource.String.ignore_user))
            {
                SeekerApplication.AddToIgnoreListFeedback(activity, usernameInQuestion);
                if (uiUpdateActionIgnored_Unignored != null)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                }
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_ignored))
            {
                SeekerApplication.RemoveFromIgnoreListFeedback(activity, usernameInQuestion);
                if (uiUpdateActionIgnored_Unignored != null)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionIgnored_Unignored);
                }
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
                UserListService.AddUserAPI(SeekerState.ActiveActivityRef, usernameInQuestion, uiUpdateActionAdded_Removed);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_from_user_list) ||
                contextMenuTitle == activity.GetString(Resource.String.remove_user))
            {
                SeekerApplication.Toaster.ShowToast(string.Format(SeekerState.ActiveActivityRef.GetString(Resource.String.removed_user), usernameInQuestion), ToastLength.Short);
                UserListService.Instance.RemoveUser(usernameInQuestion);
                if (uiUpdateActionAdded_Removed != null)
                {
                    SeekerState.ActiveActivityRef.RunOnUiThread(uiUpdateActionAdded_Removed);
                }
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.search_user_files))
            {
                SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                SearchTabHelper.SearchTargetChosenUser = usernameInQuestion;
                //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                Intent intent = new Intent(activity, typeof(MainActivity));
                intent.PutExtra(MainActivity.GoToSearchExtra, true);
                intent.AddFlags(ActivityFlags.SingleTop); //??
                activity.StartActivity(intent);
                return true;
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.browse_user))
            {
                BrowseService.RequestFilesApi(usernameInQuestion, null);
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
                UserListService.RaiseUserRowChanged(usernameInQuestion);
            }
            else if (contextMenuTitle == activity.GetString(Resource.String.remove_online_alert))
            {
                SeekerState.UserOnlineAlerts.TryRemove(usernameInQuestion, out _);
                CommonHelpers.SaveOnlineAlerts();
                UserListService.RaiseUserRowChanged(usernameInQuestion);
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
                    UserListService.RaiseUserRowChanged(username);
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
                        UserListService.RaiseUserRowChanged(username);
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

            var editorAction = MakeDialogEditorAction(SeekerState.ActiveActivityRef?.Window?.DecorView, eventHandler);

            input.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.send, eventHandler);
            builder.SetNegativeButton(Resource.String.close, eventHandlerCancel);
            // Set up the buttons

            builder.Show();
        }

        public static void SetActivityTheme(Activity a)
        {
            //useless returns the same thing every time
            //int curTheme = a.PackageManager.GetActivityInfo(a.ComponentName, 0).ThemeResource;
            if (a.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes))
            {
                a.SetTheme(ThemeHelper.ToNightThemeProper(PreferencesState.NightModeVariant));
            }
            else
            {
                a.SetTheme(ThemeHelper.ToDayThemeProper(PreferencesState.DayModeVariant));
            }
        }

        public static View GetViewForSnackbar()
        {
            bool useDownloadDialogFragment = false;
            View v = null;
            if (SeekerState.ActiveActivityRef is MainActivity mar)
            {
                var f = mar.SupportFragmentManager.FindFragmentByTag(DownloadDialog.DOWNLOAD_DIALOG_FRAGMENT);
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

        public static void SetupRecentUserAutoCompleteTextView(AutoCompleteTextView actv, bool forAddingUser = false)
        {
            if (PreferencesState.ShowRecentUsers)
            {
                if (forAddingUser)
                {
                    //dont show people that we have already added...
                    var recents = SeekerState.RecentUsersManager.GetRecentUserList();
                    lock (CommonState.UserList)
                    {
                        foreach (var uli in CommonState.UserList)
                        {
                            recents.Remove(uli.Username);
                        }
                    }
                    actv.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Android.Resource.Layout.SimpleDropDownItem1Line, recents);
                }
                else
                {
                    actv.Adapter = new ArrayAdapter<string>(SeekerState.ActiveActivityRef, Android.Resource.Layout.SimpleDropDownItem1Line, SeekerState.RecentUsersManager.GetRecentUserList());
                }
            }
        }
    }
}
