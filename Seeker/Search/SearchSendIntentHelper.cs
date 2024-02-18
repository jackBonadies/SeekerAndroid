using Android.Content;
using System;

namespace Seeker.Helpers
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
            else if (intent.GetStringExtra(FromSearchDialogDummyActivity) != null)
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
                //"I got the #lyrics for \"song\" by artist on Musixmatch https://www.musixmatch.com/lyrics/Car-Sleep-Bedtest/Mad-Gone?utm_source=application&utm_campaign=api&utm_medium=musixmatch-android%3A552993462"
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
                //SUBJECT=Mad Gone - Car Sleep BedRest
                //"I used Shazam to discover Mad Gone by Car Sleep BedRest. https://www.shazam.com/track/132815415/mad-gone"
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
                //android.intent.extra.SUBJECT = Check out what I just discovered on SoundHound!, android.intent.extra.TEXT = I found Mad Gone by Car Sleep BedRest with SoundHound and thought you'd enjoy it too! https://soundhound.com/?t=100440846754703671
                else if (!string.IsNullOrEmpty(mainText) && mainText.Contains("://soundhound.com/"))
                {
                    //no location independent version without following the link.
                    if (mainText.Contains("with SoundHound and thought you'd enjoy it too"))
                    {
                        mainText = mainText.Replace("I found ", "");
                        int ending = mainText.IndexOf(" with SoundHound and thought you'd enjoy it too!");
                        mainText = mainText.Substring(0, ending); //Replace(" with SoundHound and thought you'd enjoy it too!", "");
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
            catch (Exception ex)
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
                if (searchIntent.Extras == null || searchIntent.Extras.KeySet() == null)
                {
                    MainActivity.LogFirebase("extras is null");
                }
                else
                {
                    string keyset = String.Join(' ', searchIntent.Extras.KeySet());
                    MainActivity.LogFirebase("extras keyset is " + keyset);
                }
                return false; //bc empty.
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
                    string urlstring = String.Empty;
                    try
                    {
                        Java.Net.URL url = new Java.Net.URL(matched.Captures[0].Value);
                        Java.Net.HttpURLConnection urlConnection = (Java.Net.HttpURLConnection)(url.OpenConnection());
                        urlConnection.SetRequestProperty("Accept-Language", "en"); //tested with de. returned german for spotify..
                        var stream = (urlConnection.InputStream);
                        string fullString = new System.IO.StreamReader(stream).ReadToEnd();
                        urlstring = matched.Captures[0].Value;
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
                            trackname = Seeker.HTMLUtilities.unescapeHtml(trackname);
                            artistname = Seeker.HTMLUtilities.unescapeHtml(artistname);
                            //all slashes in the html body are unicode escaped \u002F (ex. track name "this \ that")
                            trackname = trackname.Replace(@"\u002F", "\\", StringComparison.InvariantCultureIgnoreCase);
                            artistname = artistname.Replace(@"\u002F", "\\", StringComparison.InvariantCultureIgnoreCase);
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
                            trackname = Seeker.HTMLUtilities.unescapeHtml(trackname); //"(I Don&#39;t)"
                            artistname = Seeker.HTMLUtilities.unescapeHtml(artistname);
                        }
                        //else if (urlstring.Contains("://soundcloud"))
                        //{
                        //
                        //}
                    }
                    catch (Exception ex)
                    {
                        MainActivity.LogFirebase("error following link: " + urlstring + ex.Message + " " + ex.StackTrace);
                        failed = true;
                    }

                    if (SearchDialog.IsFollowingLink) //i.e. if it hit cancel do not update it!!!
                    {
                        SearchDialog.IsFollowingLink = false;
                        if (!failed) //if failed just keep the old search term...
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
                //"track_name":"x","artist_display_name":"x" (both curl and java.net.url
                //shazam is useless
                //spotify has 
                //<title>Oops! Hungry - song by Car Sleep BedRest | Spotify</title>
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
}