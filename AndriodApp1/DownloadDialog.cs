/*
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Drm;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Work;
using Google.Android.Material.Snackbar;
using Soulseek;

using log = Android.Util.Log;

namespace AndriodApp1
{
    class DownloadDialog : Android.Support.V4.App.DialogFragment, PopupMenu.IOnMenuItemClickListener
    {
        private int searchPosition = -1;
        private SearchResponse searchResponse = null;
        DownloadCustomAdapter customAdapter = null;
        static SearchResponse SearchResponseTemp = null; //These are for when the DownloadDialog gets recreated by the system.
        static int SearchPositionTemp = -1; //The system NEEDS a default fragment constructor to call. So we re-
        //private bool diagFirstTime = true;                                                 //use these arguments.
        private Activity activity = null;
        //private List<int> selectedPositions = new List<int>();
        public DownloadDialog(int pos, SearchResponse resp)
        {
            MainActivity.LogDebug("DownloadDialog create");
            searchResponse = resp;
            searchPosition = pos;
            SearchResponseTemp = resp;
            SearchPositionTemp = pos;
        }

        public DownloadDialog()
        {
            MainActivity.LogDebug("DownloadDialog create (default constructor)"); //this gets called on recreate i.e. phone tilt, etc.
            searchResponse = SearchResponseTemp;
            searchPosition = SearchPositionTemp;
        }

        private void UpdateSearchResponseWithFullDirectory(Soulseek.Directory d)
        {
            //normally files are like this "@@ynkmv\\Albums\\Lil Wayne - Dedication 4 (2012)\\02 - Same Damn Tune.mp3"
            //but when we get a dir response the files are just the end file names i.e. "02 - Same Damn Tune.mp3" so they cannot be downloaded like that...
            //can be fixed with d.Name + "\\" + f.Filename
            //they also do not come with any attributes.. , just the filenames (and sizes) you need if you want to download them...
            List<File> fullFilenameCollection = new List<File>();
            foreach(File f in d.Files)
            {
                string fName = d.Name + "\\" + f.Filename;
                bool extraAttr = false;
                //if it existed in the old folder then we can get some extra attributes
                foreach (File fullFileInfo in searchResponse.Files)
                {
                    if(fName==fullFileInfo.Filename)
                    {
                        fullFilenameCollection.Add(new File(f.Code, fName, f.Size, f.Extension, fullFileInfo.Attributes));
                        extraAttr = true;
                        break;
                    }
                }
                if(!extraAttr)
                {
                    fullFilenameCollection.Add(new File(f.Code, fName, f.Size,f.Extension,f.Attributes));
                }
            }
            SearchResponseTemp = searchResponse = new SearchResponse(searchResponse.Username,searchResponse.Token,searchResponse.FreeUploadSlots,searchResponse.UploadSpeed,searchResponse.QueueLength, fullFilenameCollection);
        }

        public override void OnResume()
        {
            base.OnResume();
            MainActivity.LogDebug("OnResume Start");

            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
            MainActivity.LogDebug("OnResume End");
        }

        public override void OnAttach(Context context)
        {
            MainActivity.LogDebug("DownloadDialog OnAttach");
            base.OnAttach(context);
            if(context is Activity)
            {
                this.activity = context as Activity;
            }
            else
            {
                throw new Exception("Custom");
            }
        }

        public override void OnDetach()
        {
            MainActivity.LogDebug("DownloadDialog OnDetach");
            SearchFragment.dlDialogShown = false;
            base.OnDetach();
            this.activity = null;
        }

        public override void OnDestroy()
        {
            SearchFragment.dlDialogShown = false;
            base.OnDestroy();
            this.activity = null;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            SearchFragment.dlDialogShown = false;
        }

        public static DownloadDialog CreateNewInstance(int pos, SearchResponse resp)
        {
            DownloadDialog downloadDialog = new DownloadDialog(pos, resp);
            return downloadDialog;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            MainActivity.LogDebug("DownloadDialog OnCreateView");
            return inflater.Inflate(Resource.Layout.downloaddialog,container); //container is parent
        }


        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            log.Debug(MainActivity.logCatTag, "Is View null: " + (view==null).ToString());

            log.Debug(MainActivity.logCatTag, "Is savedInstanceState null: " + (savedInstanceState == null).ToString()); //this is null and it is fine..
            base.OnViewCreated(view, savedInstanceState);
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));

            //Dialog.SetTitle("File Info"); //is this needed in any way??

            this.SetStyle((int)DialogFragmentStyle.NoTitle,0);
            Button dl = view.FindViewById<Button>(Resource.Id.buttonDownload);
            log.Debug(MainActivity.logCatTag, "Is dl null: " + (dl == null).ToString());
            dl.Click += DlAll_Click;
            Button cancel = view.FindViewById<Button>(Resource.Id.buttonCancel);
            cancel.Click += Cancel_Click;
            Button dlSelected = view.FindViewById<Button>(Resource.Id.buttonDownloadSelected);
            dlSelected.Click += DlSelected_Click;
            Button reqFiles = view.FindViewById<Button>(Resource.Id.buttonRequestDirectories);
            reqFiles.Click += ReqFiles_Click;
            //selectedPositions.Clear();
            TextView userHeader = view.FindViewById<TextView>(Resource.Id.userHeader);
            TextView subHeader = view.FindViewById<TextView>(Resource.Id.userHeaderSub);



            ViewGroup headerLayout = view.FindViewById<ViewGroup>(Resource.Id.header1);
            
            if(searchResponse == null)
            {
                log.Debug(MainActivity.logCatTag, "Is searchResponse null");
                MainActivity.LogFirebase("DownloadDialog search response is null");
                this.Dismiss(); //this is honestly pretty good behavior...
                return;
            }
            userHeader.Text = "User: " + searchResponse.Username;
            subHeader.Text = "Total: " + Helpers.GetSubHeaderText(searchResponse);
            headerLayout.Click += UserHeader_Click;
            log.Debug(MainActivity.logCatTag, "Is searchResponse.Files null: " + (searchResponse.Files == null).ToString());

            ListView listView = view.FindViewById<ListView>(Resource.Id.listView1);
            listView.ItemClick += ListView_ItemClick;
            listView.ChoiceMode = ChoiceMode.Multiple;
            UpdateListView();
        }

        private void UpdateListView()
        {
            ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
            this.customAdapter = new DownloadCustomAdapter(SoulSeekState.MainActivityRef, searchResponse.Files.ToList());
            this.customAdapter.Owner = this;
            listView.Adapter = (customAdapter);
        }

        private void UpdateSubHeader()
        {
            TextView subHeader = this.View.FindViewById<TextView>(Resource.Id.userHeaderSub);
            subHeader.Text = "Total: " + Helpers.GetSubHeaderText(searchResponse);
        }

        private void UserHeader_Click(object sender, EventArgs e)
        {
            try
            {
                PopupMenu popup = new PopupMenu(SoulSeekState.MainActivityRef, sender as View,GravityFlags.Right);
                popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
                popup.Inflate(Resource.Menu.download_diag_options);
                popup.Show();
            }
            catch (System.Exception error)
            {
                //in response to a crash android.view.WindowManager.BadTokenException
                //This crash is usually caused by your app trying to display a dialog using a previously-finished Activity as a context.
                //in this case not showing it is probably best... as opposed to a crash...
                MainActivity.LogFirebase(error.Message + " POPUP BAD ERROR");
            }
        }

        private static void RequestFilesLogic(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation)
        {
            try
            {
                bool useDownloadDialogFragmentPre = false;
                View vPre = null;
                if (SoulSeekState.ActiveActivityRef is MainActivity)
                {
                    var f = (SoulSeekState.ActiveActivityRef as MainActivity).SupportFragmentManager.FindFragmentByTag("tag_download_test");
                    if (f != null && f.IsVisible)
                    {
                        useDownloadDialogFragmentPre = true;
                        vPre = f.View;
                    }
                }
                if(!useDownloadDialogFragmentPre)
                {
                    vPre = SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                }

                Snackbar.Make(vPre, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_contacting), Snackbar.LengthShort).Show();
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("RequestFilesLogic: " + e.Message + e.StackTrace);
            }
            Task<BrowseResponse> browseResponseTask = null;
            try
            {
                browseResponseTask = SoulSeekState.SoulseekClient.BrowseAsync(username);
            }
            catch(InvalidOperationException)
            {   //this can still happen on ReqFiles_Click.. maybe for the first check we were logged in but for the second we somehow were not..
                SoulSeekState.MainActivityRef.RunOnUiThread(()=>{Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show(); });
                return;
            }
            Action<Task<BrowseResponse>> continueWithAction = new Action<Task<BrowseResponse>>((br) => {
                //var arrayOfDir = br.Result.Directories.ToArray();
                //for(int i=0;i<arrayOfDir.Length;i++)
                //{
                //    Console.WriteLine(arrayOfDir[i].DirectoryName);
                //    Console.WriteLine(arrayOfDir[i].FileCount);
                //    if(i>100)
                //    {
                //        break;
                //    }
                //}
                //Console.WriteLine(arrayOfDir.ToString());
                if (br.IsFaulted && br.Exception?.InnerException is TimeoutException)
                {
                    //timeout
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_timeout), ToastLength.Short).Show(); });
                    return;
                }
                else if(br.IsFaulted && br.Exception?.InnerException != null && br.Exception.InnerException.Message.ToLower().Contains("failed to establish a direct or indirect"))
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_user_nodirectconnection), ToastLength.Short).Show(); });
                    return;
                }
                else if (br.IsFaulted)
                {
                    //shouldnt get here
                }
                //TODO there is a case due to like button mashing or if you keep requesting idk. but its a SoulseekClient InnerException and it says peer disconnected unexpectedly and timeout.

                //List<string> terms = new List<string>();
                //terms.Add("Collective");
                string errorString = string.Empty;
                var tree = CreateTree(br.Result,false, null, null, username, out errorString);
                if(tree!=null)
                {
                    SoulSeekState.OnBrowseResponseReceived(br.Result, tree, username, atLocation);
                }

                SoulSeekState.MainActivityRef.RunOnUiThread(()=> {
                    if(tree==null)
                    {
                        //error case
                        if(errorString != null && errorString!=string.Empty)
                        {
                            Toast.MakeText(SoulSeekState.MainActivityRef, errorString, ToastLength.Long).Show();
                        }
                        else
                        {
                            Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.browse_user_wefailedtoparse), ToastLength.Long).Show();
                        }
                        return;
                    }
                    if(((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).CurrentItem == 3) //AND it is our current activity...
                    {
                        if(SoulSeekState.MainActivityRef.Lifecycle.CurrentState.IsAtLeast(Android.Arch.Lifecycle.Lifecycle.State.Started))
                        {
                            return; //they are already there... they see it populating, no need to show them notification...
                        }
                    }

                    //TODO temp
                    bool useDownloadDialogFragment = false;
                    View v = null;
                    if(SoulSeekState.ActiveActivityRef is MainActivity mar)
                    {
                        var f = mar.SupportFragmentManager.FindFragmentByTag("tag_download_test"); 
                        //this is the only one we have..  tho obv a more generic way would be to see if s/t is a dialog fragmnet.  but arent a lot of just simple alert dialogs etc dialog fragment?? maybe explicitly checking is the best way.
                        if(f != null && f.IsVisible)
                        {
                            useDownloadDialogFragment = true;
                            v = f.View;
                        }
                    }


                    Action<View> action = new Action<View>((v) => {
                        Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        SoulSeekState.ActiveActivityRef.StartActivity(intent);
                        //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    //Snackbar sb = Snackbar.Make(this.View, "Browse Response Received", Snackbar.LengthLong).SetAction("Go", action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                    //Snackbar sb = Snackbar.Make(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.content), "Browse Response Received", Snackbar.LengthLong).SetAction("Go", action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                    try
                    {
                        if(!useDownloadDialogFragment)
                        {
                            v = SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                        }
                        Snackbar sb = Snackbar.Make(v, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SoulSeekState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                        (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                        sb.Show(); 
                    }
                    catch
                    {
                        try
                        {
                            Snackbar sb = Snackbar.Make(SoulSeekState.MainActivityRef.CurrentFocus, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), Snackbar.LengthLong).SetAction(SoulSeekState.ActiveActivityRef.GetString(Resource.String.go), action).SetActionTextColor(Resource.Color.lightPurpleNotTransparent);
                            (sb.View.FindViewById<TextView>(Resource.Id.snackbar_action) as TextView).SetTextColor(Android.Graphics.Color.ParseColor("#BCC1F7"));//AndroidX.Core.Content.ContextCompat.GetColor(this.Context,Resource.Color.lightPurpleNotTransparent));
                            sb.Show();
                        }
                        catch
                        {
                            Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_response_received), ToastLength.Short).Show();
                        }
                    }
                    
                    
                    });
            });
            browseResponseTask.ContinueWith(continueWithAction);
        }

        private void ReqFiles_Click(object sender, EventArgs e)
        {
            Action<View> action = new Action<View>((v) => {
                this.Dismiss();
                ((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
            });
            RequestFilesApi(searchResponse.Username, this.View, action, null);
        }


        public static void RequestFilesApi(string username, View viewForSnackBar, Action<View> goSnackBarAction, string atLocation=null)
        {
            if (!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.must_be_logged_to_browse), ToastLength.Short).Show();
                return;
            }
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.MainActivityRef, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(()=>{RequestFilesLogic(username, viewForSnackBar, goSnackBarAction, atLocation); }));
                }));
            }
            else
            {
                RequestFilesLogic(username, viewForSnackBar, goSnackBarAction, atLocation);
            }
        }

        public static Directory FilterDirectory(Directory d, List<string> wordsToAvoid, List<string> wordsToInclude)
        {
            if(d.FileCount==0)
            {
                return d;
            }
            List<File> files = new List<File>();
            string fullyQualDirName = d.Name;
            foreach(File f in d.Files)
            {
                string fullName = fullyQualDirName + f.Filename;

                bool badTerm = false;
                if(wordsToAvoid!=null)
                {
                    foreach (string avoid in wordsToAvoid)
                    {
                        if (fullName.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                        {
                            //return false;
                            badTerm = true;
                        }
                    }
                }
                if(badTerm)
                {
                    continue; //i.e. its not going to be included..
                }
                bool includesAll = true;
                if(wordsToInclude!=null)
                {
                    foreach (string include in wordsToInclude)
                    {
                        if (!fullName.Contains(include, StringComparison.OrdinalIgnoreCase))
                        {
                            includesAll = false;
                            break;
                        }
                    }
                }
                if(includesAll)
                {
                    files.Add(f);
                }
            }
            return new Directory(d.Name,files);
        }

        private static string GetLongestBeginningSubstring(string a, string b)
        {
            int maxLen = Math.Min(a.Length,b.Length);
            int maxIndexInCommon = 0;
            for(int i=0;i<maxLen;i++)
            {
                if(a[i]==b[i])
                {
                    maxIndexInCommon++;
                }
                else
                {
                    break;
                }
            }
            return a.Substring(0,maxIndexInCommon);//this can be empty..
        }

        public static string GetLongestCommonParent(string a, string b)
        {
            if (!a.Contains('\\') || !b.Contains('\\'))
            {
                return string.Empty;
            }
            string allOtherThanCurrentDirA = a.Substring(0, a.LastIndexOf('\\') + 1);
            string allOtherThanCurrentDirB = b.Substring(0, b.LastIndexOf('\\') + 1);
            int maxLen = Math.Min(allOtherThanCurrentDirA.Length, allOtherThanCurrentDirB.Length);
            int maxIndexInCommon = 0;
            for (int i = 0; i < maxLen; i++)
            {
                if (a[i] == b[i])
                {
                    maxIndexInCommon++;
                }
                else
                {
                    break;
                }
            }
            string potential = a.Substring(0, maxIndexInCommon);
            int lastIndex = potential.LastIndexOf('\\');
            if (lastIndex == -1)
            {
                return string.Empty;
            }
            else
            {
                return potential.Substring(0, lastIndex);
            }
        }

        public static TreeNode<Directory> CreateTree(BrowseResponse b, bool filter, List<string> wordsToAvoid, List<string> wordsToInclude, string username, out string errorMsgToToast)
        {
            //logging code for unit tests / diagnostic.. //TODO comment out always
            //var root = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef , Android.Net.Uri.Parse( SoulSeekState.SaveDataDirectoryUri) );
            //DocumentFile exists = root.FindFile(username + "_dir_response");
            ////save:
            //if(exists==null || !exists.Exists())
            //{
            //    DocumentFile f = root.CreateFile(@"custom\binary",username + "_dir_response");
            //
            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
            //    //Java.IO.File musicFile = new Java.IO.File(filePath);
            //    //FileOutputStream stream = new FileOutputStream(mFile);
            //    using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
            //    {
            //        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //        formatter.Serialize(userListStream, b);
            //
            //    //write to binary..
            //
            //        stream.Write(userListStream.ToArray());
            //        stream.Close();
            //    }
            //}
            //load
            //string username_to_load = "Cyborg_Master";
            //exists = root.FindFile(username_to_load + "_dir_response");
            //var str = SoulSeekState.ActiveActivityRef.ContentResolver.OpenInputStream(exists.Uri);

            //System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //b = formatter.Deserialize(str) as BrowseResponse;

            ////write to binary..

            //str.Close();
            //end logging code

            if (b.DirectoryCount==0&&b.LockedDirectoryCount!=0)
            {
                errorMsgToToast = SoulSeekState.MainActivityRef.GetString(Resource.String.browse_onlylocked);
                return null;
            }
            else if(b.DirectoryCount==0&&b.LockedDirectoryCount==0)
            {
                errorMsgToToast = SoulSeekState.MainActivityRef.GetString(Resource.String.browse_none);
                return null;
            }

            TreeNode<Directory> rootNode = null;
            try
            {
                String prevDirName = string.Empty;
                //TreeNode<Directory> rootNode = null;
                TreeNode<Directory> curNode = null;

                TreeNode<Directory> prevNodeDebug = null;
                
                var dirArray = b.Directories.ToArray();
                //TODO I think moving this out of a lambda would make it faster, but need to do unit tests first!
                //StringComparer alphabetComparer = StringComparer.Create(new System.Globalization.CultureInfo("en-US"), true); //else 'a' is 26 behind 'A'
                Array.Sort(dirArray,(x,y) =>
                {
                    int len1 = x.Name.Count();
                    int len2 = y.Name.Count();
                    int len = Math.Min(len1, len2);
                    for(int i=0;i<len;i++)
                    {
                        char cx = x.Name[i];
                        char cy = y.Name[i];
                        if(cx=='\\'||cy=='\\')
                        {
                            if(cx == '\\' && cy != '\\')
                            {
                                return -1;
                            }
                            if(cx != '\\' && cy == '\\')
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            //int comp = System.String.Compare(x.Name, i, y.Name, i , 1);
                            int comp = char.ToLowerInvariant(cx).CompareTo(char.ToLowerInvariant(cy));
                            if(comp!=0)
                            {
                                return comp;
                            }
                        }
                    }
                    return len1 - len2;
                }
                ); //sometimes i dont quite think they are sorted.
                //sorting alphabetically seems weird, but since it puts shorter strings in front of longer ones, it does actually sort the highest parent 1st, etc.

                //sorting fails as it does not consider \\ higher than other chars, etc.
                //Music
                //music 3
                //Music\test
                //fixed with custom comparer



                //normally peoples files look like 
                //@@datd\complete
                //@@datd\complete\1990
                //@@datd\complete\1990\test
                //but sometimes they do not have a common parent! they are not a tree but many different trees (which soulseek allows)
                //in that case we need to make a common root, as the directory everyone has (even if its the fake "@@adfadf" directory NOT TRUE)
                //I think a quick hack would be.. is the first directory name contained in the last directory name

                //mzawk case (This is SoulseekQT Im guessing)
                //@@bvenl\0
                //@@bvenl\1
                //@@bvenl\2
                //@@bvenl\2\complete
                //@@bvenl\2\complete\1990
                //@@bvenl\2\complete\1990\test


                //meee
                //@@pulvh\FLAC Library
                //@@pulvh\Old School

                //BeerNecessities (This is Nicotine multi-root Im guessing)
                //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\Billy Idol [video collection]"
                //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\Nina Hagen - Video Collection"
                //__INTERNAL_ERROR__P:\\My Videos\\++Music SD++\\The Beatles - 1+ (all 27 tracks with 5.1 surround audio)"
                //FLAC"
                //FLAC\..."...
                //FLAC\\++Various Artists++\\VA - Winters Of Discontent - The Peel Sessions 77-83 (1991)"
                //NOTE THERE IS NO FAKE @@lskjdf
                //sometimes the root is the empty string

                //doggoli - the first is literally just '\\' not an actual directory name... (old PowerPC Mac version??)
                //\\
                //\\Volumes
                //...
                //"\\Volumes\\Music\\**Artist**"
                //or 
                //adfzdg\\  (Note this should be adfzdg)...
                //adfzdg\\Music
                //I think this would be a special case where we simply remove the first dir.
                if(dirArray[0].Name=="\\")
                {
                    dirArray = dirArray.Skip(1).ToArray();
                }
                else if(dirArray[0].Name.EndsWith("\\"))
                {
                    dirArray[0] = new Directory(dirArray[0].Name.Substring(0, dirArray[0].Name.Length-1),dirArray[0].Files);
                }



                bool emptyRoot = false;
                //if(dirArray[dirArray.Length-1].Name.Contains(dirArray[0].Name))
                if(Helpers.IsChildDirString(dirArray[dirArray.Length-1].Name,dirArray[0].Name, true) || dirArray[dirArray.Length - 1].Name.Equals(dirArray[0].Name))
                {
                    //normal single tree case..
                }
                else
                {
                    //we need to set the first root..
                    //GetLongestCommonParent(dirArray[dirArray.Length - 1].Name, dirArray[0].Name);
                    string newRootDirName = GetLongestCommonParent(dirArray[dirArray.Length - 1].Name, dirArray[0].Name);
                    if (newRootDirName==string.Empty)
                    {
                        //MainActivity.LogFirebase("Root is the empty string: " + username); //this is fine
                        newRootDirName = "";
                        emptyRoot = true;
                    }
                    //if(newRootDirName.EndsWith("\\"))
                    //{
                    //    newRootDirName = newRootDirName.Substring(0, newRootDirName.Length-1);
                    //    //else our new folder root will be "@@sdfklj\\" rather than "@@sdfklj" causing problems..
                    //}
                    //the rootname can be "@@sdfklj\\! " if the directories are "@@sdfklj\\! mp3", "@@sdfklj\\! flac"
                    if(newRootDirName.LastIndexOf("\\")!=-1)
                    {
                        newRootDirName = newRootDirName.Substring(0, newRootDirName.LastIndexOf("\\"));
                        //else our new folder root will be "@@sdfklj\\" rather than "@@sdfklj" causing problems..
                    }
                    Directory rootDirectory = new Directory(newRootDirName);

                    //kickstart things
                    rootNode = new TreeNode<Directory>(rootDirectory);
                    prevDirName = newRootDirName;
                    curNode = rootNode;
                }



                foreach (Directory d in dirArray)
                {
                    if(prevDirName == string.Empty && !emptyRoot) //this means that you did not set anything. sometimes the root literally IS empty.. see BeerNecessities
                    {
                        rootNode = new TreeNode<Directory>(d);
                        curNode = rootNode;
                        prevDirName = d.Name;
                    }
                    else if(Helpers.IsChildDirString(d.Name,prevDirName, curNode?.Parent == null)) //if the next directory contains the previous in its path then it is a child. //this is not true... it will set music as the child of mu //TODO !!!!!
                    {
                        if(!filter)
                        {
                            curNode = curNode.AddChild(d); //add child and now curNode points to the next guy
                        }
                        else
                        {
                            curNode = curNode.AddChild(FilterDirectory(d, wordsToAvoid, wordsToInclude));
                            curNode.IsFilteredOut = true;
                        }
                        prevDirName = d.Name;
                    }
                    else
                    { //go up one OR more than one
                        prevNodeDebug = new TreeNode<Directory>(curNode.Data);
                        curNode = curNode.Parent; //This is not good if the first node is not the root...
                        while(!Helpers.IsChildDirString(d.Name, curNode.Data.Name, curNode?.Parent == null))
                        {
                            if(curNode.Parent==null)
                            {
                                break; //this might be hiding an error
                            }
                            curNode = curNode.Parent; // may have to go up more than one
                        }
                        if (!filter)
                        {
                            curNode = curNode.AddChild(d); //add child and now curNode points to the next guy
                        }
                        else
                        {
                            curNode = curNode.AddChild(FilterDirectory(d, wordsToAvoid, wordsToInclude));
                            curNode.IsFilteredOut = true;
                        }
                        prevDirName = d.Name;
                    }
                }

                if(filter)
                {
                    //unhide any ones with valid Files (by default they are all hidden).
                    IterateTreeAndUnsetFilteredForValid(rootNode);
                }
            }
            catch(Exception e)
            {
                MainActivity.LogFirebase("CreateTree " + username + "  " + e.Message + e.StackTrace);
                throw e;
            }


            //logging code for unit tests / diagnostic..
            //var root2 = DocumentFile.FromTreeUri(SoulSeekState.MainActivityRef, Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri));
            //DocumentFile exists2 = root.FindFile(username + "_parsed_answer");
            //if (exists2 == null || !exists2.Exists())
            //{
            //    DocumentFile f = root2.CreateFile(@"custom\binary", username + "_parsed_answer");

            //    System.IO.Stream stream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(f.Uri);
            //    //Java.IO.File musicFile = new Java.IO.File(filePath);
            //    //FileOutputStream stream = new FileOutputStream(mFile);
            //    using (System.IO.MemoryStream userListStream = new System.IO.MemoryStream())
            //    {
            //        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            //        formatter.Serialize(userListStream, rootNode);

            //        //write to binary..

            //        stream.Write(userListStream.ToArray());
            //        stream.Close();
            //    }
            //}
            //end logging code for unit tests / diagnostic..


            errorMsgToToast = "";
            return rootNode;
        }

        private static void IterateTreeAndUnsetFilteredForValid(TreeNode<Directory> root)
        {
            if(root.Data.FileCount!=0)
            {
                //set self and all parents as unhidden
                SetSelfAndAllParentsAsUnFilteredOut(root);
            }
            foreach(TreeNode<Directory> child in root.Children)
            {
                IterateTreeAndUnsetFilteredForValid(child);
            }
        }

        private static void SetSelfAndAllParentsAsUnFilteredOut(TreeNode<Directory> node)
        {
            node.IsFilteredOut = false;
            if(node.Parent==null)
            {
                return;//we reached the top, mission accomplished.
            }
            if(node.Parent.IsFilteredOut)
            {
                SetSelfAndAllParentsAsUnFilteredOut(node.Parent);
            }
        }

        private void DownloadSelectedLogic()
        {
            try
            {
                List<Task> tsks = new List<Task>();
                foreach (int position in this.customAdapter.SelectedPositions)
                {
                    try
                    {
                        Task tsk = CreateDownloadTask(searchResponse.Files.ElementAt(position));
                        if (tsk == null)
                        {
                            Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_duplicate), ToastLength.Long).Show(); });
                            this.activity?.RunOnUiThread(a);
                            return;
                        }
                        tsk.Start();
                        tsks.Add(tsk);
                    }
                    catch (Exception error)
                    {
                        Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_) + error.Message, ToastLength.Long).Show(); });
                        MainActivity.LogFirebase(error.Message + " DlSelected_Click");
                        this.activity.RunOnUiThread(a);
                    }

                }
                Toast.MakeText(Context, Context.GetString(Resource.String.download_is_starting), ToastLength.Short).Show();
                foreach (Task tsk in tsks)
                {
                    tsk.Wait();
                }
                Dismiss();

            }
            catch (DuplicateTransferException)
            {
                string dupMsg = this.activity.GetString(Resource.String.error_duplicates);
                Action a = new Action(() => { Toast.MakeText(this.activity, dupMsg, ToastLength.Long).Show(); });
                MainActivity.LogFirebase(dupMsg + " DlSelected_Click");
                this.activity.RunOnUiThread(a);
            }
            catch (AggregateException age)
            {
                if (age.InnerException is DuplicateTransferException)
                {
                    string dupMsg = this.activity.GetString(Resource.String.error_duplicates);
                    Action a = new Action(() => { Toast.MakeText(this.activity, dupMsg, ToastLength.Long).Show(); });
                    MainActivity.LogFirebase(dupMsg + " DlSelected_Click");
                    this.activity.RunOnUiThread(a);
                }
                else
                {
                    Action a = new Action(() => { Toast.MakeText(this.activity, age.Message, ToastLength.Long).Show(); });
                    MainActivity.LogFirebase(age.Message + " DlSelected_Click");
                    this.activity.RunOnUiThread(a);
                }
            }
        }

        private void DlSelected_Click(object sender, EventArgs e)
        {
            MainActivity.LogDebug("DownloadDialog DlSelected_Click");
            MainActivity.LogDebug("this.customAdapter.SelectedPositions.Count = " + this.customAdapter.SelectedPositions.Count);
            if (this.customAdapter.SelectedPositions.Count == 0)
            {
                Toast.MakeText(Context, Context.GetString(Resource.String.nothing_selected_extra), ToastLength.Short).Show();
                return;
            }
            if(MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                        if (t.IsFaulted)
                        {
                            SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                            return;
                        }
                        SoulSeekState.MainActivityRef.RunOnUiThread(DownloadSelectedLogic);
                }));
            }
            else
            {
                DownloadSelectedLogic();
            }

        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            bool alreadySelected = this.customAdapter.SelectedPositions.Contains<int>(e.Position);
            if(!alreadySelected)
            {
                
#pragma warning disable 0618
                if((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    e.View.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Color.cellbackSelected,null);
                }
                else
                {
                    e.View.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                }
#pragma warning restore 0618
                this.customAdapter.SelectedPositions.Add(e.Position);
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_end_dldiag, null);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Drawable.cell_shape_end_dldiag, null);
                }
                else
                {
                    e.View.Background = Resources.GetDrawable(Resource.Attribute.cellback);
                    e.View.FindViewById(Resource.Id.mainDlLayout).Background = Resources.GetDrawable(Resource.Attribute.cellback);
                }
#pragma warning restore 0618
                this.customAdapter.SelectedPositions.Remove(e.Position);
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Dismiss();
        }

        private void DlAll_Click(object sender, EventArgs e)
        {

            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //we disconnected. login then do the rest.
                //this is due to temp lost connection
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) => {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short).Show(); });
                        return;
                    }
                    MainActivity.LogDebug("DownloadDialog Dl_Click");
                    var task = CreateDownloadAllTask();
                    task.Start(); //start task immediately
                    SoulSeekState.MainActivityRef.RunOnUiThread(() => {
                    Toast.MakeText(Context, Context.GetString(Resource.String.download_is_starting), ToastLength.Short).Show();
                    });
                    task.Wait(); //it only waits for the downloadasync (and optionally connectasync tasks).
                    
                }));
                try
                {
                    t.Wait(); //errors will propagate on WAIT.  They will not propagate on ContinueWith.  So you can get an exception thrown here if there is no network.
                    //we dont need to do anything if there is an exception thrown here.  Since the ContinueWith actually takes care of it by checking if task faulted..
                }
                catch(Exception exx)
                {
                    MainActivity.LogDebug("DownloadDialog DlAll_Click: " + exx.Message);
                    return; //dont dismiss dialog.  that only happens on success..
                }
                Dismiss();
            }
            else
            {
                MainActivity.LogDebug("DownloadDialog Dl_Click");
                var task = CreateDownloadAllTask(); 
                task.Start(); //start task immediately
                Toast.MakeText(Context, Context.GetString(Resource.String.download_is_starting), ToastLength.Short).Show();
                task.Wait(); //it only waits for the downloadasync (and optionally connectasync tasks).
                Dismiss();
            }
        }

        private Task CreateDownloadTask(Soulseek.File file)
        {
            //TODO TODO downloadInfoList is stale..... not what you want to use....
            //TransfersFragment frag = (StaticHacks.TransfersFrag as TransfersFragment);
            if(TransfersFragment.TransferItemManagerDL != null)
            {
                bool dup = TransfersFragment.TransferItemManagerDL.Exists(file.Filename, searchResponse.Username, file.Size);
                if(dup)
                {
                    string msg = "Duplicate Detected: user:" + searchResponse.Username + "filename: " + file.Filename;
                    MainActivity.LogDebug("CreateDownloadTask " + msg);
                    MainActivity.LogFirebase(msg);
                    Action a = new Action(() => { Toast.MakeText(this.activity, this.activity.GetString(Resource.String.error_duplicate), ToastLength.Long); });
                    SoulSeekState.MainActivityRef.RunOnUiThread(a);
                    return null;
                }
            }

            MainActivity.LogDebug("CreateDownloadTask");
            Task task = new Task(()=>
            {
                SetupAndDownloadFile(searchResponse.Username, file.Filename, file.Size, GetQueueLength(searchResponse), out _);

            });
            return task;
        }


        public static void SetupAndDownloadFile(string username, string fname, long size, int queueLength, out bool errorExists)
        {
            errorExists = false;
            Task dlTask = null;
            Android.Net.Uri incompleteUri = null;
            System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();
            bool exists = false;
            TransferItem originalTransferItem;
            TransferItem transferItem = null;
            DownloadInfo downloadInfo = null;
            System.Threading.CancellationTokenSource oldCts = null;
            try
            {

                downloadInfo = new DownloadInfo(username, fname, size, dlTask, cancellationTokenSource, queueLength, 0);

                transferItem = new TransferItem();
                transferItem.Filename = Helpers.GetFileNameFromFile(downloadInfo.fullFilename);
                transferItem.FolderName = Helpers.GetFolderNameFromFile(downloadInfo.fullFilename);
                transferItem.Username = downloadInfo.username;
                transferItem.FullFilename = downloadInfo.fullFilename;
                transferItem.Size = downloadInfo.Size;
                transferItem.QueueLength = downloadInfo.QueueLength;

                try
                {
                    TransfersFragment.SetupCancellationToken(transferItem, downloadInfo.CancellationTokenSource, out oldCts); //if its already there we dont add it..
                }
                catch (Exception errr)
                {
                    MainActivity.LogFirebase("concurrency issue: " + errr); //I think this is fixed by changing to concurrent dict but just in case...
                }
                transferItem = TransfersFragment.TransferItemManagerDL.AddIfNotExistAndReturnTransfer(transferItem, out exists);
                downloadInfo.TransferItemReference = transferItem;




                dlTask = DownloadFileAsync(username, fname, size, cancellationTokenSource);

                var e = new DownloadAddedEventArgs(downloadInfo);
                downloadInfo.downloadTask = dlTask;
                Action<Task> continuationActionSaveFile = MainActivity.DownloadContinuationActionUI(e);
                dlTask.ContinueWith(continuationActionSaveFile);
                MainActivity.InvokeDownloadAddedUINotify(e);




            }
            catch (Exception e)
            {
                if (!exists)
                {
                    TransfersFragment.TransferItemManagerDL.Remove(transferItem); //if it did not previously exist then remove it..
                }
                else
                {
                    errorExists = exists;
                }
                if (oldCts != null)
                {
                    TransfersFragment.SetupCancellationToken(transferItem, oldCts, out _); //put it back..
                }
            }
        }

        /// <summary>
        /// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        /// its the same as the old SoulSeekState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        /// </summary>
        /// <param name="username"></param>
        /// <param name="fullfilename"></param>
        /// <param name="size"></param>
        /// <param name="cts"></param>
        /// <param name="incompleteUri"></param>
        /// <returns></returns>
        public static Task DownloadFileAsync(string username, string fullfilename, long size, CancellationTokenSource cts)
        {
            MainActivity.LogDebug("DownloadFileAsync - " + fullfilename);
            Task dlTask = null;
            if (SoulSeekState.MemoryBackedDownload)
            {
                dlTask =
                    SoulSeekState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        size: size,
                        cancellationToken: cts.Token);
            }
            else
            {



                long partialLength = 0;

                dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
                        username: username,
                        filename: fullfilename,
                        null,
                        size: size,
                        startOffset:partialLength, //this will get populated
                        options: new TransferOptions(disposeOutputStreamOnCompletion: true),
                        cancellationToken: cts.Token,
                        streamTask: GetStreamTask(username, fullfilename));


                //System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, out incompleteUri, out partialLength);
                

                //dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
                //        username: username,
                //        filename: fullfilename,
                //        streamToWriteTo,
                //        size: size,
                //        startOffset:partialLength, //this will get populated
                //        options: new TransferOptions(disposeOutputStreamOnCompletion: true),
                //        cancellationToken: cts.Token);



            }
            return dlTask;
        }

        public static Task<Tuple<System.IO.Stream, long, string, string>> GetStreamTask(string username, string fullfilename)
        {
            Task<Tuple<System.IO.Stream, long, string, string>> task = new Task<Tuple<System.IO.Stream, long, string, string>>(
                () =>
                {
                    long partialLength = 0;
                    Android.Net.Uri incompleteUri = null;
                    Android.Net.Uri incompleteUriDirectory = null;
                    System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, out incompleteUri, out incompleteUriDirectory, out partialLength);
                    return new Tuple<System.IO.Stream, long, string, string>(streamToWriteTo, partialLength, incompleteUri.ToString(), incompleteUriDirectory.ToString());
                });
            return task;
        }


        ///// <summary>
        ///// takes care of resuming incomplete downloads, switching between mem and file backed, creating the incompleteUri dir.
        ///// its the same as the old SoulSeekState.SoulseekClient.DownloadAsync but with a few bells and whistles...
        ///// </summary>
        ///// <param name="username"></param>
        ///// <param name="fullfilename"></param>
        ///// <param name="size"></param>
        ///// <param name="cts"></param>
        ///// <param name="incompleteUri"></param>
        ///// <returns></returns>
        //public static async Task DownloadFileAsync2(string username, string fullfilename, long size, CancellationTokenSource cts)
        //{
        //    Task dlTask = null;
        //    if (SoulSeekState.MemoryBackedDownload)
        //    {
        //        dlTask =
        //            SoulSeekState.SoulseekClient.DownloadAsync(
        //                username: username,
        //                filename: fullfilename,
        //                size: size,
        //                cancellationToken: cts.Token);
        //        //incompleteUri = null;
        //    }
        //    else
        //    {
        //        long partialLength = 0;
        //        System.IO.Stream streamToWriteTo = MainActivity.GetIncompleteStream(username, fullfilename, out _, out partialLength);

        //        dlTask = SoulSeekState.SoulseekClient.DownloadAsync(
        //                username: username,
        //                filename: fullfilename,
        //                streamToWriteTo,
        //                size: size,
        //                startOffset: partialLength,
        //                options: new TransferOptions(disposeOutputStreamOnCompletion: true),
        //                cancellationToken: cts.Token);
        //    }
        //    return dlTask;
        //}


        public static int GetQueueLength(SearchResponse s)
        {
            if(s.FreeUploadSlots>0)
            {
                return 0;
            }
            else
            {
                return (int)(s.QueueLength);
            }
        }

        private Task CreateDownloadAllTask()
        {
            MainActivity.LogDebug("CreateDownloadAllTask");
            Task task = new Task(() => { 
                foreach(Soulseek.File file in searchResponse.Files)
                {
                    SetupAndDownloadFile(searchResponse.Username, file.Filename, file.Size, GetQueueLength(searchResponse), out _);
                }
            });
            return task;
        }

        public void OnCloseClick(object sender, DialogClickEventArgs d)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        public static bool InNightMode(Context c)
        {
            try
            {
            Android.Content.Res.UiMode nightModeFlags = (c.Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask);
            switch(nightModeFlags)
            {
                case Android.Content.Res.UiMode.NightNo:
                    return false;
                case Android.Content.Res.UiMode.NightYes:
                    return true;
                case Android.Content.Res.UiMode.NightUndefined:
                    return false;
                default:
                    return false;
            }
            }
            catch (Exception e)
            {
                MainActivity.LogFirebase(e.Message + " InNightMode");
                return false;
            }
        }

        public void DirectoryReceivedContAction(Task<Directory> dirTask)
        {
            
            //if we have since closed the dialog, then this.View will be null
            if(this.View==null)
            {
                return;
            }
            SoulSeekState.MainActivityRef.RunOnUiThread(() => {
                if (this.View == null)
                {
                    return;
                }
                if (dirTask.IsFaulted)
                {
                    if(dirTask.Exception?.InnerException?.Message!=null)
                    {
                        if(dirTask.Exception.InnerException.Message.ToLower().Contains("timed out"))
                        {
                            Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_timed_out), ToastLength.Short).Show();
                        }
                        MainActivity.LogDebug(dirTask.Exception.InnerException.Message);
                    }
                    Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_failed), ToastLength.Short).Show();
                    MainActivity.LogDebug("DirectoryReceivedContAction faulted");
                }
                else
                {
                    MainActivity.LogDebug("DirectoryReceivedContAction successful!");
                    ListView listView = this.View.FindViewById<ListView>(Resource.Id.listView1);
                    if(listView.Count== dirTask.Result.Files.Count)
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.folder_request_already_have), ToastLength.Short).Show();
                        return;
                    }
                    this.UpdateSearchResponseWithFullDirectory(dirTask.Result);
                    this.UpdateListView();
                    this.UpdateSubHeader();
                    //this.customAdapter = new DownloadCustomAdapter(Context, dirTask.Result.Files.ToList());
                    //this.customAdapter.Owner = this;
                    //listView.Adapter = (customAdapter);
                    ////listView.ItemClick += ListView_ItemClick; //already hooked up!
                    //listView.ChoiceMode = ChoiceMode.Multiple;
                }
            });
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.getFolderContents:
                    string dirname = Helpers.GetDirectoryRequestFolderName(searchResponse.Files.ElementAt(0).Filename);
                    if(dirname==string.Empty)
                    {
                        MainActivity.LogFirebase("The dirname is empty!!");
                        return true;
                    }
                    if (!SoulSeekState.currentlyLoggedIn)
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_in_to_get_dir_contents, ToastLength.Short).Show();
                        return true;
                    }
                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        //we disconnected. login then do the rest.
                        //this is due to temp lost connection
                        Task conTask;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out conTask))
                        {
                            return true;
                        }
                        conTask.ContinueWith(new Action<Task>((Task connectionTask)=>
                        {
                            if(connectionTask.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => { 
                                        Toast tst2 = Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.failed_to_connect), ToastLength.Short);
                                        tst2.Show(); 
                                    }));
                                return;
                            }
                            else
                            {
                                //the original logic...
                                Task<Directory> t = SoulSeekState.SoulseekClient.GetDirectoryContentsAsync(searchResponse.Username, dirname);
                                t.ContinueWith(DirectoryReceivedContAction);
                            }
                        }));
                    }
                    else
                    {
                        Task<Directory> t = SoulSeekState.SoulseekClient.GetDirectoryContentsAsync(searchResponse.Username, dirname); //throws not logged in...
                        t.ContinueWith(DirectoryReceivedContAction);
                    }
                    return true;
                case Resource.Id.browseAtLocation:
                    string startingDir = Helpers.GetDirectoryRequestFolderName(searchResponse.Files.First().Filename);
                    Action<View> action = new Action<View>((v) => {
                        this.Dismiss();
                        ((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });
                    RequestFilesApi(searchResponse.Username, this.View, action, startingDir);
                    return true;
                case Resource.Id.moreInfo:
                    //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
                    //transferItems.CopyTo(tempArry);
                    var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this.Context, Resource.Style.MyAlertDialogTheme);
                    var diag = builder.SetMessage(this.Context.GetString(Resource.String.queue_length_) + searchResponse.QueueLength + System.Environment.NewLine + System.Environment.NewLine + this.Context.GetString(Resource.String.upload_slots_) + searchResponse.FreeUploadSlots).SetPositiveButton("Close", OnCloseClick).Create();
                    diag.Show();
                    //System.Threading.Thread.Sleep(100); Is this required?
                    //diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(9804764)); makes the whole button invisible...
                    if(InNightMode(this.Context))
                    {
                        diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(Android.Graphics.Color.ParseColor("#bcc1f7")));
                    }
                    else
                    {
                        diag.GetButton((int)Android.Content.DialogButtonType.Positive).SetTextColor(new Android.Graphics.Color(Android.Graphics.Color.ParseColor("#4f58c4")));
                    }
                    return true;
                case Resource.Id.getUserInfo:
                    RequestedUserInfoHelper.RequestUserInfoApi(searchResponse.Username);
                    return true;
                default:
                    return false;

            }
        }
    }


    public class DownloadCustomAdapter : ArrayAdapter<Soulseek.File>
    {
        public List<int> SelectedPositions = new List<int>();
        public Android.Support.V4.App.DialogFragment Owner = null;
        public DownloadCustomAdapter(Context c, List<Soulseek.File> items) : base(c, 0, items)
        {
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            DownloadItemView itemView = (DownloadItemView)convertView;
            if (null == itemView)
            {
                itemView = DownloadItemView.inflate(parent);
            }
            itemView.setItem(GetItem(position));

            if(SelectedPositions.Contains(position))
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    itemView.Background = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    itemView.FindViewById<TextView>(Resource.Id.textView1).Background = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                }
                else
                {
                    itemView.Background = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected);
                    itemView.FindViewById<TextView>(Resource.Id.textView1).Background = Owner.Resources.GetDrawable(Resource.Color.cellbackSelected);
                }
#pragma warning restore 0618
            }
            else //views get reused, hence we need to reset the color so that when we scroll the resused views arent still highlighted.
            {
#pragma warning disable 0618
                 if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                 {
                    itemView.Background = Owner.Resources.GetDrawable(Resource.Drawable.cell_shape_end_dldiag, null);
                    //itemView.FindViewById<TextView>(Resource.Id.textView1).Background = Owner.Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                 }
                 else
                 {
                    itemView.Background = Owner.Resources.GetDrawable(Resource.Attribute.cellback);
                    //itemView.FindViewById<TextView>(Resource.Id.textView1).Background = Owner.Resources.GetDrawable(Resource.Color.cellback);
                 }
            }
#pragma warning restore 0618
            return itemView;
            //return base.GetView(position, convertView, parent);
        }
    }

    public class DownloadItemView : RelativeLayout
    {
        private TextView viewFilename;
        //private TextView viewSize;
        private TextView viewAttributes;
        public DownloadItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.download_row, this, true);
            setupChildren();
        }
        public DownloadItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.download_row, this, true);
            setupChildren();
        }

        public static DownloadItemView inflate(ViewGroup parent)
        {
            DownloadItemView itemView = (DownloadItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.download_view_row_dummy, parent, false);
            return itemView;
        }

        private void setupChildren()
        {
            viewFilename = FindViewById<TextView>(Resource.Id.textView1);
            //viewSize = FindViewById<TextView>(Resource.Id.textView2);
            viewAttributes = FindViewById<TextView>(Resource.Id.textView3);
        }

        public void setItem(Soulseek.File item)
        {
            viewFilename.Text = Helpers.GetFileNameFromFile(item.Filename);
            //viewSize.Text = string.Format("{0:0.##} mb", item.Size / (1024.0 * 1024.0));
            viewAttributes.Text = Helpers.GetSizeLengthAttrString(item);
        }

        /// <summary>
        /// The other one cuts off 1 character..... They both do
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        //public static string GetFileNameFromFile(string filename)
        //{
        //    int begin = filename.LastIndexOf("\\");
        //    string clipped = filename.Substring(begin + 1);
        //    return clipped;
        //}

        private string GetStringFromAttributes(IReadOnlyCollection<Soulseek.FileAttribute> fileAttributes)
        {
            if(fileAttributes.Count == 0)
            {
                return "";
            }
            StringBuilder stringBuilder = new StringBuilder();
            string attrString = "";
            foreach(FileAttribute attr in fileAttributes)
            {

                if(attr.Type == FileAttributeType.BitDepth)
                {
                    attrString = attr.Value.ToString();
                }
                else if(attr.Type == FileAttributeType.SampleRate)
                {
                    attrString = (attr.Value / 1000.0).ToString();
                }
                else if(attr.Type == FileAttributeType.BitRate)
                {
                    attrString = attr.Value.ToString() + "kbs";
                }
                else if(attr.Type == FileAttributeType.VariableBitRate)
                {
                    attrString = attr.Value.ToString() + "kbs";
                }
                else if(attr.Type == FileAttributeType.Length)
                {
                    continue;
                }
                stringBuilder.Append(attrString);
                stringBuilder.Append(", ");
            }
            if(stringBuilder.Length <= 3)
            {
                return "";
            }
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
    }


}