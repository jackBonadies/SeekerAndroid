using Seeker.Helpers;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
namespace Seeker
{
    public class SearchDialog : AndroidX.Fragment.App.DialogFragment
    {

        public static EventHandler<bool> SearchTermFetched;

        public static volatile string SearchTerm = string.Empty;
        public static volatile bool IsFollowingLink = false;

        private Guid guid = Guid.NewGuid();

        public SearchDialog(string searchTerm, bool isFollowingLink)
        {
            SearchTerm = searchTerm;
            IsFollowingLink = isFollowingLink;
            SearchDialog.Instance = this;
        }
        public SearchDialog()
        {

        }

        private void SetControlState()
        {
            var editText = this.View.FindViewById<EditText>(Resource.Id.editText);
            ViewGroup followingLinkLayout = this.View.FindViewById<ViewGroup>(Resource.Id.followingLinkLayout);
            //ProgressBar followingLinkBar = this.View.FindViewById<ProgressBar>(Resource.Id.progressBarFollowingLink);
            editText.Text = SearchTerm;
            if (IsFollowingLink)
            {
                editText.Enabled = false;
                editText.Clickable = false;
                editText.Focusable = false;
                editText.FocusableInTouchMode = false;
                editText.SetCursorVisible(false);
                editText.Alpha = 0.8f;
                followingLinkLayout.Visibility = ViewStates.Visible;
            }
            else
            {
                editText.Enabled = true;
                editText.Clickable = true;
                editText.Focusable = true;
                editText.FocusableInTouchMode = true;
                editText.SetCursorVisible(true);
                editText.Alpha = 1.0f;
                followingLinkLayout.Visibility = ViewStates.Gone;
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            SearchTermFetched -= SearchTermFetchedEventHandler;
        }

        public static SearchDialog Instance = null;

        public override void OnResume()
        {
            if (SearchDialog.Instance != null && SearchDialog.Instance != this)
            {
                //we only support 1 dialog, the most recent one..
                Logger.Debug("cancelling old search dialog");
                this.Dismiss();
            }
            Logger.Debug("resuming instance: " + guid.ToString());

            SetControlState();
            base.OnResume();
            SearchTermFetched += SearchTermFetchedEventHandler;

            Dialog?.SetSizeProportional(.9, -1);
        }

        public override void OnDestroy()
        {
            Logger.Debug("OnDestroy SearchDialog");
            SearchDialog.Instance = null;
            base.OnDestroy();
        }

        private void SearchTermFetchedEventHandler(object o, bool failed)
        {
            this.Activity.RunOnUiThread(() =>
            {
                this.SetControlState();
                if (failed)
                {
                    SeekerApplication.Toaster.ShowToast("Failed to parse search term from link. Contact Developer.", ToastLength.Long);
                }
            }
            );
        }

        private void SetupEventHandlers()
        {
            View Cancel = this.View.FindViewById<View>(Resource.Id.textViewCancel);
            Cancel.Click += Cancel_Click;

            //todo search and cancel / close.
            Button closeButton = this.View.FindViewById<Button>(Resource.Id.searchCloseButton);
            closeButton.Click += CloseButton_Click;

            Button searchButton = this.View.FindViewById<Button>(Resource.Id.searchButton);
            searchButton.Click += SearchButton_Click; ;
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            var editText = this.View.FindViewById<EditText>(Resource.Id.editText);
            SearchFragment.PerformSearchLogicFromSearchDialog(editText.Text);
            IsFollowingLink = false;
            SearchTerm = null;
            this.Dismiss();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            IsFollowingLink = false;
            SearchTerm = null;
            this.Dismiss();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            IsFollowingLink = false;
            SetControlState();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.search_intent_dialog, container); //container is parent
        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SeekerState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));

            this.SetStyle((int)DialogFragmentStyle.Normal, 0);
            //this.Dialog.SetTitle(OurRoomName);

            //listViewTickers = view.FindViewById<ListView>(Resource.Id.listViewTickers);
            SetupEventHandlers();


        }
    }

    /**
    This is a dummy activity used to solve the following problem.

    By default when launch the action send intent, the activity will be launched in a new task.
    For example, in Spotify, share, Seeker will have a new task.  
    Now before this (1) we ALWAYS had just 1 task (so potential for new bugs and 
    (2) having multiple tasks seems messy, if the user does the feature 10 times, they get 10 tasks in their recent tasks.
    One solution to this is to set LaunchMode = SingleTask on the MainActivity.  But that has the side effect that
    it changes the behavior for other things i.e. open MainActivity > Open Chatrooms > Users in Room > Search User Files >
    press back, now instead of going back to Users in Room the activity gets finished.
    This fix solves the issue without the unintented changes of making MainActivity a SingleTask activity.

    **/
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = true)]
    [IntentFilter(new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain", Label = "Search Here")]
    public class SearchDialogDummyActivity : ThemeableActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            if (Intent != null && SearchSendIntentHelper.IsFromActionSend(Intent))
            {
                Intent intent = new Intent(this, typeof(MainActivity));
                intent.PutExtra(SearchSendIntentHelper.FromSearchDialogDummyActivity, SearchSendIntentHelper.FromSearchDialogDummyActivity);
                string mainText = Intent.GetStringExtra(Intent.ExtraText);
                string subject = Intent.GetStringExtra(Intent.ExtraSubject);
                if (mainText != null)
                {
                    intent.PutExtra(Intent.ExtraText, mainText);
                }
                if (subject != null)
                {
                    intent.PutExtra(Intent.ExtraSubject, subject);
                }
                Logger.Debug("SearchDialogDummyActivity launch intent");
                this.StartActivity(intent);
                this.Finish();
            }
            base.OnCreate(savedInstanceState);
        }
    }
}