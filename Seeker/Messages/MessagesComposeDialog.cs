using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Common;
using Seeker.Services;
using System;

namespace Seeker.Messages
{
    /// <summary>
    /// The "message a user" dialog shared by MessagesActivity and the shell's
    /// Messages page. Keeping it here is what stops the two hosts from copying
    /// the autocomplete/validation logic.
    /// </summary>
    public static class MessagesComposeDialog
    {
        private static AndroidX.AppCompat.App.AlertDialog messageUserDialog = null;

        public static void Show(Activity activity, Action<string> onUserSelected)
        {
            if (activity == null || onUserSelected == null)
            {
                return;
            }
            if (SessionService.Instance.IsNotLoggedIn())
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_be_logged_to_send_message), ToastLength.Short);
                return;
            }

            var builder = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(activity);
            builder.SetTitle(SeekerState.ActiveActivityRef.GetString(Resource.String.msg_user));
            var rootView = (ViewGroup)activity.FindViewById(Android.Resource.Id.Content).RootView;
            View viewInflated = LayoutInflater.From(activity).Inflate(Resource.Layout.autocomplete_user_dialog_content, rootView, false);
            AutoCompleteTextView input = (AutoCompleteTextView)viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserEditText);
            UiHelpers.SetupRecentUserAutoCompleteTextView(input);
            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs okayArgs) =>
            {
                string userToMessage = UiHelpers.GetEnteredUsername(input);
                if (userToMessage == null || userToMessage == string.Empty)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.must_type_a_username_to_message), ToastLength.Short);
                    if ((sender as AndroidX.AppCompat.App.AlertDialog) == null)
                    {
                        messageUserDialog?.Dismiss();
                    }
                    else
                    {
                        (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
                    }
                    return;
                }

                UserMetadataService.RecentUsersManager.AddUserToTop(userToMessage, true);
                onUserSelected(userToMessage);

                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    messageUserDialog?.Dismiss();
                }
            });
            EventHandler<DialogClickEventArgs> eventHandlerCancel = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    messageUserDialog?.Dismiss();
                }
            });

            var editorAction = UiHelpers.MakeDialogEditorAction(activity.FindViewById(Android.Resource.Id.Content)?.RootView, eventHandler);
            var keypressAction = UiHelpers.MakeDialogKeyPressAction(activity.FindViewById(Android.Resource.Id.Content)?.RootView, eventHandler);

            input.KeyPress += keypressAction;
            input.EditorAction += editorAction;
            input.FocusChange += UiHelpers.OnFocusAdjustNothing;

            builder.SetPositiveButton(Resource.String.okay, eventHandler);
            builder.SetNegativeButton(Resource.String.cancel, eventHandlerCancel);

            messageUserDialog = builder.Create();
            messageUserDialog.Show();
            UiHelpers.DoNotEnablePositiveUntilText(messageUserDialog, input);
        }
    }
}
