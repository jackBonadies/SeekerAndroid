using Android.Content;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Text.Format;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Android.Util;
using System;
using Android.Graphics;
namespace AndriodApp1
{
    public static class SearchSendIntentHelper
    {
        public const string FromSearchDialogDummyActivity = "FromSearchDialogDummyActivity";
        public static bool IsFromActionSend(Intent intent)
        {
            if (intent == null)
            {
                return false;
            }
            if (intent.Action == Intent.ActionSend)
            {
                return true;
            }
            else if(intent.GetStringExtra(FromSearchDialogDummyActivity)!=null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// If the search term can be immediately and definitively resolved from the string then get it. else return false.
        /// This is stage 1.  Stage 2 is either follow a known link to get the info.  Or we dont parse it and let the user parse it.
        /// </summary>
        /// <param name="intent"></param>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        public static bool TryParseIntent(Intent intent, out string searchTerm)
        {
            try
            {
                //first try known shares
                string mainText = intent.GetStringExtra(Intent.ExtraText);
                string subject = intent.GetStringExtra(Intent.ExtraSubject);
                //music match
                //"I got the #lyrics for \"Maud Gone\" by Car Seat Headrest on Musixmatch https://www.musixmatch.com/lyrics/Car-Seat-Headrest/Maud-Gone?utm_source=application&utm_campaign=api&utm_medium=musixmatch-android%3A552993462"
                if (!string.IsNullOrEmpty(mainText) && mainText.Contains("://www.musixmatch.com/"))
                {
                    string lyric = "/lyrics/";
                    if (mainText.Contains(lyric))
                    {
                        string artistSongExtra = mainText.Substring(mainText.IndexOf(lyric) + lyric.Length);
                        string artist = artistSongExtra.Substring(0, artistSongExtra.IndexOf('/'));
                        string song = artistSongExtra.Substring(artist.Length + 1, artistSongExtra.IndexOf('?') - (artist.Length + 1));
                        searchTerm = artist.Replace('-', ' ') + ' ' + song.Replace('-', ' ');
                        return true;
                    }
                    searchTerm = null;
                    return false;
                }
                //shazam case
                //SUBJECT=Maud Gone - Car Seat Headrest
                //"I used Shazam to discover Maud Gone by Car Seat Headrest. https://www.shazam.com/track/132815415/maud-gone"
                else if (!string.IsNullOrEmpty(mainText) && mainText.Contains("://www.shazam.com/"))
                {
                    if (!string.IsNullOrEmpty(subject))
                    {
                        searchTerm = subject.Replace(" - ", " ");
                        return true;
                    }
                    //backup
                    string track = "/track/";
                    if (mainText.Contains(track))
                    {
                        searchTerm = mainText.Substring(mainText.LastIndexOf('/')).Replace('-', ' '); //song
                        return true;
                    }
                    searchTerm = null;
                    return false;
                }
                //soundhound case. 
                //android.intent.extra.SUBJECT = Check out what I just discovered on SoundHound!, android.intent.extra.TEXT = I found Maud Gone by Car Seat Headrest with SoundHound and thought you'd enjoy it too! https://soundhound.com/?t=100440846754703671
                else if (!string.IsNullOrEmpty(mainText) && mainText.Contains("://soundhound.com/"))
                {
                    //no location independent version without following the link.
                    if (mainText.Contains("with SoundHound and thought you'd enjoy it too"))
                    {
                        mainText = mainText.Replace("I found ", "");
                        int ending = mainText.IndexOf(" with SoundHound and thought you'd enjoy it too!");
                        mainText = mainText.Substring(0,ending); //Replace(" with SoundHound and thought you'd enjoy it too!", "");
                        searchTerm = mainText.Replace(" by ", " ");
                        return true;
                    }
                    else
                    {
                        searchTerm = null;
                        return false;
                    }
                }
                //youtube case
                //{android.intent.extra.SUBJECT=Watch "Title of the youtube video" on YouTube, android.intent.extra.TEXT=https://youtu.be/xyzabc}
                else if (!string.IsNullOrEmpty(mainText) && mainText.Contains("://youtu.be/"))
                {
                    if (!string.IsNullOrEmpty(subject))
                    {
                        int firstQuote = subject.IndexOf("\"");
                        int lastQuote = subject.LastIndexOf("\"");
                        searchTerm = subject.Substring(firstQuote + 1, lastQuote - (firstQuote + 1));
                        return true;
                    }
                    else
                    {
                        searchTerm = null;
                        return false;
                    }
                }
                else if (!string.IsNullOrEmpty(subject) && subject.EndsWith(" - SoundCloud"))
                {
                    searchTerm = subject.Replace(" - SoundCloud", "");
                    return true;
                }
            }
            catch(Exception ex)
            {
                MainActivity.LogFirebase("tryparseintent step1: " + ex.Message + " " + ex.StackTrace);
                searchTerm = null;
                return false;
            }
            searchTerm = null;
            return false;
        }

        /// <summary>
        /// Returns true if we are going to follow a link to get the rest of the info
        /// </summary>
        /// <returns></returns>
        public static bool FollowLinkTaskIfApplicable(Intent searchIntent)
        {
            string trackname = string.Empty;
            string artistname = string.Empty;
            bool isFollowingLink = false;
            var regex = new System.Text.RegularExpressions.Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
            string extraText = searchIntent.GetStringExtra(Intent.ExtraText);
            if (extraText == null)
            {
                if(searchIntent.Extras == null || searchIntent.Extras.KeySet() == null)
                {
                    MainActivity.LogFirebase("extras is null");
                }
                else
                {
                    string keyset = String.Join(' ',searchIntent.Extras.KeySet());
                    MainActivity.LogFirebase("extras keyset is " + keyset);
                }
            }
            var matched = regex.Match(extraText);
            bool containsLink = matched.Success;
            if (containsLink && ToLookUp(matched.Captures[0].Value))
            {
                isFollowingLink = true;
                System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() =>
                {

                    //by default:
                    //https://www.whatismybrowser.com/detect/what-is-my-user-agent
                    //User Agent: Dalvik/2.1.0 (Linux; U; Android 11; Pixel 2 Build/RP1A.201005.004.A1)
                    bool failed = false;
                    try
                    {
                        Java.Net.URL url = new Java.Net.URL(matched.Captures[0].Value);
                        Java.Net.HttpURLConnection urlConnection = (Java.Net.HttpURLConnection)(url.OpenConnection());
                        urlConnection.SetRequestProperty("Accept-Language", "en"); //tested with de. returned german for spotify..
                        var stream = (urlConnection.InputStream);
                        string fullString = new System.IO.StreamReader(stream).ReadToEnd();
                        string urlstring = matched.Captures[0].Value;
                        if (urlstring.Contains("://www.soundhound.com") || urlstring.Contains("://soundhound.com"))
                        {
                            string track_name = "\"track_name\":";
                            string artist_display_name = "\"artist_display_name\":";
                            int track_name_start_index = fullString.IndexOf(track_name) + track_name.Length + 1;
                            int track_end_index = fullString.IndexOf("\"", track_name_start_index);
                            int artist_display_name_start_index = fullString.IndexOf(artist_display_name) + artist_display_name.Length + 1;
                            int artist_display_name_end_index = fullString.IndexOf("\"", artist_display_name_start_index);
                            trackname = fullString.Substring(track_name_start_index, track_end_index - track_name_start_index);
                            artistname = fullString.Substring(artist_display_name_start_index, artist_display_name_end_index - artist_display_name_start_index);
                            trackname = AndriodApp1.HTMLUtilities.unescapeHtml(trackname);
                            artistname = AndriodApp1.HTMLUtilities.unescapeHtml(artistname);
                            //all slashes in the html body are unicode escaped \u002F (ex. track name "this \ that")
                            trackname = trackname.Replace(@"\u002F","\\",StringComparison.InvariantCultureIgnoreCase);
                            artistname = artistname.Replace(@"\u002F","\\",StringComparison.InvariantCultureIgnoreCase);
                        }
                        else if (urlstring.Contains("://open.spotify"))
                        {
                            string title_string = "<title>";
                            string song_by_string = "- song by ";
                            string end_string = " | Spotify";
                            int track_name_start_index = fullString.IndexOf(title_string) + title_string.Length;
                            int track_end_index = fullString.IndexOf(song_by_string, track_name_start_index);
                            int artist_display_name_start_index = fullString.IndexOf(song_by_string) + song_by_string.Length;
                            int artist_display_name_end_index = fullString.IndexOf(end_string, artist_display_name_start_index);
                            trackname = fullString.Substring(track_name_start_index, track_end_index - track_name_start_index - 1);
                            artistname = fullString.Substring(artist_display_name_start_index, artist_display_name_end_index - artist_display_name_start_index);
                            trackname = AndriodApp1.HTMLUtilities.unescapeHtml(trackname); //"Los Barrachos (I Don&#39;t Have Any Hope Left, But the Weather is Nice)"
                            artistname = AndriodApp1.HTMLUtilities.unescapeHtml(artistname);
                        }
                        //else if (urlstring.Contains("://soundcloud"))
                        //{
                        //
                        //}
                    }
                    catch (Exception ex)
                    {
                        MainActivity.LogFirebase("error following link: " + ex.Message + " " + ex.StackTrace);
                        failed = true;
                    }

                    if (SearchDialog.IsFollowingLink) //i.e. if it hit cancel do not update it!!!
                    {
                        SearchDialog.IsFollowingLink = false;
                        if(!failed) //if failed just keep the old search term...
                        {
                            SearchDialog.SearchTerm = artistname + " " + trackname;
                        }
                        SearchDialog.SearchTermFetched?.Invoke(null, failed);
                    }


                    //if soundhound
                    //fullString.IndexOf("\"track_name\":")
                    //fullString.IndexOf("\"artist_display_name\":")
                    //then to the next '",' or '"}'

                    //fullString.IndexOf("creator-name")
                });
                t.Start();
                //soundhound has
                //"track_name":"Maud Gone","artist_display_name":"Car Seat Headrest" (both curl and java.net.url
                //shazam is useless
                //spotify has 
                //<title>Oh! Starving - song by Car Seat Headrest | Spotify</title>
                //and is different using CURL.  so use the above regardless of what desktop curl shows.
                //soundcloud has "artist":"artist a, artist b" "title":"x"
            }
            return isFollowingLink;
        }

        /// <summary>
        /// The sites vary a lot so we only do preapproved sites
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool ToLookUp(string url)
        {
            if (url.Contains("://open.spotify") || url.Contains("://www.soundhound.com") || url.Contains("://soundhound.com"))
            {
                return true;
            }
            return false;
        }
    }


    public class SearchDialog : Android.Support.V4.App.DialogFragment
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
            if(SearchDialog.Instance!=null && SearchDialog.Instance!=this)
            {
                //we only support 1 dialog, the most recent one..
                MainActivity.LogDebug("cancelling old search dialog");
                this.Dismiss();
            }
            MainActivity.LogDebug("resuming instance: " + guid.ToString());

            SetControlState();
            base.OnResume();
            SearchTermFetched += SearchTermFetchedEventHandler;
            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
        }

        public override void OnDestroy()
        {
            MainActivity.LogDebug("OnDestroy SearchDialog");
            SearchDialog.Instance = null;
            base.OnDestroy();
        }

        private void SearchTermFetchedEventHandler(object o, bool failed)
        {
            this.Activity.RunOnUiThread(() =>
            {
                this.SetControlState();
                if(failed)
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, "Failed to parse search term from link. Contact Developer.", ToastLength.Long).Show();
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
            //Dialog.SetTitle("File Info"); //is this needed in any way??
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));

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
                if(mainText != null)
                {
                    intent.PutExtra(Intent.ExtraText,mainText);
                }
                if(subject != null)
                {
                    intent.PutExtra(Intent.ExtraSubject, subject);
                }
                MainActivity.LogDebug("SearchDialogDummyActivity launch intent");
                this.StartActivity(intent);
                this.Finish();
            }
            base.OnCreate(savedInstanceState);
        }
    }
}