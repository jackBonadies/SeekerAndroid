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

using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using Android.Runtime;
using Java.Interop;
using Android.Animation;
using AndroidX.RecyclerView.Widget;
using System.Runtime.Serialization.Formatters.Binary;
using SearchResponseExtensions;
using AndroidX.DocumentFile.Provider;

namespace AndriodApp1
{
    public class TabsPagerAdapter : FragmentPagerAdapter
    {
        Fragment login = null;
        Fragment search = null;
        Fragment transfer = null;
        Fragment browse = null;
        public TabsPagerAdapter(FragmentManager fm) : base(fm)
        {
            login = new LoginFragment();
            search = new SearchFragment();
            transfer = new TransfersFragment();
            browse = new BrowseFragment();
        }

        public override int Count => 4;

        public override Fragment GetItem(int position)
        {
            Fragment frag = null;
            switch (position)
            {
                case 0:
                    frag = login;
                    break;
                case 1:
                    frag = search;
                    break;
                case 2:
                    frag = transfer;
                    break;
                case 3:
                    frag = browse;
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return frag;
        }

        public override int GetItemPosition(Java.Lang.Object @object)
        {
            return PositionNone;
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            ICharSequence title;
            switch (position)
            {
                case 0:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.account_tab));
                    break;
                case 1:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.searches_tab));
                    break;
                case 2:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.transfer_tab));
                    break;
                case 3:
                    title = new Java.Lang.String(SoulSeekState.ActiveActivityRef.GetString(Resource.String.browse_tab));
                    break;
                default:
                    throw new System.Exception("Invalid Tab");
            }
            return title;
        }
    }

    public class PageFragment : Fragment
    {
        private static System.String ARG_PAGE_NUMBER = "page_number";

        public static PageFragment newInstance(int page)
        {
            PageFragment fragment = new PageFragment();
            Bundle args = new Bundle();
            args.PutInt(ARG_PAGE_NUMBER, page);
            fragment.Arguments = args;
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container,
                                 Bundle savedInstanceState)
        {
            int position = Arguments.GetInt(ARG_PAGE_NUMBER);
            int resId = int.MinValue;
            //switch (position)
            //{
            //    case 0:
            resId = Resource.Layout.login;
            //        break;
            //    case 1:
            //        resId = Resource.Layout.searches;
            //        break;
            //    case 2:
            //        resId = Resource.Layout.transfers;
            //        break;
            //    default:
            //        throw new System.Exception("Invalid Position");
            //}
            View rootView = inflater.Inflate(Resource.Layout.login, container, false);
            var txt = rootView.FindViewById<TextView>(Resource.Id.textView);
            txt.Text = "pos " + position;
            return rootView;
        }
    }



    public class LoginFragment : Fragment //, Android.Net.DnsResolver.ICallback //this class sadly gets recreating i.e. not just the view but everything many times. so members are kinda useless...
    {
        public volatile View rootView;
        private Button cbttn;
        private TextView cWelcome;
        private ViewGroup cLoading;
        private bool refreshView = false;


        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.account_menu, menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }

        private static void EnableDisableLoginButton(EditText uname, EditText passwd, Button login)
        {
            if(string.IsNullOrEmpty(uname.Text) || string.IsNullOrEmpty(passwd.Text))
            {
                login.Alpha = 0.5f;
                login.Clickable = false;
            }
            else
            {
                login.Alpha = 1.0f;
                login.Clickable = true;
            }
            try
            {
                SoulSeekState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        private Button loginButton = null;
        private EditText usernameTextEdit = null;
        private EditText passwordTextEdit = null;


        public void SetUpLogInLayout()
        {
            loginButton = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
            loginButton.Click += LogInClick;
            usernameTextEdit = rootView.FindViewById<EditText>(Resource.Id.etUsername);
            passwordTextEdit = rootView.FindViewById<EditText>(Resource.Id.etPassword);
            usernameTextEdit.TextChanged += UsernamePasswordTextEdit_TextChanged;
            usernameTextEdit.FocusChange += SearchFragment.MainActivity_FocusChange;
            passwordTextEdit.TextChanged += UsernamePasswordTextEdit_TextChanged;
            passwordTextEdit.FocusChange += SearchFragment.MainActivity_FocusChange;
            EnableDisableLoginButton(usernameTextEdit, passwordTextEdit, loginButton);
        }


        //private bool firstTime = true;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            HasOptionsMenu = true;
            MainActivity.LogDebug("LoginFragmentOnCreateView");
            StaticHacks.LoginFragment = this;
            if (MainActivity.IsNotLoggedIn())//you are not logged in if username or password is null
            {
                SoulSeekState.currentlyLoggedIn = false;
                this.rootView = inflater.Inflate(Resource.Layout.login, container, false);

                SetUpLogInLayout();

                //StaticHacks.RootView = this.rootView;
                //firstTime = false;
                return rootView;
            }
            else
            {
                if (!SoulSeekState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn) && !StaticHacks.LoggingIn)
                {
                    SoulSeekState.ManualResetEvent.Reset();
                    Task login = null;
                    try
                    {
                        //there is a bug where we reach here with an empty Username and Password...
                        login = SeekerApplication.ConnectAndPerformPostConnectTasks(SoulSeekState.Username, SoulSeekState.Password);
                    }
                    catch (InvalidOperationException)
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.we_are_already_logging_in, ToastLength.Short).Show();
                        MainActivity.LogFirebase("We are already logging in");
                    }
                    //Task login = SoulSeekState.SoulseekClient.ConnectAsync("208.76.170.59", 2271, SoulSeekState.Username, SoulSeekState.Password);
                    login?.ContinueWith(new Action<Task>((task) => { UpdateLoginUI(task); }));
                    SoulSeekState.MainActivityRef.SetUpLoginContinueWith(login); //sets up a continue with if sharing is enabled, else noop
                }
                else if (!StaticHacks.LoggingIn || StaticHacks.UpdateUI)
                {
                    StaticHacks.UpdateUI = false;
                    StaticHacks.LoggingIn = false;
                    refreshView = true;
                }

                this.rootView = inflater.Inflate(Resource.Layout.loggedin, container, false);
                StaticHacks.RootView = this.rootView;
                Button bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                
                bttn.Click += LogoutClick;
                var welcome = rootView.FindViewById<TextView>(Resource.Id.userNameView);
                welcome.Text = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.welcome), SoulSeekState.Username);
                welcome.Visibility = ViewStates.Gone;
                bttn.Visibility = ViewStates.Gone;

                Button settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                settings.Visibility = ViewStates.Gone;
                settings.Click += Settings_Click;

                Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn, 0);
                this.cbttn = bttn;
                this.cLoading = rootView.FindViewById<ViewGroup>(Resource.Id.loggingInLayout);
                this.cWelcome = welcome;
                //firstTime = false;
                return rootView;
            }
        }

        private void UsernamePasswordTextEdit_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            EnableDisableLoginButton(this.usernameTextEdit, this.passwordTextEdit, loginButton);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Settings_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(SoulSeekState.MainActivityRef, typeof(SettingsActivity));
            //intent.PutExtra("SaveDataDirectoryUri", SoulSeekState.SaveDataDirectoryUri); //CURRENT SETTINGS - never necessary... static
            SoulSeekState.MainActivityRef.StartActivityForResult(intent, 140);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            SoulSeekState.ManualResetEvent.Set(); //the UI is ready for any modifications
            if (refreshView)
            {
                refreshView = false;
                UpdateLoginUI(null);
            }
        }

        private void UpdateLoginUI(Task t)
        {
            //all logins go to here...
            if (SoulseekClient.DNS_LOOKUP_FAILED && (t != null && t.Status == TaskStatus.Faulted)) //task can be null and so if DNS lookup fails this will be a nullref...
            {
                //this can happen if we do not have internet....

            }
            else if (SoulseekClient.DNS_LOOKUP_FAILED)
            {
                var action = new Action(() =>
                {
                    Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.dns_failed, ToastLength.Long).Show();
                    //MainActivity.LogFirebase("DNS Lookup of Server Failed. Falling back on hardcoded IP succeeded.");
                });
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
                SoulseekClient.DNS_LOOKUP_FAILED = false; // dont have to keep showing this... wait for next failure for it to be set...
            }

            Console.WriteLine("Update Login UI");
            bool cannotLogin = false;
            string msg = string.Empty;
            string msgToLog = string.Empty;
            bool clearUserPass = true;
            if (t != null && t.Status == TaskStatus.Faulted)
            {
                if (t.Exception != null && t.Exception.InnerExceptions != null && t.Exception.InnerExceptions.Count != 0)
                {
                    Console.WriteLine(t.Exception.ToString());
                    Console.WriteLine(t.Exception?.InnerExceptions?.Count);
                    Console.WriteLine(t.Exception?.InnerExceptions?.ToString());
                    if (t.Exception.InnerExceptions[0] is Soulseek.LoginRejectedException)
                    {
                        cannotLogin = true;
                        msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.bad_user_pass);
                    }
                    else if (t.Exception.InnerExceptions[0] is Soulseek.SoulseekClientException)
                    {
                        if (t.Exception.InnerExceptions[0].Message.Contains("Network is unreachable"))
                        {
                            cannotLogin = true;
                            clearUserPass = false;
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_unreachable);
                        }
                        else if (t.Exception.InnerExceptions[0].Message.Contains("Connection refused"))
                        {
                            cannotLogin = true;
                            clearUserPass = false;
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_unreachable);
                        }
                        else
                        {
                            cannotLogin = true;
                            clearUserPass = false;
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                            msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;

                        }
                    }
                    else if (t.Exception.InnerExceptions[0].Message != null && 
                        (t.Exception.InnerExceptions[0].Message.Contains("wait timed out") || (t.Exception.InnerExceptions[0].Message.ToLower().Contains("operation timed out"))))
                    {
                        //this happens at work where slsk is banned. technically its due to connection RST. the timeout is not the tcp handshake timeout its instead a wait timeout in the connect async code. so this could probably be improved.
                        cannotLogin = true;
                        clearUserPass = false;
                        msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login) + " - Time Out Waiting for Server Response.";
                    }
                    else
                    {
                        msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                        cannotLogin = true;
                        clearUserPass = false;
                        msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                    }
                }
                else
                {
                    if (t.Exception != null)
                    {
                        msgToLog = t.Exception.Message + t.Exception.StackTrace;
                    }
                    cannotLogin = true;
                    msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                }

                if (msgToLog != string.Empty)
                {
                    MainActivity.LogDebug(msgToLog);
                    MainActivity.LogFirebase(msgToLog);
                }

                MainActivity.LogDebug("time to update layouts..");
                MainActivity.AddLoggedInLayout(this.rootView);
                MainActivity.BackToLogInLayout(this.rootView, LogInClick, clearUserPass);
            }


            MainActivity.LogDebug("Login Status: " + cannotLogin);
            //SoulSeekState.ManualResetEvent.WaitOne();

            if (cannotLogin == false)
            {
                StaticHacks.UpdateUI = true;
                SoulSeekState.currentlyLoggedIn = true; //when we recreate we lose the statics as they get overwritten by bundle
                StaticHacks.LoggingIn = false;
                //SoulSeekState.currentSessionLoggedIn = true;
                //var tabLayout = (Android.Support.Design.Widget.TabLayout)SoulSeekState.MainActivityRef.FindViewById(Resource.Id.tabs);
                //tabLayout.RemoveTabAt(0);

                MainActivity.AddLoggedInLayout(this.rootView);

                MainActivity.UpdateUIForLoggedIn(this.rootView, LogoutClick, cWelcome, cbttn, cLoading, Settings_Click);


            }
            else
            {
                var action = new Action(() =>
                {
                    string message = msg;
                    Toast.MakeText(SoulSeekState.MainActivityRef, msg, ToastLength.Long).Show();
                    SoulSeekState.currentlyLoggedIn = false; //this should maybe be removed???
                    SoulSeekState.Username = null;
                    SoulSeekState.Password = null;
                    //this.Activity.Recreate();
                    //SoulSeekState.MainActivityRef.Recreate();
                });

                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
        }

        private void LogoutClick(object sender, EventArgs e)
        {//logout
            try
            {
                SoulSeekState.logoutClicked = true;
                SoulSeekState.SoulseekClient.Disconnect();
            }
            catch
            {

            }
            SoulSeekState.Username = null;
            SoulSeekState.Password = null;
            SoulSeekState.currentlyLoggedIn = false;
            SoulSeekState.MainActivityRef.Recreate();

        }

        public void LogInClick(object sender, EventArgs e)
        {
            var pager = (Android.Support.V4.View.ViewPager)Activity.FindViewById(Resource.Id.pager);

            Task login = null;
            try
            {
                //System.Net.IPAddress ipaddr = null;
                //try
                //{
                //MUST DO THIS ON A SEPARATE THREAD !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //ipaddr = System.Net.Dns.GetHostEntry("vps.slsknet.org").AddressList[0];
                //Android.Net.DnsResolver.Instance.Query(null, "vps.slsknet.org",Android.Net.DnsResolverFlag.Empty,this.Context.MainExecutor,null,this);
                if (string.IsNullOrEmpty(this.usernameTextEdit.Text) || string.IsNullOrEmpty(this.passwordTextEdit.Text))
                {
                    Toast.MakeText(this.Activity, Resource.String.no_empty_user_pass, ToastLength.Long).Show();
                    return;
                }
                login = SeekerApplication.ConnectAndPerformPostConnectTasks(this.usernameTextEdit.Text, this.passwordTextEdit.Text);
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)(this.Activity).GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(this.usernameTextEdit.WindowToken, 0);
                }
                catch (System.Exception)
                {

                }
                //}
                //catch(AddressException)
                //{
                //    Toast.MakeText(this.Activity, "Failed to resolve soulseek server ip address from name. Trying hardcoded ip.", ToastLength.Long).Show();
                //    login = SoulSeekState.SoulseekClient.ConnectAsync("208.76.170.59", 2271, user.Text, pass.Text);
                //}
            }
            catch (AddressException)
            {
                Toast.MakeText(this.Activity, Resource.String.dns_failed_2, ToastLength.Long).Show();
                SoulSeekState.currentlyLoggedIn = false;
                return;
            }
            catch (InvalidOperationException err)
            {
                if (err.Message.Equals("The client is already connected"))
                {
                    SoulSeekState.currentlyLoggedIn = true;
                    MainActivity.AddLoggedInLayout(StaticHacks.RootView);
                    MainActivity.UpdateUIForLoggedIn(StaticHacks.RootView);
                }
                else
                {
                    Toast.MakeText(this.Activity, err.Message, ToastLength.Long).Show();
                    SoulSeekState.currentlyLoggedIn = false;
                    return;
                }
            }
            try
            {
                //SoulSeekState.currentlyLoggedIn = true;
                //LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                //View rootView = inflater.Inflate(Resource.Layout.loggedin,null);
                MainActivity.AddLoggedInLayout(this.rootView); //i.e. if not already
                MainActivity.UpdateUIForLoggingInLoading(this.rootView);
                SoulSeekState.Username = this.usernameTextEdit.Text;
                SoulSeekState.Password = this.passwordTextEdit.Text;
                StaticHacks.LoggingIn = true;
                SoulSeekState.ManualResetEvent.Reset();
                //if(login.IsCompleted)
                //{
                //    StaticHacks.UpdateUI = true;
                //    SoulSeekState.currentlyLoggedIn = true;
                //    StaticHacks.LoggingIn = false;
                //    Activity.Recreate();
                //}
                //else
                //{
                if (login == null) //i.e. if we threw bc we are already logged in or similar
                {
                    return;
                }
                login.ContinueWith(new Action<Task>((task) => { UpdateLoginUI(task); }));
                //}
                //if we get here then we logged in
                //rootView.FindViewById<TextView>(Resource.Id.userNameView).Text = "Welcome, " + SoulSeekState.Username;
            }
            catch (System.Exception ex)
            {
                string message = string.Empty;
                if (ex?.InnerException is Soulseek.LoginRejectedException)
                {
                    message = SoulSeekState.ActiveActivityRef.GetString(Resource.String.bad_user_pass);
                }
                else
                {
                    message = ex.Message;
                }
                Toast.MakeText(this.Activity, message, ToastLength.Long).Show();
                SoulSeekState.currentlyLoggedIn = false;
            }

            //Activity.Recreate();
        }

        //public void OnAnswer(Java.Lang.Object answer, int rcode)
        //{
        //    //NEVER GOT THIS WORKING. supposed to be a fallback if the dns resolve does not work.
        //    //might be best to async try the System.Net.Dns resolve.  or to see if it has a timeout or error handling stuff.
        //    //dont know why it happens...


        //    //Java.Util.ArrayList ans = answer as Java.Util.ArrayList;
        //    //IEnumerable<Java.Net.Inet4Address>  ans2 = answer as IEnumerable<Java.Net.Inet4Address>;
        //    //Java.Net.Inet4Address ans3 = ans2.ToArray()[0] as Java.Net.Inet4Address;
        //    //Java.Net.Inet4Address ans1 = ans.ToArray()[0] as Java.Net.Inet4Address;
        //    //string a = ans1.HostAddress;

        //    ////Java.Util.ArrayList<Java.Net.InetAddress> ans = answer as Java.Util.LinkedList<Java.Net.InetAddress>;
        //    ////string a = ans[0].HostAddress;
        //    ////System.Console.WriteLine(a);
        //    //throw new NotImplementedException();
        //}

        //public void OnError(DnsResolver.DnsException error)
        //{
        //    //throw new NotImplementedException();
        //}


    }

    public class SearchResultComparableWishlist : SearchResultComparable
    {
        public SearchResultComparableWishlist(SearchResultSorting _searchResultSorting) : base(_searchResultSorting)
        {

        }

        public override int Compare(SearchResponse x, SearchResponse y)
        {
            if (x.Username == y.Username)
            {
                if ((x.FileCount == y.FileCount) && (x.LockedFileCount == y.LockedFileCount))
                {
                    if (x.FileCount != 0 && (x.Files.First().Filename == y.Files.First().Filename))
                    {
                        return 0;
                    }
                    if (x.LockedFileCount != 0 && (x.LockedFiles.First().Filename == y.LockedFiles.First().Filename))
                    {
                        return 0;
                    }
                }
            }
            return base.Compare(x, y); //the actual comparison for which is "better"
        }

        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
    }

    public class SearchResultComparable : IComparer<SearchResponse>
    {
        private readonly SearchResultSorting searchResultSorting;
        public SearchResultComparable(SearchResultSorting _searchResultSorting)
        {
            searchResultSorting = _searchResultSorting;
        }

        public virtual int Compare(SearchResponse x, SearchResponse y)
        {
            if(searchResultSorting == SearchResultSorting.Available)
            {
                //highest precedence. locked files.
                //so if any of the search responses have 0 unlocked files, they are considered the worst.
                if ((x.FileCount != 0 && y.FileCount == 0) || (x.FileCount == 0 && y.FileCount != 0))
                {
                    if (y.FileCount == 0)
                    {
                        //x is better
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                //next highest - free upload slots. for now just they are free or not.
                if ((x.FreeUploadSlots == 0 && y.FreeUploadSlots != 0) || (x.FreeUploadSlots != 0 && y.FreeUploadSlots == 0))
                {
                    if (x.FreeUploadSlots == 0)
                    {
                        //x is worse
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                //next highest - queue length
                if (x.QueueLength != y.QueueLength)
                {
                    if (x.QueueLength > y.QueueLength)
                    {
                        //x is worse
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                //next speed (MOST should fall here, from my testing at least).
                if (x.UploadSpeed != y.UploadSpeed)
                {
                    if (x.UploadSpeed > y.UploadSpeed)
                    {
                        //x is better
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                //VERY FEW, should go here
                if (x.Files.Count != 0 && y.Files.Count != 0)
                {
                    return x.Files.First().Filename.CompareTo(y.Files.First().Filename);
                }
                if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
                {
                    return x.LockedFiles.First().Filename.CompareTo(y.LockedFiles.First().Filename);
                }
                return 0;
            }
            else if(searchResultSorting == SearchResultSorting.Fastest)
            {
                //for fastest, only speed matters. if they pick this then even locked files are in the running.
                if (x.UploadSpeed != y.UploadSpeed)
                {
                    if (x.UploadSpeed > y.UploadSpeed)
                    {
                        //x is better
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                if (x.Files.Count != 0 && y.Files.Count != 0)
                {
                    return x.Files.First().Filename.CompareTo(y.Files.First().Filename);
                }
                if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
                {
                    return x.LockedFiles.First().Filename.CompareTo(y.LockedFiles.First().Filename);
                }
                return 0;
            }
            else if(searchResultSorting == SearchResultSorting.FolderAlphabetical)
            {
                if (x.Files.Count != 0 && y.Files.Count != 0)
                {
                    string xFolder = Helpers.GetFolderNameFromFile(x.Files.First().Filename);
                    string yFolder = Helpers.GetFolderNameFromFile(y.Files.First().Filename);
                    int ret = xFolder.CompareTo(yFolder);
                    if(ret != 0)
                    {
                        return ret;
                    }
                }
                if (x.LockedFiles.Count != 0 && y.LockedFiles.Count != 0)
                {
                    string xLockedFolder = Helpers.GetFolderNameFromFile(x.Files.First().Filename);
                    string yLockedFolder = Helpers.GetFolderNameFromFile(y.Files.First().Filename);
                    int lockedret = xLockedFolder.CompareTo(yLockedFolder);
                    if (lockedret != 0)
                    {
                        return lockedret;
                    }
                }

                //if its a tie (which is probably pretty common)
                //both username and foldername cant be same, so we are safe doing this..
                int userRet = x.Username.CompareTo(y.Username);
                if (userRet != 0)
                {
                    return userRet;
                }
                return 0;
            }
            else
            {
                throw new System.Exception("Unknown sorting algorithm");
            }
        }
    }

    public class FilterSpecialFlags
    {
        public bool ContainsSpecialFlags = false;
        public int MinFoldersInFile = 0;
        public int MinFileSizeMB = 0;
        public int MinBitRateKBS = 0;
        public bool IsVBR = false;
        public bool IsCBR = false;
        public void Clear()
        {
            ContainsSpecialFlags = false;
            MinFoldersInFile = 0;
            MinFileSizeMB = 0;
            MinBitRateKBS = 0;
            IsVBR = false;
            IsCBR = false;
        }
    }

    public enum SearchTarget
    {
        AllUsers = 0,
        UserList = 1,
        ChosenUser = 2,
        Wishlist = 3,
        Room = 4
    }

    public enum SearchResultStyleEnum
    {
        Minimal = 0,
        Medium = 1,
        CollapsedAll = 2,
        ExpandedAll = 3,
    }

    public enum TabType
    {
        Search = 0,
        Wishlist = 1
    }

    public enum SearchResultSorting
    {
        Available = 0,
        Fastest = 1,
        FolderAlphabetical = 2,
    }

    public class SearchTab
    {
        public List<SearchResponse> SearchResponses = new List<SearchResponse>();
        public SortedDictionary<SearchResponse, object> SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparable(SoulSeekState.DefaultSearchResultSortAlgorithm));
        public SearchResultSorting SortHelperSorting = SoulSeekState.DefaultSearchResultSortAlgorithm;
        public object SortHelperLockObject = new object();
        public bool FilteredResults = false;
        public bool FilterSticky = false;
        public string FilterString = string.Empty;
        public List<string> WordsToAvoid = new List<string>();
        public List<string> WordsToInclude = new List<string>();
        public FilterSpecialFlags FilterSpecialFlags = new FilterSpecialFlags();
        public List<SearchResponse> UI_SearchResponses = new List<SearchResponse>();
        public SearchTarget SearchTarget = SearchTarget.AllUsers;
        public bool CurrentlySearching = false;
        public string SearchTargetChosenRoom = string.Empty;
        public string SearchTargetChosenUser = string.Empty;
        public int LastSearchResponseCount = -1; //this tell us how many we have filtered.  since we only filter when its the Current UI Tab.
        public CancellationTokenSource CancellationTokenSource = null;
        public DateTime LastRanTime = DateTime.MinValue;

        public string LastSearchTerm = string.Empty;
        public int LastSearchResultsCount = 0;

        public List<ChipDataItem> ChipDataItems;
        public SearchFragment.ChipFilter ChipsFilter;

        public bool IsLoaded()
        {
            return this.SearchResponses != null;
        }


        public SearchTab Clone(bool forWishlist)
        {
            SearchTab clone = new SearchTab();
            clone.SearchResponses = this.SearchResponses.ToList();
            SortedDictionary<SearchResponse, object> cloned = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(clone.SortHelperSorting));
            //without lock, extremely easy to reproduce "collection was modified" exception if creating wishlist tab while searching.
            lock(this.SortHelperLockObject) //lock the sort helper we are copying from
            {
                foreach (var entry in SortHelper)
                {
                    if (!cloned.ContainsKey(entry.Key))
                    {
                        cloned.Add(entry.Key, entry.Value);
                    }
                }
            }
            clone.SortHelper = cloned;
            clone.FilteredResults = this.FilteredResults;
            clone.FilterSticky = this.FilterSticky;
            clone.FilterString = this.FilterString;
            clone.WordsToAvoid = this.WordsToAvoid.ToList();
            clone.WordsToInclude = this.WordsToInclude.ToList();
            clone.FilterSpecialFlags = this.FilterSpecialFlags;
            clone.UI_SearchResponses = this.UI_SearchResponses.ToList();
            clone.CurrentlySearching = this.CurrentlySearching;
            clone.SearchTarget = this.SearchTarget;
            clone.SearchTargetChosenRoom = this.SearchTargetChosenRoom;
            clone.SearchTargetChosenUser = this.SearchTargetChosenUser;
            clone.LastSearchResponseCount = this.LastSearchResponseCount;
            clone.LastSearchTerm = this.LastSearchTerm;
            clone.LastSearchResultsCount = this.LastSearchResultsCount;
            clone.LastRanTime = this.LastRanTime;
            clone.ChipDataItems = this.ChipDataItems;
            clone.ChipsFilter = this.ChipsFilter;
            return clone;
        }
    }

    [Serializable]
    public class SavedStateSearchTabHeader
    {
        string LastSearchTerm;
        long LastRanTime;
        int LastSearchResultsCount;

        /// <summary>
        /// Get what you need to display the tab (i.e. result count, term, last ran)
        /// </summary>
        /// <param name="searchTab"></param>
        /// <returns></returns>
        public static SavedStateSearchTabHeader GetSavedStateHeaderFromTab(SearchTab searchTab)
        {
            SavedStateSearchTabHeader searchTabState = new SavedStateSearchTabHeader();
            searchTabState.LastSearchResultsCount = searchTab.LastSearchResultsCount;
            searchTabState.LastSearchTerm = searchTab.LastSearchTerm;
            searchTabState.LastRanTime = searchTab.LastRanTime.Ticks;
            return searchTabState;
        }

        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// this restores the wishlist tabs, optionally with the search results, otherwise they will be added later.
        /// </summary>
        /// <param name="savedState"></param>
        /// <returns></returns>
        public static SearchTab GetTabFromSavedState(SavedStateSearchTabHeader savedStateHeader, List<SearchResponse> responses)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = responses;
            searchTab.LastSearchTerm = savedStateHeader.LastSearchTerm;
            searchTab.LastRanTime = new DateTime(savedStateHeader.LastRanTime);
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = responses != null ? responses.Count : savedStateHeader.LastSearchResultsCount;
            if (SearchFragment.FilterSticky)
            {
                searchTab.FilterSticky = SearchFragment.FilterSticky;
                searchTab.FilterString = SearchFragment.FilterStickyString;
                SearchFragment.ParseFilterString(searchTab);
            }
            searchTab.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(searchTab.SortHelperSorting));
            if (responses != null)
            {
                foreach (SearchResponse resp in searchTab.SearchResponses)
                {
                    if (!searchTab.SortHelper.ContainsKey(resp))
                    {
                        //bool isItActuallyNotThere = true;
                        //foreach(var key in searchTab.SortHelper.Keys)
                        //{
                        //    if (key.Username == resp.Username)
                        //    {
                        //        if ((key.FileCount == resp.FileCount) && (key.LockedFileCount == resp.LockedFileCount))
                        //        {
                        //            if (key.FileCount != 0 && (key.Files.First().Filename == resp.Files.First().Filename))
                        //            {
                        //                isItActuallyNotThere = false;
                        //            }
                        //            if (key.LockedFileCount != 0 && (key.LockedFiles.First().Filename == resp.LockedFiles.First().Filename))
                        //            {
                        //                isItActuallyNotThere = false;
                        //            }
                        //        }
                        //    }
                        //}

                        searchTab.SortHelper.Add(resp, null);
                    }
                    else
                    {

                    }
                }
            }
            return searchTab;

        }


    }

    [Serializable]
    public class SavedStateSearchTab
    {
        //there are all of the things we must save in order to later restore a SearchTab
        public List<SearchResponse> searchResponses;
        public string LastSearchTerm;
        public long LastRanTime;
        public static SavedStateSearchTab GetSavedStateFromTab(SearchTab searchTab)
        {
            SavedStateSearchTab searchTabState = new SavedStateSearchTab();
            searchTabState.searchResponses = searchTab.SearchResponses.ToList();
            searchTabState.LastSearchTerm = searchTab.LastSearchTerm;
            searchTabState.LastRanTime = searchTab.LastRanTime.Ticks;
            return searchTabState;
        }

        /// <summary>
        /// these by definition will always be wishlist tabs...
        /// </summary>
        /// <param name="savedState"></param>
        /// <returns></returns>
        public static SearchTab GetTabFromSavedState(SavedStateSearchTab savedState, bool searchResponsesOnly=false, SearchTab oldTab = null)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = savedState.searchResponses;
            if(!searchResponsesOnly)
            {
                searchTab.LastSearchTerm = savedState.LastSearchTerm;
                searchTab.LastRanTime = new DateTime(savedState.LastRanTime);
            }
            else
            {
                searchTab.LastSearchTerm = oldTab.LastSearchTerm;
                searchTab.LastRanTime = oldTab.LastRanTime;
            }
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = searchTab.SearchResponses.Count;
            if (SearchFragment.FilterSticky)
            {
                searchTab.FilterSticky = SearchFragment.FilterSticky;
                searchTab.FilterString = SearchFragment.FilterStickyString;
                SearchFragment.ParseFilterString(searchTab);
            }
            searchTab.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(searchTab.SortHelperSorting));
            foreach (SearchResponse resp in searchTab.SearchResponses)
            {
                if (!searchTab.SortHelper.ContainsKey(resp))
                {
                    //bool isItActuallyNotThere = true;
                    //foreach(var key in searchTab.SortHelper.Keys)
                    //{
                    //    if (key.Username == resp.Username)
                    //    {
                    //        if ((key.FileCount == resp.FileCount) && (key.LockedFileCount == resp.LockedFileCount))
                    //        {
                    //            if (key.FileCount != 0 && (key.Files.First().Filename == resp.Files.First().Filename))
                    //            {
                    //                isItActuallyNotThere = false;
                    //            }
                    //            if (key.LockedFileCount != 0 && (key.LockedFiles.First().Filename == resp.LockedFiles.First().Filename))
                    //            {
                    //                isItActuallyNotThere = false;
                    //            }
                    //        }
                    //    }
                    //}

                    searchTab.SortHelper.Add(resp, null);
                }
                else
                {
                    
                }
            }
            return searchTab;
        }
    }

    public class SearchTabHelper
    {
        public static void SaveStateToSharedPreferencesFullLegacy()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                Dictionary<int, SavedStateSearchTab> savedStates = new Dictionary<int, SavedStateSearchTab>();
                foreach (int tabIndex in tabsToSave)
                {
                    savedStates.Add(tabIndex, SavedStateSearchTab.GetSavedStateFromTab(SearchTabHelper.SearchTabCollection[tabIndex]));
                }
                using (System.IO.MemoryStream savedStateStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(savedStateStream, savedStates);
                    stringToSave = Convert.ToBase64String(savedStateStream.ToArray());
                }
            }

            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchTabsState_LEGACY, stringToSave);
                editor.Commit();
            }

            sw.Stop();
            MainActivity.LogDebug("OLD STYLE: " + sw.ElapsedMilliseconds);
        }

        public static void RemoveTabFromSharedPrefs(int wishlistSearchResultsToRemove, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToRemove) + "_wishlist_tab";
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);
            if(!fileForOurInternalStorage.Delete())
            {
                MainActivity.LogDebug("HEADERS - Delete Search Results: FAILED TO DELETE");
                MainActivity.LogFirebase("HEADERS - Delete Search Results: FAILED TO DELETE");
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Delete Search Results: " + sw.ElapsedMilliseconds);
        }


        public static void SaveAllSearchTabsToDisk(Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                foreach (int tabIndex in tabsToSave)
                {
                    SaveSearchResultsToDisk(tabIndex, c);
                }
            }
            sw.Stop();
            MainActivity.LogDebug("HEADERS - Save ALL Search Results: " + sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Restoring them when someone taps them is fast enough even for 1000 results...
        /// So this method probably isnt needed.
        /// </summary>
        /// <param name="c"></param>
        public static void RestoreAllSearchTabsFromDisk(Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                foreach (int tabIndex in tabsToSave)
                {
                    RestoreSearchResultsFromDisk(tabIndex, c);
                }
            }
            sw.Stop();
            MainActivity.LogDebug("HEADERS - Restore ALL Search Results: " + sw.ElapsedMilliseconds);
        }


        public static void SaveSearchResultsToDisk(int wishlistSearchResultsToSave, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            if (!wishlist_dir.Exists())
            {
                wishlist_dir.Mkdir();
            }
            string name = System.Math.Abs(wishlistSearchResultsToSave) + "_wishlist_tab"; 
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);
            System.IO.Stream outputStream = c.ContentResolver.OpenOutputStream(Android.Support.V4.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri, "w");


            using (System.IO.MemoryStream searchRes = new System.IO.MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(searchRes, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToSave].SearchResponses);
                byte[] arr = searchRes.ToArray();
                outputStream.Write(arr,0,arr.Length);
                outputStream.Flush();
                outputStream.Close();
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Save Search Results: " + sw.ElapsedMilliseconds + " count " + SearchTabHelper.SearchTabCollection[wishlistSearchResultsToSave].SearchResponses.Count);
        }


        public static void RestoreSearchResultsFromDisk(int wishlistSearchResultsToRestore, Context c)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Java.IO.File wishlist_dir = new Java.IO.File(c.FilesDir, "wishlist_dir");
            //if (!wishlist_dir.Exists())
            //{
            //    wishlist_dir.Mkdir();
            //}
            string name = System.Math.Abs(wishlistSearchResultsToRestore) + "_wishlist_tab";
            Java.IO.File fileForOurInternalStorage = new Java.IO.File(wishlist_dir, name);

            //there are two cases.
            //  1) we imported the term.  In that case there are no results yet as it hasnt been ran.  Which is fine.  
            //  2) its a bug.
            if(!fileForOurInternalStorage.Exists())
            {
                if(SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResultsCount == 0 || SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastRanTime == DateTime.MinValue)
                {
                    //nothing to do.  this is the good case..
                }
                else
                {
                    //log error... but still safely fix the state. otherwise the user wont even be able to load the app without crash...
                    MainActivity.LogFirebase("search tab does not exist on disk but it should... ");
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastRanTime = DateTime.MinValue;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResponseCount = 0;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].LastSearchResultsCount = 0;
                    try
                    {
                        //may not be on UI thread if from wishlist timer elapsed...
                        Toast.MakeText(c, "Failed to restore wishlist search results from disk", ToastLength.Long).Show();
                    }
                    catch
                    {

                    }
                }
                //safely fix the state. even in case of error...
                SavedStateSearchTab tab = new SavedStateSearchTab();
                tab.searchResponses = new List<SearchResponse>();
                SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore] = SavedStateSearchTab.GetTabFromSavedState(tab, true, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore]);
                return;
            }

            using(System.IO.Stream inputStream = c.ContentResolver.OpenInputStream(Android.Support.V4.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri))
            {
                MainActivity.LogDebug("HEADERS - get file: " + sw.ElapsedMilliseconds);

                using(System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    ms.Position = 0;
                    MainActivity.LogDebug("HEADERS - read file: " + sw.ElapsedMilliseconds);
                    BinaryFormatter formatter = new BinaryFormatter();
                    //SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].SearchResponses = formatter.Deserialize(ms) as List<SearchResponse>;
                    SavedStateSearchTab tab = new SavedStateSearchTab();
                    tab.searchResponses = formatter.Deserialize(ms) as List<SearchResponse>;
                    SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore] = SavedStateSearchTab.GetTabFromSavedState(tab, true, SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore]);

                    //SearchTabCollection[pair.Key] = SavedStateSearchTab.GetTabFromSavedState(pair.Value);
                }
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - Restore Search Results: " + sw.ElapsedMilliseconds + " count " + SearchTabHelper.SearchTabCollection[wishlistSearchResultsToRestore].SearchResponses.Count);
        }


        public static void SaveHeadersToSharedPrefs()
        {

            var sw = System.Diagnostics.Stopwatch.StartNew();

            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if (tabsToSave.Count == 0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                Dictionary<int, SavedStateSearchTabHeader> savedStates = new Dictionary<int, SavedStateSearchTabHeader>();
                foreach (int tabIndex in tabsToSave)
                {
                    savedStates.Add(tabIndex, SavedStateSearchTabHeader.GetSavedStateHeaderFromTab(SearchTabHelper.SearchTabCollection[tabIndex]));
                }
                using (System.IO.MemoryStream savedStateStream = new System.IO.MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(savedStateStream, savedStates);
                    stringToSave = Convert.ToBase64String(savedStateStream.ToArray());
                }
            }

            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchTabsState_Headers, stringToSave);
                editor.Commit();
            }

            sw.Stop();
            MainActivity.LogDebug("HEADERS - SaveHeadersToSharedPrefs: " + sw.ElapsedMilliseconds);
        }

        //load legacy, and then save new to shared prefs and disk
        public static void ConvertLegacyWishlistsIfApplicable(Context c)
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty);
            if (savedState == string.Empty)
            {
                //nothing to do...
                return;
            }
            else
            {
                MainActivity.LogDebug("Converting Wishlists to New Format...");
                RestoreStateFromSharedPreferencesLegacy();
                SoulSeekState.SharedPreferences.Edit().Remove(SoulSeekState.M_SearchTabsState_LEGACY).Commit();
                //string x = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty); //works, string is empty.
                SaveHeadersToSharedPrefs();
                SaveAllSearchTabsToDisk(c);
            }
        }

        public static void RestoreHeadersFromSharedPreferences()
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_Headers, string.Empty);
            if (savedState == string.Empty)
            {
                return;
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                MainActivity.LogDebug("HEADERS - base64 string length: " + sw.ElapsedMilliseconds);

                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(Convert.FromBase64String(savedState)))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    var savedStateDict = formatter.Deserialize(memStream) as Dictionary<int, SavedStateSearchTabHeader>;
                    int lowestID = int.MaxValue;
                    foreach (var pair in savedStateDict)
                    {
                        if (pair.Key < lowestID)
                        {
                            lowestID = pair.Key;
                        }
                        SearchTabCollection[pair.Key] = SavedStateSearchTabHeader.GetTabFromSavedState(pair.Value, null);
                    }
                    if (lowestID != int.MaxValue)
                    {
                        lastWishlistID = lowestID;
                    }
                }
                sw.Stop();
                MainActivity.LogDebug("HEADERS - RestoreStateFromSharedPreferences: wishlist: " + sw.ElapsedMilliseconds);
            }
            //SoulSeekState.SharedPreferences.Edit().Remove
        }

        public static void RestoreStateFromSharedPreferencesLegacy()
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState_LEGACY, string.Empty);
            if (savedState == string.Empty)
            {
                return;
            }
            else
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                MainActivity.LogDebug("base64 string length: " + sw.ElapsedMilliseconds);

                using (System.IO.MemoryStream memStream = new System.IO.MemoryStream(Convert.FromBase64String(savedState)))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    var savedStateDict = formatter.Deserialize(memStream) as Dictionary<int, SavedStateSearchTab>;
                    int lowestID = int.MaxValue;
                    foreach (var pair in savedStateDict)
                    {
                        if (pair.Key < lowestID)
                        {
                            lowestID = pair.Key;
                        }
                        SearchTabCollection[pair.Key] = SavedStateSearchTab.GetTabFromSavedState(pair.Value);
                    }
                    if (lowestID != int.MaxValue)
                    {
                        lastWishlistID = lowestID;
                    }
                }
                sw.Stop();
                MainActivity.LogDebug("RestoreStateFromSharedPreferences: wishlist: " + sw.ElapsedMilliseconds);
            }
        }




        static SearchTabHelper()
        {
            SearchTabCollection[CurrentTab] = new SearchTab();
        }
        //CURRENT TAB == what is current being shown in UI. Therefore, everyone should use it OTHER THAN the search logic which should only use it IF it matches the current tab.
        public static volatile int CurrentTab = 0;
        public static TabType CurrentTabType = TabType.Search;
        public static string FilterStickyString = string.Empty;

        public static System.Collections.Concurrent.ConcurrentDictionary<int, SearchTab> SearchTabCollection = new System.Collections.Concurrent.ConcurrentDictionary<int, SearchTab>();
        private static int lastSearchID = 0;
        private static int lastWishlistID = 0;

        //all of these getters and setters work on current tab.

        public static int AddSearchTab() //returns ID of new search term added.
        {
            lastSearchID++;
            SearchTabCollection[lastSearchID] = new SearchTab();
            return lastSearchID;
        }

        //public static int AddWishlistSearchTab() //returns ID of new search term added.
        //{
        //    lastWishlistID--;
        //    SearchTabCollection[lastWishlistID] = new SearchTab();
        //    SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
        //    return lastWishlistID;
        //}

        public static void AddWishlistSearchTabFromCurrent()
        {
            lastWishlistID--;
            SearchTabCollection[lastWishlistID] = SearchTabCollection[CurrentTab].Clone(true);
            SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
            SearchTabCollection[lastWishlistID].CurrentlySearching = false;

            //*********************
            SearchTabHelper.SaveSearchResultsToDisk(lastWishlistID, SoulSeekState.ActiveActivityRef);
            SearchTabHelper.SaveHeadersToSharedPrefs();
        }

        /// <summary>
        /// This is done in the case of import
        /// TODO: testing - this is the only way to create a wishlist tab without the main activity created so beware!! and test!!
        ///    prove that this is always initialized (so we dont save and wipe away all previous wishes...)
        /// </summary>
        public static void AddWishlistSearchTabFromString(string wish)
        {
            lastWishlistID--;
            SearchTabCollection[lastWishlistID] = new SearchTab();
            SearchTabCollection[lastWishlistID].LastSearchTerm = wish;
            SearchTabCollection[lastWishlistID].SearchTarget = SearchTarget.Wishlist;
            SearchTabCollection[lastWishlistID].CurrentlySearching = false;

            //*********************

        }

        public static int LastSearchResultsCount
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchResultsCount;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchResultsCount = value;
            }
        }

        public static string LastSearchTerm
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchTerm;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchTerm = value;
            }
        }

        public static SearchResultSorting SortHelperSorting
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelperSorting;
            }
            set
            {
                SearchTabCollection[CurrentTab].SortHelperSorting = value;
            }
        }

        public static int LastSearchResponseCount //static so when the fragment gets remade we can use it
        {
            get
            {
                return SearchTabCollection[CurrentTab].LastSearchResponseCount;
            }
            set
            {
                SearchTabCollection[CurrentTab].LastSearchResponseCount = value;
            }
        }

        public static List<SearchResponse> SearchResponses //static so when the fragment gets remade we can use it
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchResponses;

            }
            set
            {
                SearchTabCollection[CurrentTab].SearchResponses = value;
            }
        }

        public static SortedDictionary<SearchResponse, object> SortHelper
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelper;
            }
            set
            {
                SearchTabCollection[CurrentTab].SortHelper = value;
            }
        }

        /// <summary>
        /// Locking on the SortHelper is not enough, since it gets replaced if user changes the sort algorithm
        /// </summary>
        public static object SortHelperLockObject
        {
            get
            {
                return SearchTabCollection[CurrentTab].SortHelperLockObject;
            }
        }

        public static bool FilteredResults
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilteredResults;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilteredResults = value;
            }
        }
        //public static bool FilterSticky
        //{
        //    get
        //    {
        //        return SearchTabCollection[CurrentTab].FilterSticky;
        //    }
        //    set
        //    {
        //        SearchTabCollection[CurrentTab].FilterSticky = value;
        //    }
        //}

        public static CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return SearchTabCollection[CurrentTab].CancellationTokenSource;
            }
            set
            {
                SearchTabCollection[CurrentTab].CancellationTokenSource = value;
            }
        }

        public static string FilterString
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilterString;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilterString = value;
            }
        }
        public static List<string> WordsToAvoid
        {
            get
            {
                return SearchTabCollection[CurrentTab].WordsToAvoid;
            }
            set
            {
                SearchTabCollection[CurrentTab].WordsToAvoid = value;
            }
        }
        public static List<string> WordsToInclude
        {
            get
            {
                return SearchTabCollection[CurrentTab].WordsToInclude;
            }
            set
            {
                SearchTabCollection[CurrentTab].WordsToInclude = value;
            }
        }


        public static FilterSpecialFlags FilterSpecialFlags
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilterSpecialFlags;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilterSpecialFlags = value;
            }
        }

        public static List<SearchResponse> UI_SearchResponses
        {
            get
            {
                return SearchTabCollection[CurrentTab].UI_SearchResponses;
            }
            set
            {
                SearchTabCollection[CurrentTab].UI_SearchResponses = value;
            }
        }
        public static SearchTarget SearchTarget
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTarget;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTarget = value;
            }
        }
        public static bool CurrentlySearching
        {
            get
            {
                return SearchTabCollection[CurrentTab].CurrentlySearching;
            }
            set
            {
                SearchTabCollection[CurrentTab].CurrentlySearching = value;
            }
        }


        public static string SearchTargetChosenUser
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTargetChosenUser;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTargetChosenUser = value;
            }
        }

        public static string SearchTargetChosenRoom
        {
            get
            {
                return SearchTabCollection[CurrentTab].SearchTargetChosenRoom;
            }
            set
            {
                SearchTabCollection[CurrentTab].SearchTargetChosenRoom = value;
            }
        }


    }


    public class SearchFragment : Fragment
    {
        public override void OnStart()
        {
            //this fixes the same bug as the MainActivity OnStart fixes.
            SearchFragment.Instance = this;
            base.OnStart();
        }
        public override void OnResume()
        {
            base.OnResume();
            MainActivity.LogDebug("Search Fragment On Resume");
            //you had a pending intent that could not get handled til now.
            if (MainActivity.goToSearchTab != int.MaxValue)
            {
                MainActivity.LogDebug("Search Fragment On Resume for wishlist");
                this.GoToTab(MainActivity.goToSearchTab, false, true);
                MainActivity.goToSearchTab = int.MaxValue;
            }            
        }
        public View rootView = null;
        //private SearchResponse[] cachedSearchResults = null;

        //public static SearchTabHelper OurSearchTabHelper = new SearchTabHelper();

        //public SearchFragment() : base()
        //{
        //    Instance = this;
        //}

        //these are now going to be per "tab"
        //private static List<SearchResponse> SearchResponses = new List<SearchResponse>(); //static so when the fragment gets remade we can use it
        //private static SortedDictionary<SearchResponse, object> SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparable()); //should have same lifetime as above
        //public static bool FilteredResults = false;
        //public static string FilterString = string.Empty;
        //public static List<string> WordsToAvoid = new List<string>();
        //public static List<string> WordsToInclude = new List<string>();
        //public static FilterSpecialFlags FilterSpecialFlags = new FilterSpecialFlags();
        //public static List<SearchResponse> FilteredResponses = new List<SearchResponse>();
        //public static SearchTarget SearchTarget = SearchTarget.AllUsers;
        //public static bool CurrentlySearching = false;
        //public static string SearchTargetChosenUser = string.Empty;

        public static bool FilterSticky = false;
        public static string FilterStickyString = string.Empty; //if FilterSticky is on then always use this string..

        private Context context;
        public static List<string> searchHistory = new List<string>();


        public static SearchResultStyleEnum SearchResultStyle = SearchResultStyleEnum.Medium;

        public static IMenu ActionBarMenu = null;
        public static int LastSearchResponseCount = -1;

        private void ClearFilterStringAndCached(bool force = false)
        {
            if (!FilterSticky || force)
            {
                SearchTabHelper.FilterString = string.Empty;
                SearchTabHelper.FilteredResults = this.AreChipsFiltering();
                SearchTabHelper.WordsToAvoid.Clear();
                SearchTabHelper.WordsToInclude.Clear();
                SearchTabHelper.FilterSpecialFlags.Clear();
                EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
                if(filterText.Text != string.Empty)
                {
                    //else you trigger the event.
                    filterText.Text = string.Empty;
                    UpdateDrawableState(filterText, true);
                }
                FilterStickyString = string.Empty;
            }
        }

        public static void SetSearchResultStyle(int style)
        {
            //in case its out of range bc we add / rm enums in the future...
            foreach (int i in System.Enum.GetValues(typeof(SearchResultStyleEnum)))
            {
                if (i == style)
                {
                    SearchResultStyle = (SearchResultStyleEnum)(i);
                    break;
                }
            }
        }

        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if (menuVisible)
            {
                var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (navigator != null)
                {
                    navigator.Menu.GetItem(1).SetCheckable(true);
                    navigator.Menu.GetItem(1).SetChecked(true);
                }
            }
            base.SetMenuVisibility(menuVisible);
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.search_menu, menu); //test432
            (menu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable).CrossFadeEnabled = true;
            if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(false);
            }
            else
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(true);
            }

            ActionBarMenu = menu;

            if(SearchTabHelper.CurrentlySearching)
            {
                GetTransitionDrawable().StartTransition(0);
            }
            //ActionBar actionBar = getActionBar();
            //// add the custom view to the action bar
            //actionBar.setCustomView(R.layout.actionbar_view);
            //SearchView searchView = (SearchView)(menu.FindItem(Resource.Id.action_search).ActionView);
            //menu.FindItem(Resource.Id.action_search).ExpandActionView();
            base.OnCreateOptionsMenu(menu, inflater);

            //IMenuItem searchItem = menu.FindItem(Resource.Id.action_search);


        }

        public static void SetCustomViewTabNumberInner(ImageView imgView, Context c)
        {
            int numTabs = int.MinValue;
            if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
            {
                numTabs = -1;
            }
            else
            {
                numTabs = SearchTabHelper.SearchTabCollection.Keys.Count;
            }
            int idOfDrawable = int.MinValue;
            if (numTabs > 10)
            {
                numTabs = 10;
            }
            switch (numTabs)
            {
                case 1:
                    idOfDrawable = Resource.Drawable.numeric_1_box_multiple_outline;
                    break;
                case 2:
                    idOfDrawable = Resource.Drawable.numeric_2_box_multiple_outline;
                    break;
                case 3:
                    idOfDrawable = Resource.Drawable.numeric_3_box_multiple_outline;
                    break;
                case 4:
                    idOfDrawable = Resource.Drawable.numeric_4_box_multiple_outline;
                    break;
                case 5:
                    idOfDrawable = Resource.Drawable.numeric_5_box_multiple_outline;
                    break;
                case 6:
                    idOfDrawable = Resource.Drawable.numeric_6_box_multiple_outline;
                    break;
                case 7:
                    idOfDrawable = Resource.Drawable.numeric_7_box_multiple_outline;
                    break;
                case 8:
                    idOfDrawable = Resource.Drawable.numeric_8_box_multiple_outline;
                    break;
                case 9:
                    idOfDrawable = Resource.Drawable.numeric_9_box_multiple_outline;
                    break;
                case 10:
                    idOfDrawable = Resource.Drawable.numeric_9_plus_box_multiple_outline;
                    break;
                case -1: //wishlist
                    idOfDrawable = Resource.Drawable.wishlist_icon;
                    break;
            }

            Android.Graphics.Drawables.Drawable drawable = null;
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                drawable = c.Resources.GetDrawable(idOfDrawable, c.Theme);
            }
            else
            {
                AndroidX.AppCompat.App.AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
                //the above is needed else it fails **Java.Lang.RuntimeException:** 'File res/drawable/numeric_1_box_multiple_outline.xml from drawable resource ID #0x7f0700d3'
                drawable = c.Resources.GetDrawable(idOfDrawable);
            }
            imgView.SetImageDrawable(drawable);

        }

        public void SetCustomViewTabNumberImageViewState()
        {
            ImageView imgView = null;
            Context c = null;
            if (this.Activity is Android.Support.V7.App.AppCompatActivity appCompat)
            {
                c = this.Activity;
                imgView = appCompat.SupportActionBar?.CustomView?.FindViewById<ImageView>(Resource.Id.search_tabs);
            }
            else
            {
                c = SoulSeekState.MainActivityRef;
                imgView = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<ImageView>(Resource.Id.search_tabs);
            }

            SetCustomViewTabNumberInner(imgView, c);
        }

        public EditText GetCustomViewSearchHere()
        {
            if (this.Activity is Android.Support.V7.App.AppCompatActivity appCompat)
            {
                var editText = appCompat.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                if (editText == null)
                {

                }
                return editText;
            }
            else
            {
                var editText = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
                if (editText == null)
                {

                }
                return editText;
            }
        }

        private void SetFilterState()
        {
            EditText filter = rootView.FindViewById<EditText>(Resource.Id.filterText);
            if (FilterSticky)
            {
                filter.Text = FilterStickyString;
            }
            else
            {
                filter.Text = SearchTabHelper.FilterString;
            }
            UpdateDrawableState(filter, true);
        }

        private void SetTransitionDrawableState()
        {
            if (SearchTabHelper.CurrentlySearching)
            {
                MainActivity.LogDebug("CURRENT SEARCHING SET TRANSITION DRAWABLE");
                GetTransitionDrawable().StartTransition(0);
            }
            else
            {
                GetTransitionDrawable().ResetTransition();
            }
            //forces refresh.
            ActionBarMenu.FindItem(Resource.Id.action_search).SetVisible(false);
            ActionBarMenu.FindItem(Resource.Id.action_search).SetVisible(true);
        }

        public void GoToTab(int tabToGoTo, bool force, bool fromIntent = false)
        {
            if (force || tabToGoTo != SearchTabHelper.CurrentTab)
            {
                int lastTab = SearchTabHelper.CurrentTab;
                SearchTabHelper.CurrentTab = tabToGoTo;

                //update for current tab
                //set icon state
                //set search text
                //set results
                //set filter if not sticky
                int fromTab = SearchTabHelper.CurrentTab;

                Action a = new Action(() =>
                {

                    if (!SearchTabHelper.SearchTabCollection.ContainsKey(tabToGoTo))
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.search_tab_error, ToastLength.Long).Show();
                        SearchTabHelper.CurrentTab = lastTab;
                        fromTab = lastTab;
                        return;
                    }

                    if(tabToGoTo<0)
                    {
                        if(!SearchTabHelper.SearchTabCollection[fromTab].IsLoaded())
                        {
                            SearchTabHelper.RestoreSearchResultsFromDisk(tabToGoTo, SoulSeekState.ActiveActivityRef);
                        }
                    }

                    GetCustomViewSearchHere().Text = SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm;
                    SetSearchHintTarget(SearchTabHelper.SearchTabCollection[fromTab].SearchTarget);
                    if (!fromIntent)
                    {
                        SetTransitionDrawableState();
                    }//timing issue where menu options invalidate etc. may not be done yet...
                    //bool isVisible = GetTransitionDrawable().IsVisible;
                    //GetTransitionDrawable().InvalidateSelf();
                    //(this.Activity as Android.Support.V7.App.AppCompatActivity).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).RefreshDrawableState();
                    //(this.Activity as Android.Support.V7.App.AppCompatActivity).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidateOnAnimation();
                    //(this.Activity as Android.Support.V7.App.AppCompatActivity).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidate();
                    //Handler handler = new Handler(Looper.MainLooper);
                    //handler.PostDelayed(new Action(()=> {
                    //    (this.Activity as Android.Support.V7.App.AppCompatActivity).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).PostInvalidate();
                    //    GetTransitionDrawable().InvalidateSelf();
                    //    ActionBarMenu?.FindItem(Resource.Id.action_search).SetVisible(false);
                    //    ActionBarMenu?.FindItem(Resource.Id.action_search).SetVisible(true);
                    //}), 100);

                    SetFilterState();



                    if (SearchTabHelper.SearchTabCollection[fromTab].FilteredResults)
                    {
                        if (SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount != SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count)
                        {
                            MainActivity.LogDebug("filtering...");
                            UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                        }
                        SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                        //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses); //this throws, its not ready..
                        //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        //lv.Adapter = (customAdapter);

                        recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                        recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

                    }
                    else
                    {
                        //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        //MainActivity.LogDebug("new tab refresh " + tabToGoTo + " count " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);
                        //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        //lv.Adapter = (customAdapter);
                        SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                        recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                        recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if (!fromIntent)
                    {
                        GetTransitionDrawable().InvalidateSelf();
                    }
                    this.SetCustomViewTabNumberImageViewState();
                    if (this.Activity == null)
                    {
                        GetSearchFragmentMoreDiag();
                    }
                    this.Activity.InvalidateOptionsMenu(); //this wil be the new nullref if fragment isnt ready...

                    if (!fromIntent)
                    {
                        SetTransitionDrawableState();
                    }
                });
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("mainActivityRef is null GoToTab");
                }
                SoulSeekState.MainActivityRef?.RunOnUiThread(a);
            }
        }




        public override bool OnOptionsItemSelected(IMenuItem item)
        {

            switch (item.ItemId)
            {
                case Resource.Id.action_search_target:
                    if (SearchTabHelper.SearchTarget == SearchTarget.Wishlist)
                    {
                        Toast.MakeText(this.Context, Resource.String.wishlist_tab_target, ToastLength.Long).Show();
                        return true;
                    }
                    ShowChangeTargetDialog();
                    return true;
                case Resource.Id.action_sort_results_by:
                    ShowChangeSortOrderDialog();
                    return true;
                case Resource.Id.action_search:
                    if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
                    {
                        MainActivity.LogDebug("transitionDrawable: REVERSE transition");
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                        SearchTabHelper.CancellationTokenSource.Cancel();
                        SearchTabHelper.CurrentlySearching = false;
                        return true;
                    }
                    else
                    {
                        (item.Icon as Android.Graphics.Drawables.TransitionDrawable).StartTransition(SearchToCloseDuration);
                        PerformBackUpRefresh();
                        MainActivity.LogDebug("START TRANSITION");
                        SearchTabHelper.CurrentlySearching = true;
                        SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
                        EditText editText = SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                        string searchText = string.Empty;
                        if (editText == null)
                        {
                            searchText = SearchingText;
                        }
                        else
                        {
                            searchText = editText.Text;
                        }
                        SearchAPI(SearchTabHelper.CancellationTokenSource.Token, (item.Icon as Android.Graphics.Drawables.TransitionDrawable), searchText, SearchTabHelper.CurrentTab);
                        return true;
                    }
                case Resource.Id.action_change_result_style:
                    ShowChangeResultStyleBottomDialog();
                    return true;
                case Resource.Id.action_add_to_wishlist:
                    AddSearchToWishlist();
                    return true;
                    //case Resource.Id.action_view_search_tabs:
                    //    ShowSearchTabsDialog();
                    //    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void AddSearchToWishlist()
        {
            //here we "fork" the current search, adding it to the wishlist
            if (SearchTabHelper.LastSearchTerm == string.Empty || SearchTabHelper.LastSearchTerm == null)
            {
                Toast.MakeText(this.Context, Resource.String.perform_search_first, ToastLength.Long).Show();
                return;
            }
            SearchTabHelper.AddWishlistSearchTabFromCurrent();
            Toast.MakeText(this.Context, string.Format(this.Context.GetString(Resource.String.added_to_wishlist), SearchTabHelper.LastSearchTerm), ToastLength.Long).Show();
            this.SetCustomViewTabNumberImageViewState();
        }

        public static SearchFragment GetSearchFragment()
        {
            foreach (Fragment frag in SoulSeekState.MainActivityRef.SupportFragmentManager.Fragments)
            {
                if (frag is SearchFragment sfrag)
                {
                    return sfrag;
                }
            }
            return null;
        }

        public static SearchFragment GetSearchFragmentMoreDiag()
        {
            if (SoulSeekState.ActiveActivityRef is MainActivity)
            {
                MainActivity.LogInfoFirebase("current activity is Main");
            }
            else
            {
                MainActivity.LogInfoFirebase("current activity is NOT Main");
            }
            foreach (Fragment frag in SoulSeekState.MainActivityRef.SupportFragmentManager.Fragments)
            {
                if (frag is SearchFragment sfrag)
                {
                    MainActivity.LogInfoFirebase("yes search fragment,  isAdded: " + sfrag.IsAdded);
                    return sfrag;
                }
            }
            MainActivity.LogInfoFirebase("no search fragment.");
            return null;
        }

        public void ShowSearchTabsDialog()
        {
            SearchTabDialog searchTabDialog = new SearchTabDialog();
            //bool isAdded = (((SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager) as Android.Support.V4.View.ViewPager).Adapter as TabsPagerAdapter).GetItem(1) as SearchFragment).IsAdded; //this is EXTREMELY stale
            if (!this.IsAdded || this.Activity == null) //then child fragment manager will likely be null
            {
                MainActivity.LogInfoFirebase("ShowSearchTabsDialog, fragment no longer attached...");

                //foreach(Fragment frag in SoulSeekState.MainActivityRef.SupportFragmentManager.Fragments)
                //{
                //    if(frag is SearchFragment sfrag)
                //    {
                //        bool isAdded = sfrag.IsAdded;
                //    }
                //}

                searchTabDialog.Show(SoulSeekState.MainActivityRef.SupportFragmentManager, "search tab dialog");
                //I tested this many times (outside of this clause).  Works very well.x
                //But I dont know if not attached fragment will just cause other issues later on... yes it will as for example adding a new search tab, there are methods that rely on this.Activity and this.rootView etc.
                return;
            }
            searchTabDialog.Show(this.ChildFragmentManager, "search tab dialog");
        }

        public static void UpdateDrawableState(EditText actv, bool purple = false)
        {
            if (actv.Text == string.Empty || actv.Text == null)
            {
                actv.SetCompoundDrawables(null, null, null, null);
            }
            else
            {
                var cancel = ContextCompat.GetDrawable(SoulSeekState.MainActivityRef, Resource.Drawable.ic_cancel_black_24dp);
                cancel.SetBounds(0, 0, cancel.IntrinsicWidth, cancel.IntrinsicHeight);
                if (purple)
                {
                    //https://developer.android.com/reference/android/graphics/PorterDuff.Mode
                    cancel.SetColorFilter(SearchItemViewExpandable.GetColorFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.mainTextColor), PorterDuff.Mode.SrcAtop);
                }
                actv.SetCompoundDrawables(null, null, cancel, null);
            }
        }


        public static void ConfigureSupportCustomView(View customView/*, Context contextJustInCase*/) //todo: seems to be an error. which seems entirely possible. where ActiveActivityRef does not get set yet.
        {
            MainActivity.LogDebug("ConfigureSupportCustomView");
            AutoCompleteTextView actv = customView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            try
            {
                actv.Text = SearchingText; //this works with string.Empty and emojis so I dont think its here...
                UpdateDrawableState(actv);
                actv.Touch += Actv_Touch;
                //ContextCompat.GetDrawable(SoulSeekState.MainActivityRef,Resource.Drawable.ic_cancel_black_24dp);
            }
            catch (System.ArgumentException e)
            {
                MainActivity.LogFirebase("ArugmentException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch (System.Exception e)
            {
                MainActivity.LogFirebase("catchException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch
            {
                MainActivity.LogFirebase("catchunspecException Value does not fall within range: " + SearchingText);
            }
            ImageView iv = customView.FindViewById<ImageView>(Resource.Id.search_tabs);
            iv.Click += Iv_Click;
            actv.EditorAction -= Search_EditorActionHELPER;
            actv.EditorAction += Search_EditorActionHELPER;
            string searchHistoryXML = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchHistory, string.Empty);
            if (searchHistory == null || searchHistory.Count == 0) // i think we just have to deserialize once??
            {
                if (searchHistoryXML == string.Empty)
                {
                    searchHistory = new List<string>();
                }
                else
                {
                    using (var stream = new System.IO.StringReader(searchHistoryXML))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType()); //this happens too often not allowing new things to be properly stored..
                        searchHistory = serializer.Deserialize(stream) as List<string>;
                    }
                    //noTransfers.Visibility = ViewStates.Gone;
                }

            }

            SetSearchHintTarget(SearchTabHelper.SearchTarget, actv);

            Context contextToUse = SoulSeekState.ActiveActivityRef;
            //if (SoulSeekState.ActiveActivityRef==null)
            //{
            //    MainActivity.LogFirebase("Active ActivityRef is null!!!");
            //    //contextToUse = contextJustInCase;
            //}
            //else
            //{
            //    contextToUse = SoulSeekState.ActiveActivityRef;
            //}

            actv.Adapter = new ArrayAdapter<string>(contextToUse, Resource.Layout.autoSuggestionRow, searchHistory);
            actv.KeyPress -= Actv_KeyPressHELPER;
            actv.KeyPress += Actv_KeyPressHELPER;
            actv.FocusChange += MainActivity_FocusChange;
            actv.TextChanged += Actv_TextChanged;

            SetCustomViewTabNumberInner(iv, contextToUse);
        }

        private static void Actv_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if (e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if (e.Event.Action == MotionEventActions.Up)
                {
                    //e.Handled = true;
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText);
                    //editText.RequestFocus();
                }
            }
        }

        private static void Actv_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static string SearchingText = string.Empty;

        private static void Actv_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            //if it went from non empty to empty, or vice versa
            if (SearchingText == string.Empty && e.Text.ToString() != string.Empty)
            {
                UpdateDrawableState(sender as EditText);
            }
            else if (SearchingText != string.Empty && e.Text.ToString() == string.Empty)
            {
                UpdateDrawableState(sender as EditText);
            }
            SearchingText = e.Text.ToString();
        }

        public static void MainActivity_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SoulSeekState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        public void SearchResultStyleChanged()
        {
            //notify changed isnt enough if the xml is different... it is enough in the case of expandAll to collapseAll tho..

            //(rootView.FindViewById<ListView>(Resource.Id.listView1).Adapter as SearchAdapter).NotifyDataSetChanged();
            RecyclerView rv = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches); //TODO //TODO //TODO

            if (SearchTabHelper.FilteredResults)
            {
                SearchAdapterRecyclerVersion customAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                rv.SetAdapter(customAdapter);
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                SearchAdapterRecyclerVersion customAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                rv.SetAdapter(customAdapter);
            }

            //(rootView.FindViewById<ListView>(Resource.Id.listView1).Adapter as SearchAdapter)
        }

        public class BSDF_Menu : BottomSheetDialogFragment
        {
            //public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
            //{
            //    inflater.Inflate(Resource.Menu.transfers_menu, menu);
            //    base.OnCreateOptionsMenu(menu, inflater);
            //}

            //public override int Theme => Resource.Style.BottomSheetDialogTheme;

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {

                //return base.OnCreateView(inflater, container, savedInstanceState);
                View rootView = inflater.Inflate(Resource.Layout.search_results_expandablexml, container);
                RadioGroup resultStyleRadioGroup = rootView.FindViewById<RadioGroup>(Resource.Id.radioGroup);



                switch (SearchFragment.SearchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonExpanded);
                        break;
                    case SearchResultStyleEnum.CollapsedAll:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonCollapsed);
                        break;
                    case SearchResultStyleEnum.Medium:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMedium);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        resultStyleRadioGroup.Check(Resource.Id.radioButtonMinimal);
                        break;
                }
                resultStyleRadioGroup.CheckedChange += ResultStyleRadioGroup_CheckedChange;
                return rootView;
            }

            private void ResultStyleRadioGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
            {
                //RadioButton checkedRadioButton = (RadioButton)(sender as View).FindViewById(e.CheckedId);
                var prev = SearchFragment.SearchResultStyle;
                switch (e.CheckedId)
                {
                    case Resource.Id.radioButtonExpanded:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.ExpandedAll;
                        break;
                    case Resource.Id.radioButtonCollapsed:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.CollapsedAll;
                        break;
                    case Resource.Id.radioButtonMedium:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Medium;
                        break;
                    case Resource.Id.radioButtonMinimal:
                        SearchFragment.SearchResultStyle = SearchResultStyleEnum.Minimal;
                        break;
                }
                if (prev != SearchFragment.SearchResultStyle)
                {
                    SearchFragment.Instance.SearchResultStyleChanged();
                }
                this.Dismiss();
            }

            //public override int Theme => Resource.Style.MyCustomTheme; //for rounded corners...
        }


        private static void Iv_Click(object sender, EventArgs e)
        {
            //SearchFragment.Instance = GetSearchFragment(); //tested this and it works!
            if (!SearchFragment.Instance.IsAdded)
            {
                SearchFragment f = GetSearchFragment(); //is there an attached fragment?? i.e. is our instance just a stale one..
                if (f == null)
                {
                    MainActivity.LogInfoFirebase("search fragment not on activities fragment manager");
                }
                else if (!f.IsAdded)
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is not added");
                }
                else
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is good, though not setting it");
                    //SearchFragment.Instance = f; 
                }
                MainActivity.LogFirebase("SearchFragment.Instance.IsAdded == false, currently searching: " + SearchTabHelper.CurrentlySearching);
            }
            //try
            //{
            SearchFragment.Instance.ShowSearchTabsDialog();
            //}
            //catch(Java.Lang.Exception ex)
            //{
            //    string currentMainState = SoulSeekState.MainActivityRef.Lifecycle.CurrentState.ToString();
            //    string currentFragState = SearchFragment.Instance.Lifecycle.CurrentState.ToString();
            //    string diagMessage = string.Format("Last Stopped: {0} Last Started: {1} currentMainState: {2} currentFragState: {3}", ForegroundLifecycleTracker.DiagLastStopped,ForegroundLifecycleTracker.DiagLastStarted, currentMainState, currentFragState);
            //    System.Exception diagException = new System.Exception(diagMessage);
            //    throw diagException;
            //}
            //catch(System.Exception ex)
            //{
            //    string currentMainState = SoulSeekState.MainActivityRef.Lifecycle.CurrentState.ToString();
            //    string currentFragState = SearchFragment.Instance.Lifecycle.CurrentState.ToString();
            //    string diagMessage = string.Format("Last Stopped: {0} Last Started: {1} currentMainState: {2} currentFragState: {3}", ForegroundLifecycleTracker.DiagLastStopped, ForegroundLifecycleTracker.DiagLastStarted, currentMainState, currentFragState);
            //    System.Exception diagException = new System.Exception(diagMessage, ex);
            //    throw diagException;
            //}
        }

        public static void ShowChangeResultStyleBottomDialog()
        {
            BSDF_Menu bsdf = new BSDF_Menu();
            bsdf.HasOptionsMenu = true;
            bsdf.ShowNow(SoulSeekState.MainActivityRef.SupportFragmentManager, "options");
        }

        public static volatile SearchFragment Instance = null;
        public RecyclerView recyclerViewChips;
        public ChipsItemRecyclerAdapter recyclerChipsAdapter;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            Instance = this;
            HasOptionsMenu = true;
            //SoulSeekState.MainActivityRef.SupportActionBar.SetDisplayShowCustomEnabled(true);
            //SoulSeekState.MainActivityRef.SupportActionBar.SetCustomView(Resource.Layout.custom_menu_layout);//FindViewById< Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar).(Resource.Layout.custom_menu_layout);
            MainActivity.LogDebug("SearchFragmentOnCreateView");
            MainActivity.LogDebug("SearchFragmentOnCreateView - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);
            this.rootView = inflater.Inflate(Resource.Layout.searches, container, false);
            UpdateForScreenSize();

            //Button search = rootView.FindViewById<Button>(Resource.Id.button2);
            //search.Click -= Search_Click;
            //search.Click += Search_Click;

            context = this.Context;

            //note: changing from AutoCompleteTextView to EditText fixes both the hardware keyboard issue, and the backspace issue.

            //this.listView = rootView.FindViewById<ListView>(Resource.Id.listView1);

            recyclerViewChips = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewChips);
            //if(SoulSeekState.ShowSmartFilters)
            //{
            recyclerViewChips.Visibility = ViewStates.Visible;
            //}
            //else
            //{
            //    recyclerViewChips.Visibility = ViewStates.Gone;
            //}

            var manager = new LinearLayoutManager(this.Context, LinearLayoutManager.Horizontal, false);
            recyclerViewChips.SetItemAnimator(null);
            recyclerViewChips.SetLayoutManager(manager);
            recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
            recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);

            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            bsb.Hideable = true;
            bsb.PeekHeight = 320;
            bsb.State = BottomSheetBehavior.StateHidden;

            CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
            filterSticky.Checked = FilterSticky;
            filterSticky.CheckedChange += FilterSticky_CheckedChange;

            //bsb.SetBottomSheetCallback(new MyCallback());
            View b = rootView.FindViewById<View>(Resource.Id.bsbutton);
            (b as FloatingActionButton).SetImageResource(Resource.Drawable.ic_filter_list_white_24dp);
            View v = rootView.FindViewById<View>(Resource.Id.focusableLayout);
            v.Focusable = true;
            //SetFocusable(int) was added in API26. bool was there since API1
            if ((int)Android.OS.Build.VERSION.SdkInt >= 26)
            {
                v.SetFocusable(ViewFocusability.Focusable);
            }
            else
            {
                //v.SetFocusable(true); no bool method in xamarin...
            }

            v.FocusableInTouchMode = true;
            //b.Focusable = true;
            //b.SetFocusable(ViewFocusability.Focusable);
            //b.FocusableInTouchMode = true;
            b.Click += B_Click;

            //Button clearFilter = rootView.FindViewById<Button>(Resource.Id.clearFilter);
            //clearFilter.Click += ClearFilter_Click;

            string searchHistoryXML = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchHistory, string.Empty);
            if (searchHistory == null || searchHistory.Count == 0) // i think we just have to deserialize once??
            {
                if (searchHistoryXML == string.Empty)
                {
                    searchHistory = new List<string>();
                }
                else
                {
                    using (var stream = new System.IO.StringReader(searchHistoryXML))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType()); //this happens too often not allowing new things to be properly stored..
                        searchHistory = serializer.Deserialize(stream) as List<string>;
                    }
                    //noTransfers.Visibility = ViewStates.Gone;
                }
            }

            //actv.Adapter = new ArrayAdapter<string>(context, Resource.Layout.autoSuggestionRow, searchHistory);
            //actv.KeyPress -= Actv_KeyPress;
            //actv.KeyPress += Actv_KeyPress;

            //List<SearchResponse> rowItems = new List<SearchResponse>();
            //if (SearchTabHelper.FilteredResults)
            //{
            //    SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.FilteredResponses);
            //    listView.Adapter = (customAdapter);
            //}
            //else
            //{
            //    SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.SearchResponses);
            //    listView.Adapter = (customAdapter);
            //}


            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerViewSearches);
            recycleLayoutManager = new LinearLayoutManager(Activity);
            recyclerViewTransferItems.SetItemAnimator(null); //todo
            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);
            if (SearchTabHelper.FilteredResults)
            {
                recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                //CustomAdapter customAdapter = new CustomAdapter(Context, FilteredResponses);
                //lv.Adapter = (customAdapter);
            }
            else
            {
                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses.ToList();
                recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                //CustomAdapter customAdapter = new CustomAdapter(Context, SearchResponses);
                //lv.Adapter = (customAdapter);
            }


            //listView.ItemClick -= Lv_ItemClick;
            //listView.ItemClick += Lv_ItemClick;
            //listView.Clickable = true;
            //listView.Focusable = true;
            SoulSeekState.ClearSearchHistoryEventsFromTarget(this);
            SoulSeekState.ClearSearchHistory += SoulSeekState_ClearSearchHistory;
            SoulSeekState.SoulseekClient.ClearSearchResponseReceivedFromTarget(this);
            //SoulSeekState.SoulseekClient.SearchResponseReceived -= SoulseekClient_SearchResponseReceived;
            int x = SoulSeekState.SoulseekClient.GetInvocationListOfSearchResponseReceived();
            MainActivity.LogDebug("NUMBER OF DELEGATES AFTER WE REMOVED OURSELF: (before doing the deep clear this would increase every rotation orientation)" + x);
            //SoulSeekState.SoulseekClient.SearchResponseReceived += SoulseekClient_SearchResponseReceived;
            MainActivity.LogDebug("SearchFragmentOnCreateViewEnd - SearchResponses.Count=" + SearchTabHelper.SearchResponses.Count);

            EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
            filterText.TextChanged += FilterText_TextChanged;
            filterText.FocusChange += FilterText_FocusChange;
            filterText.EditorAction += FilterText_EditorAction;
            filterText.Touch += FilterText_Touch;
            if (FilterSticky)
            {
                filterText.Text = FilterStickyString;
            }
            UpdateDrawableState(filterText, true);

            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = SoulSeekState.ShowSmartFilters ? "Hide Smart Filters" : "Show Smart Filters";
            showHideSmartFilters.Click += ShowHideSmartFilters_Click;

            return rootView;
        }

        private void ShowHideSmartFilters_Click(object sender, EventArgs e)
        {
            SoulSeekState.ShowSmartFilters = !SoulSeekState.ShowSmartFilters;
            Button showHideSmartFilters = rootView.FindViewById<Button>(Resource.Id.toggleSmartFilters);
            showHideSmartFilters.Text = SoulSeekState.ShowSmartFilters ? "Hide Smart Filters" : "Show Smart Filters";
            if (SoulSeekState.ShowSmartFilters)
            {
                if (SearchTabHelper.CurrentlySearching)
                {
                    return; //it will update on complete search
                }
                if ((SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses?.Count ?? 0) != 0)
                {
                    List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].SearchResponses, SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].LastSearchTerm, SoulSeekState.SmartFilterOptions);
                    SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = chipDataItems;
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
                        SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                    }));
                }
            }
            else
            {
                SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = null;
                SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipsFilter = null; //in case there was previously a filter
                SearchFragment.Instance.RefreshOnChipChanged();
                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(null);
                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
            }
        }

        private void FilterText_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if (e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if (e.Event.Action == MotionEventActions.Up)
                {
                    //e.Handled = true;
                    editText.Text = string.Empty;
                    UpdateDrawableState(editText, true);

                    ClearFilterStringAndCached(true);
                    //editText.RequestFocus();
                }
            }
        }

        /// <summary>
        /// Are chips filtering out results..
        /// </summary>
        /// <returns></returns>
        private bool AreChipsFiltering()
        {
            if (!SoulSeekState.ShowSmartFilters || (SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems?.Count ?? 0) == 0)
            {
                return false;
            }
            else
            {
                return SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems.Any(i => i.IsChecked);
            }
        }

        private void FilterText_FocusChange(object sender, View.FocusChangeEventArgs e)
        {
            try
            {
                SoulSeekState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustResize);
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("MainActivity_FocusChange" + err.Message);
            }
        }

        //private void ClearFilter_Click(object sender, EventArgs e)
        //{
        //    CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
        //    filterSticky.Checked = false;
        //    ClearFilterStringAndCached(true);
        //}

        private void B_Click(object sender, EventArgs e)
        {
            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            if (bsb.State != BottomSheetBehavior.StateExpanded && bsb.State != BottomSheetBehavior.StateCollapsed)
            {
                bsb.State = BottomSheetBehavior.StateExpanded;
            }
            else
            {
                //if the keyboard is up and the edittext is in focus then maybe just put the keyboard down
                //else put the bottom sheet down.  
                //so make it two tiered.
                //or maybe just unset the focus...
                EditText test = rootView.FindViewById<EditText>(Resource.Id.filterText);
                //Android.Views.InputMethods.InputMethodManager IMM = context.GetSystemService(Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
                //Rect outRect = new Rect();
                //this.rootView.GetWindowVisibleDisplayFrame(outRect);
                //MainActivity.LogDebug("Window Visible Display Frame " + outRect.Height());
                //MainActivity.LogDebug("Actual Height " + this.rootView.Height);
                //Type immType = IMM.GetType();

                //MainActivity.LogDebug("Y Position " + rel.GetY());
                //int[] location = new int[2];
                //rel.GetLocationOnScreen(location);
                //MainActivity.LogDebug("X Pos: " + location[0] + "  Y Pos: " + location[1]);
                //var method = immType.GetProperty("InputMethodWindowVisibleHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                //foreach (var prop in immType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                //{
                //    MainActivity.LogDebug(string.Format("Property Name: {0}", prop.Name));
                //}
                //foreach(var meth in immType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                //{
                //    MainActivity.LogDebug(string.Format("Property Name: {0}", meth.Name));
                //}

                MainActivity.LogDebug(this.Resources.Configuration.HardKeyboardHidden.ToString()); //on pixel2 it is YES. on emulator with HW Keyboard = true it is NO

                if (test.IsFocused && (this.Resources.Configuration.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.Yes)) //it can still be focused without the keyboard up...
                {
                    try
                    {

                        //SoulSeekState.MainActivityRef.DispatchKeyEvent(new KeyEvent(new KeyEventActions(),Keycode.Enter));
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)Context.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                        test.ClearFocus();
                        rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    }
                    catch
                    {
                        //not worth throwing over
                    }
                    return;
                }
                else if (test.IsFocused && (this.Resources.Configuration.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.No))
                {

                    //we still want to change focus as otherwise one can still type into it...
                    test.ClearFocus();
                    rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    bsb.State = BottomSheetBehavior.StateHidden;

                }
                //test.ClearFocus(); //doesnt do anything. //maybe focus the search text.

                bsb.State = BottomSheetBehavior.StateHidden;
            }
        }

        private void FilterSticky_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            FilterSticky = e.IsChecked;
            if (FilterSticky)
            {
                FilterStickyString = SearchTabHelper.FilterString;
            }
        }

        private static void Search_EditorActionHELPER(object sender, TextView.EditorActionEventArgs e)
        {
            //bool x = SoulSeekState.MainActivityRef.IsDestroyed;

            //SearchFragment searchFragment = ((SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager) as Android.Support.V4.View.ViewPager).Adapter as TabsPagerAdapter).GetItem(1) as SearchFragment;
            SearchFragment.Instance.Search_EditorAction(sender, e);
        }

        private void Search_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            {
                string editSearchText = null;
                EditText editTextSearch = SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere); //get asap to avoid nullref...
                if (editTextSearch == null)
                {
                    EditText searchHere = (this.Activity as Android.Support.V7.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                    if (searchHere != null)
                    {
                        //MainActivity.LogFirebase("editTextSearch is NULL only on cached activity");//these are both real cases that occur
                        editSearchText = searchHere.Text;
                    }
                    else
                    {
                        //MainActivity.LogFirebase("editTextSearch is NULL from both cached and MainActivity"); //these are both real cases that occur
                        editSearchText = SearchingText;
                    }
                }
                else
                {
                    editSearchText = editTextSearch.Text;
                }
                MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                //overriding this, the keyboard fails to go down by default for some reason.....
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                }
                catch (System.Exception ex)
                {
                    MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                }
                var transitionDrawable = GetTransitionDrawable();
                if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
                {
                    MainActivity.LogDebug("transitionDrawable: reverse transition");
                    transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                    SearchTabHelper.CancellationTokenSource.Cancel();
                    SearchTabHelper.CurrentlySearching = false;
                }
                else
                {
                    MainActivity.LogDebug("transitionDrawable: start transition");
                    transitionDrawable.StartTransition(SearchToCloseDuration);
                    PerformBackUpRefresh();
                    SearchTabHelper.CurrentlySearching = true;
                }
                SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
                SearchAPI(SearchTabHelper.CancellationTokenSource.Token, transitionDrawable, editSearchText, SearchTabHelper.CurrentTab);
                (sender as AutoCompleteTextView).DismissDropDown();
            }
        }

        private void ChooseUserInput_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            SearchTabHelper.SearchTargetChosenUser = e.Text.ToString();
        }

        private void AllUsers_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.AllUsers;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.AllUsers);
        }

        private void ChosenUser_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Visible;
            SetSearchHintTarget(SearchTarget.ChosenUser);
        }

        private void UserList_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTarget = SearchTarget.UserList;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
            chooseUserInput.Visibility = ViewStates.Gone;
            SetSearchHintTarget(SearchTarget.UserList);
        }

        public static void SetSearchHintTarget(SearchTarget target, AutoCompleteTextView actv = null)
        {
            if (actv == null)
            {
                actv = SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            }
            if (actv != null)
            {
                switch (target)
                {
                    case SearchTarget.AllUsers:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.search_here);
                        break;
                    case SearchTarget.UserList:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.saerch_user_list);
                        break;
                    case SearchTarget.Room:
                        actv.Hint = string.Format(SeekerApplication.ApplicationContext.GetString(Resource.String.search_room_), SearchTabHelper.SearchTargetChosenRoom);
                        break;
                    case SearchTarget.ChosenUser:
                        actv.Hint = string.Format(SeekerApplication.ApplicationContext.GetString(Resource.String.search_user_), SearchTabHelper.SearchTargetChosenUser);
                        break;
                    case SearchTarget.Wishlist:
                        actv.Hint = SeekerApplication.ApplicationContext.GetString(Resource.String.wishlist_search);
                        break;
                }
            }
        }

        public Android.Graphics.Drawables.TransitionDrawable GetTransitionDrawable()
        {
            Android.Graphics.Drawables.TransitionDrawable icon = ActionBarMenu?.FindItem(Resource.Id.action_search)?.Icon as Android.Graphics.Drawables.TransitionDrawable;
            //tested this and it works well
            if (icon == null)
            {
                if (this.Activity == null)
                {
                    MainActivity.LogInfoFirebase("GetTransitionDrawable activity is null");
                    SearchFragment f = GetSearchFragment();
                    if (f == null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable no search fragment attached to activity");
                    }
                    else if (!f.IsAdded)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable attached but not added");
                    }
                    else if (f.Activity == null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable f.Activity activity is null");
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("we should be using the fragment manager one...");
                    }
                }
                //when coming from an intent its actually (toolbar.Menu.FindItem(Resource.Id.action_search)) that is null.  so the menu is there, just no action_search menu item.
                Android.Support.V7.Widget.Toolbar toolbar = (this.Activity as Android.Support.V7.App.AppCompatActivity).FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                return toolbar.Menu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable;
            }
            else
            {
                return icon;
            }
            //return ActionBarMenu.FindItem(Resource.Id.action_search).Icon as Android.Graphics.Drawables.TransitionDrawable; // we got nullref here...

        }

        public static void ClearFocusSearchEditText()
        {
            SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<View>(Resource.Id.searchHere)?.ClearFocus();
        }



        private void SetRoomSpinnerAndEditTextInitial(Spinner s, EditText custom)
        {
            if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
            {
                s.SetSelection(0);
            }
            else
            {
                bool found = false;
                for (int i = 0; i < s.Adapter.Count; i++)
                {
                    if ((string)(s.GetItemAtPosition(i)) == SearchTabHelper.SearchTargetChosenRoom)
                    {
                        found = true;
                        s.SetSelection(i);
                        custom.Text = string.Empty;
                        break;
                    }
                }
                if (!found)
                {
                    s.SetSelection(s.Adapter.Count - 1);
                    custom.Text = SearchTabHelper.SearchTargetChosenRoom;
                }
            }
        }

        public void ShowChangeSortOrderDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SoulSeekState.MainActivityRef;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(toUse, Resource.Style.MyAlertDialogTheme); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.sort_results_by_);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeresultsortorder, this.rootView as ViewGroup, false); //TODO replace rootView with ActiveActivity.GetContent()

            AndroidX.AppCompat.Widget.AppCompatRadioButton sortAvailability = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.availability);
            AndroidX.AppCompat.Widget.AppCompatRadioButton sortSpeed = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.speed);
            AndroidX.AppCompat.Widget.AppCompatRadioButton sortFolderNameAlpha = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.folderNameAlpha);
            CheckBox checkBoxSetAsDefault = viewInflated.FindViewById<CheckBox>(Resource.Id.setAsDefault);
            switch (SearchTabHelper.SortHelperSorting)
            {
                case SearchResultSorting.Available:
                    sortAvailability.Checked = true;
                    break;
                case SearchResultSorting.Fastest:
                    sortSpeed.Checked = true;
                    break;
                case SearchResultSorting.FolderAlphabetical:
                    sortFolderNameAlpha.Checked = true;
                    break;
            }

            sortAvailability.Click += SortAvailabilityClick;
            sortSpeed.Click += SortSpeedClick;
            sortFolderNameAlpha.Click += SortFoldernameAlphaClick;

            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> positiveButtonEventHandler = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                //if cancelled via back button we dont go here
                
                if(checkBoxSetAsDefault.Checked)
                {
                    var old = SoulSeekState.DefaultSearchResultSortAlgorithm;
                    SoulSeekState.DefaultSearchResultSortAlgorithm = SearchTabHelper.SortHelperSorting; //whatever one we just changed it to.
                    if(old != SoulSeekState.DefaultSearchResultSortAlgorithm)
                    {
                        lock (MainActivity.SHARED_PREF_LOCK)
                        {
                            var editor = SoulSeekState.SharedPreferences.Edit();
                            editor.PutInt(SoulSeekState.M_DefaultSearchResultSortAlgorithm, (int)SoulSeekState.DefaultSearchResultSortAlgorithm);
                            editor.Commit();
                        }
                    }
                }
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });

            builder.SetPositiveButton(Resource.String.okay, positiveButtonEventHandler);
            dialogInstance = builder.Create();
            dialogInstance.Show();
        }
        private void SortAvailabilityClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.Available);
        }

        private void SortSpeedClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.Fastest);
        }

        private void SortFoldernameAlphaClick(object sender, EventArgs e)
        {
            UpdateSortAvailability(SearchResultSorting.FolderAlphabetical);
        }

        private void UpdateSortAvailability(SearchResultSorting searchResultSorting)
        {
            if(SearchTabHelper.SortHelperSorting != searchResultSorting)
            {
                lock (SearchTabHelper.SortHelperLockObject) //this is also always going to be on the UI thread. so we have that guaranteeing safety. 
                {
                    SearchTabHelper.SortHelperSorting = searchResultSorting;
                    SearchTabHelper.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist(SearchTabHelper.SortHelperSorting));

                    //put all the search responses into the new sort helper
                    if(SearchTabHelper.SearchResponses!=null)
                    {
                        foreach(var searchResponse in SearchTabHelper.SearchResponses)
                        {
                            if (!SearchTabHelper.SortHelper.ContainsKey(searchResponse))
                            {
                                SearchTabHelper.SortHelper.Add(searchResponse, null);
                            }
                            else
                            {

                            }
                        }
                    }

                    //now that they are sorted, replace them.
                    SearchTabHelper.SearchResponses = SearchTabHelper.SortHelper.Keys.ToList();

                    if(SearchTabHelper.FilteredResults)
                    {
                        UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                        recyclerSearchAdapter.NotifyDataSetChanged();
                    }
                    else
                    {
                        SearchTabHelper.UI_SearchResponses.Clear();
                        SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                        recyclerSearchAdapter.NotifyDataSetChanged();
                    }

                    

                }
            }
        }





        private AutoCompleteTextView chooseUserInput = null;
        private EditText customRoomName = null;
        private Spinner roomListSpinner = null;
        private LinearLayout targetRoomLayout = null;
        public void ShowChangeTargetDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SoulSeekState.MainActivityRef;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(toUse, Resource.Style.MyAlertDialogTheme); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.search_target_);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeusertarget, this.rootView as ViewGroup, false);
            chooseUserInput = viewInflated.FindViewById<AutoCompleteTextView>(Resource.Id.chosenUserInput);
            SeekerApplication.SetupRecentUserAutoCompleteTextView(chooseUserInput);
            customRoomName = viewInflated.FindViewById<EditText>(Resource.Id.customRoomName);
            targetRoomLayout = viewInflated.FindViewById<LinearLayout>(Resource.Id.targetRoomLayout);
            roomListSpinner = viewInflated.FindViewById<Spinner>(Resource.Id.roomListSpinner);

            AndroidX.AppCompat.Widget.AppCompatRadioButton allUsers = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.allUsers);
            AndroidX.AppCompat.Widget.AppCompatRadioButton chosenUser = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.chosenUser);
            AndroidX.AppCompat.Widget.AppCompatRadioButton userList = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetUserList);
            AndroidX.AppCompat.Widget.AppCompatRadioButton room = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetRoom);
            List<string> possibleRooms = new List<string>();
            if (ChatroomController.JoinedRoomNames != null && ChatroomController.JoinedRoomNames.Count != 0)
            {
                possibleRooms = ChatroomController.JoinedRoomNames.ToList();
            }
            possibleRooms.Add(SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_));
            roomListSpinner.Adapter = new ArrayAdapter<string>(SoulSeekState.ActiveActivityRef, Resource.Layout.support_simple_spinner_dropdown_item, possibleRooms.ToArray());
            SetRoomSpinnerAndEditTextInitial(roomListSpinner, customRoomName);
            chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
            switch (SearchTabHelper.SearchTarget)
            {
                case SearchTarget.AllUsers:
                    allUsers.Checked = true;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.UserList:
                    userList.Checked = true;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    break;
                case SearchTarget.ChosenUser:
                    chosenUser.Checked = true;
                    targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Gone;
                    chooseUserInput.Visibility = ViewStates.Visible;
                    chooseUserInput.Text = SearchTabHelper.SearchTargetChosenUser;
                    break;
                case SearchTarget.Room:
                    room.Checked = true;
                    chooseUserInput.Visibility = ViewStates.Gone;
                    targetRoomLayout.Visibility = ViewStates.Visible;
                    if (roomListSpinner.SelectedItem.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_))
                    {
                        customRoomName.Visibility = ViewStates.Visible;
                        customRoomName.Text = SearchTabHelper.SearchTargetChosenRoom;
                    }
                    break;
            }

            allUsers.Click += AllUsers_Click;
            room.Click += Room_Click;
            chosenUser.Click += ChosenUser_Click;
            first = true;
            roomListSpinner.ItemSelected += RoomListSpinner_ItemSelected;
            userList.Click += UserList_Click;
            chooseUserInput.TextChanged += ChooseUserInput_TextChanged;
            customRoomName.TextChanged += CustomRoomName_TextChanged;


            builder.SetView(viewInflated);

            EventHandler<DialogClickEventArgs> eventHandlerClose = new EventHandler<DialogClickEventArgs>((object sender, DialogClickEventArgs cancelArgs) =>
            {
                SetSearchHintTarget(SearchTabHelper.SearchTarget, (this.Activity as Android.Support.V7.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere)); //in case of hitting choose user, you still have to update the name (since that gets input after clicking radio button)...
                if (SearchTabHelper.SearchTarget == SearchTarget.ChosenUser && !string.IsNullOrEmpty(SearchTabHelper.SearchTargetChosenUser))
                {
                    SoulSeekState.RecentUsersManager.AddUserToTop(SearchTabHelper.SearchTargetChosenUser, true);
                }
                if (sender is AndroidX.AppCompat.App.AlertDialog aDiag)
                {
                    aDiag.Dismiss();
                }
                else
                {
                    dialogInstance.Dismiss();
                }
            });

            System.EventHandler<TextView.EditorActionEventArgs> editorAction = (object sender, TextView.EditorActionEventArgs e) =>
            {
                if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                    e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                    e.ActionId == Android.Views.InputMethods.ImeAction.Search) //i get a lot of imenull..
                {
                    MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                    //rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                    //rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                    //overriding this, the keyboard fails to go down by default for some reason.....
                    try
                    {
                        Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
                        imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                    }
                    catch (System.Exception ex)
                    {
                        MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                    }
                    eventHandlerClose(sender, null);
                }
            };

            chooseUserInput.EditorAction += editorAction;
            customRoomName.EditorAction += editorAction;

            builder.SetPositiveButton(Resource.String.okay, eventHandlerClose);
            dialogInstance = builder.Create();
            dialogInstance.Show();
        }
        private bool first = true;
        private void RoomListSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (roomListSpinner.Adapter.Count - 1 == e.Position)
            {
                customRoomName.Visibility = ViewStates.Visible;
                if (first)
                {
                    first = false;
                }
                else
                {
                    customRoomName.Text = string.Empty; //if you go off this and back then it should clear
                    SearchTabHelper.SearchTargetChosenRoom = string.Empty;
                }
            }
            else
            {
                SearchTabHelper.SearchTargetChosenRoom = roomListSpinner.GetItemAtPosition(e.Position).ToString();
                customRoomName.Visibility = ViewStates.Gone;
            }
        }

        private void CustomRoomName_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            SearchTabHelper.SearchTargetChosenRoom = e.Text.ToString();
        }

        private string GetRoomListSpinnerSelection()
        {
            if (roomListSpinner.SelectedItem.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_))
            {
                return SearchTabHelper.SearchTargetChosenRoom;
            }
            else
            {
                return roomListSpinner.SelectedItem.ToString();
            }
        }

        private void Room_Click(object sender, EventArgs e)
        {
            SearchTabHelper.SearchTargetChosenRoom = GetRoomListSpinnerSelection();
            SearchTabHelper.SearchTarget = SearchTarget.Room;
            targetRoomLayout.Visibility = customRoomName.Visibility = ViewStates.Visible;
            chooseUserInput.Visibility = ViewStates.Gone;
            if (roomListSpinner.SelectedItem.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_))
            {
                customRoomName.Visibility = ViewStates.Visible;
            }
            else
            {
                customRoomName.Visibility = ViewStates.Gone;
            }
            SetSearchHintTarget(SearchTarget.Room);
        }

        private static AndroidX.AppCompat.App.AlertDialog dialogInstance = null;

        private void FilterText_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done || //in this case it is Done (blue checkmark)
                e.ActionId == Android.Views.InputMethods.ImeAction.Go ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Next ||
                e.ActionId == Android.Views.InputMethods.ImeAction.Search)
            {
                MainActivity.LogDebug("IME ACTION: " + e.ActionId.ToString());
                rootView.FindViewById<EditText>(Resource.Id.filterText).ClearFocus();
                rootView.FindViewById<View>(Resource.Id.focusableLayout).RequestFocus();
                //overriding this, the keyboard fails to go down by default for some reason.....
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(rootView.WindowToken, 0);
                }
                catch (System.Exception ex)
                {
                    MainActivity.LogFirebase(ex.Message + " error closing keyboard");
                }
            }
        }

        //public class MyCallback : BottomSheetBehavior.BottomSheetCallback
        //{
        //    public override void OnSlide(View bottomSheet, float slideOffset)
        //    {
        //        //
        //    }

        //    public override void OnStateChanged(View bottomSheet, int newState)  //the main problem is the slow animation...
        //    {
        //        if(newState==BottomSheetBehavior.StateHidden)
        //        {
        //            try
        //            {

        //                //SoulSeekState.MainActivityRef.DispatchKeyEvent(new KeyEvent(new KeyEventActions(),Keycode.Enter));
        //                Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
        //                imm.HideSoftInputFromWindow(bottomSheet.WindowToken, 0);
        //            }
        //            catch
        //            {
        //                //not worth throwing over
        //            }
        //        }
        //    }
        //}

        public class ChipFilter
        {
            //this comes from "mp3 - all" and will match any (== "mp3") or (contains "mp3 ") results
            //the items in these filters are always OR'd
            public ChipFilter()
            {
                AllVarientsFileType = new List<string>();
                SpecificFileType = new List<string>();
                NumFiles = new List<int>();
                FileRanges = new List<Tuple<int, int>>();
                Keywords = new List<string>();
                KeywordInvarient = new List<List<string>>();

            }
            public List<string> AllVarientsFileType;
            public List<string> SpecificFileType;
            public List<int> NumFiles;
            public List<Tuple<int, int>> FileRanges;

            //these are the keywords.  keywords invarient will contain say "Paul and Jake", "Paul & Jake". they are OR'd inner.  both collections outer are AND'd.
            public List<string> Keywords;
            public List<List<string>> KeywordInvarient;

            public bool IsEmpty()
            {
                return (AllVarientsFileType.Count == 0 && SpecificFileType.Count == 0 && NumFiles.Count == 0 && FileRanges.Count == 0 && Keywords.Count == 0 && KeywordInvarient.Count == 0);
            }
        }

        public static ChipFilter ParseChips(SearchTab searchTab)
        {
            ChipFilter chipFilter = new ChipFilter();
            var checkedChips = searchTab.ChipDataItems.Where(i => i.IsChecked).ToList();
            foreach (var chip in checkedChips)
            {
                if (chip.ChipType == ChipType.FileCount)
                {
                    if (chip.DisplayText.EndsWith(" file"))
                    {
                        chipFilter.NumFiles.Add(1);
                    }
                    else if (chip.DisplayText.Contains(" to "))
                    {
                        int endmin = chip.DisplayText.IndexOf(" to ");
                        int min = int.Parse(chip.DisplayText.Substring(0, endmin));
                        int max = int.Parse(chip.DisplayText.Substring(endmin + 4, chip.DisplayText.IndexOf(" files") - (endmin + 4)));
                        chipFilter.FileRanges.Add(new Tuple<int, int>(min, max));
                    }
                    else if (chip.DisplayText.EndsWith(" files"))
                    {
                        chipFilter.NumFiles.Add(int.Parse(chip.DisplayText.Replace(" files", "")));
                    }
                }
                else if (chip.ChipType == ChipType.FileType)
                {
                    if (chip.HasTag())
                    {
                        foreach (var subChipString in chip.Children)
                        {
                            //its okay if this contains "mp3 (other)" say because if it does then by definition it will also contain
                            //mp3 - all bc we dont split groups.
                            if (subChipString.EndsWith(" - all"))
                            {
                                chipFilter.AllVarientsFileType.Add(subChipString.Replace(" - all", ""));
                            }
                            else
                            {
                                chipFilter.SpecificFileType.Add(subChipString);
                            }
                        }
                    }
                    else if (chip.DisplayText.EndsWith(" - all"))
                    {
                        chipFilter.AllVarientsFileType.Add(chip.DisplayText.Replace(" - all", ""));
                    }
                    else
                    {
                        chipFilter.SpecificFileType.Add(chip.DisplayText);
                    }
                }
                else if (chip.ChipType == ChipType.Keyword)
                {
                    if (chip.Children == null)
                    {
                        chipFilter.Keywords.Add(chip.DisplayText);
                    }
                    else
                    {
                        chipFilter.KeywordInvarient.Add(chip.Children);
                    }
                }
            }
            return chipFilter;
        }


        public static void ParseFilterString(SearchTab searchTab)
        {
            List<string> filterStringSplit = searchTab.FilterString.Split(' ').ToList();
            searchTab.WordsToAvoid.Clear();
            searchTab.WordsToInclude.Clear();
            searchTab.FilterSpecialFlags.Clear();
            foreach (string word in filterStringSplit)
            {
                if (word.Contains("mbr:") || word.Contains("minbitrate:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinBitRateKBS = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfs:") || word.Contains("minfilesize:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFileSizeMB = (Integer.ParseInt(word.Split(':')[1]));
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFoldersInFile = Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch (System.Exception)
                    {

                    }
                }
                else if (word == "isvbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsVBR = true;
                }
                else if (word == "iscbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsCBR = true;
                }
                else if (word.StartsWith('-'))
                {
                    if(word.Length>1)//if just '-' dont remove everything. just skip it.
                    {
                        searchTab.WordsToAvoid.Add(word.Substring(1)); //skip the '-'
                    }
                }
                else
                {
                    searchTab.WordsToInclude.Add(word);
                }
            }
        }

        private bool MatchesChipCriteria(SearchResponse s, ChipFilter chipFilter, bool hideLocked)
        {
            if (chipFilter == null || chipFilter.IsEmpty())
            {
                return true;
            }
            else
            {
                bool match = chipFilter.NumFiles.Count == 0 && chipFilter.FileRanges.Count == 0;
                int fcount = hideLocked ? s.FileCount : s.FileCount + s.LockedFileCount;
                foreach (int num in chipFilter.NumFiles)
                {
                    if (fcount == num)
                    {
                        match = true;
                    }
                }
                foreach (Tuple<int, int> range in chipFilter.FileRanges)
                {
                    if (fcount >= range.Item1 && fcount <= range.Item2)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                match = chipFilter.AllVarientsFileType.Count == 0 && chipFilter.SpecificFileType.Count == 0;
                foreach (string varient in chipFilter.AllVarientsFileType)
                {
                    if (s.GetDominantFileType(hideLocked) == varient || s.GetDominantFileType(hideLocked).Contains(varient + " "))
                    {
                        match = true;
                    }
                }
                foreach (string specific in chipFilter.SpecificFileType)
                {
                    if (s.GetDominantFileType(hideLocked) == specific)
                    {
                        match = true;
                    }
                }
                if (!match)
                {
                    return false;
                }

                string fullFname = s.Files.FirstOrDefault()?.Filename ?? s.LockedFiles.FirstOrDefault().Filename;
                foreach (string keyword in chipFilter.Keywords)
                {
                    if (!Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) &&
                        !Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return false;
                    }
                }
                foreach (List<string> keywordsInvar in chipFilter.KeywordInvarient)
                {
                    //do any match?
                    bool anyMatch = false;
                    foreach (string keyword in keywordsInvar)
                    {
                        if (Helpers.GetFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase) ||
                            Helpers.GetParentFolderNameFromFile(fullFname).Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                        {
                            anyMatch = true;
                            break;
                        }
                    }
                    if (!anyMatch)
                    {
                        return false;
                    }
                }
                if (!match)
                {
                    return false;
                }

                return true;
            }
        }


        private bool MatchesCriteria(SearchResponse s, bool hideLocked)
        {
            foreach (File f in s.GetFiles(hideLocked))
            {
                string dirString = Helpers.GetFolderNameFromFile(f.Filename);
                string fileString = Helpers.GetFileNameFromFile(f.Filename);
                foreach (string avoid in SearchTabHelper.WordsToAvoid)
                {
                    if (dirString.Contains(avoid, StringComparison.OrdinalIgnoreCase) || fileString.Contains(avoid, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                bool includesAll = true;
                foreach (string include in SearchTabHelper.WordsToInclude)
                {
                    if (!dirString.Contains(include, StringComparison.OrdinalIgnoreCase) && !fileString.Contains(include, StringComparison.OrdinalIgnoreCase))
                    {
                        includesAll = false;
                        break;
                    }
                }
                if (includesAll)
                {
                    return true;
                }
            }
            return false;
        }

        //should be called whenever either the filter changes, new search results come in, the search gets cleared, etc.
        //includes chips
        private void UpdateFilteredResponses(SearchTab searchTab)
        {
            //The Rules:
            //if separated by space, then it must contain both them, in any order
            //if - in front then it must not contain this word
            //there are also several keywords

            MainActivity.LogDebug("Words To Avoid: " + searchTab.WordsToAvoid.ToString());
            MainActivity.LogDebug("Words To Include: " + searchTab.WordsToInclude.ToString());
            MainActivity.LogDebug("Whether to Filer: " + searchTab.FilteredResults);
            MainActivity.LogDebug("FilterString: " + searchTab.FilterString);
            bool hideLocked = SoulSeekState.HideLockedResultsInSearch;
            searchTab.UI_SearchResponses.Clear();
            searchTab.UI_SearchResponses.AddRange(searchTab.SearchResponses.FindAll(new Predicate<SearchResponse>(
            (SearchResponse s) =>
            {
                if (!MatchesCriteria(s, hideLocked))
                {
                    return false;
                }
                else if (!MatchesChipCriteria(s, searchTab.ChipsFilter, hideLocked))
                {
                    return false;
                }
                else
                {   //so it matches the word criteria.  now lets see if it matches the flags if any...
                    if (!searchTab.FilterSpecialFlags.ContainsSpecialFlags)
                    {
                        return true;
                    }
                    else
                    {
                        //we need to make sure this also matches our special flags
                        if (searchTab.FilterSpecialFlags.MinFoldersInFile != 0)
                        {
                            if (searchTab.FilterSpecialFlags.MinFoldersInFile > (hideLocked ? s.Files.Count : (s.Files.Count + s.LockedFiles.Count)))
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.MinFileSizeMB != 0)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                int mb = (int)(f.Size) / (1024 * 1024);
                                if (mb > searchTab.FilterSpecialFlags.MinFileSizeMB)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.MinBitRateKBS != 0)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.BitRate == null || !(f.BitRate.HasValue))
                                {
                                    continue;
                                }
                                if ((int)(f.BitRate) > searchTab.FilterSpecialFlags.MinBitRateKBS)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.IsCBR)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.IsVariableBitRate == false)//this is bool? can have no value...
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        if (searchTab.FilterSpecialFlags.IsVBR)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.GetFiles(hideLocked))
                            {
                                if (f.IsVariableBitRate == true)
                                {
                                    match = true;
                                }
                            }
                            if (!match)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            })));
        }

        private void FilterText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            MainActivity.LogDebug("Text Changed: " + e.Text);
            string oldFilterString = SearchTabHelper.FilteredResults ? SearchTabHelper.FilterString : string.Empty;
            if ((e.Text != null && e.Text.ToString() != string.Empty && SearchTabHelper.SearchResponses != null) || this.AreChipsFiltering())
            {

#if DEBUG
                var sw = System.Diagnostics.Stopwatch.StartNew();
#endif
                SearchTabHelper.FilteredResults = true;
                SearchTabHelper.FilterString = e.Text.ToString();
                if (FilterSticky)
                {
                    FilterStickyString = SearchTabHelper.FilterString;
                }
                ParseFilterString(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);

                var oldList = SearchTabHelper.UI_SearchResponses.ToList();
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);

                

#if DEBUG

                int oldCount = oldList.Count;
                int newCount = SearchTabHelper.UI_SearchResponses.Count();
                MainActivity.LogDebug($"update filtered only - old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");

#endif

                //DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, SearchTabHelper.UI_SearchResponses), true);

                //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.FilteredResponses);
                //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                //lv.Adapter = (customAdapter);

                //res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);
#if DEBUG
                sw.Stop();
#endif
                //DIFFUTIL is extremely extremely slow going from large to small number i.e. 1000 to 250 results.  
                //it takes a full 700ms.  Whereas NotifyDataSetChanged and setting the adapter take 0-7ms. with notifydatasetcahnged being a bit faster.

                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top. (unlike old method)
#if DEBUG
                MainActivity.LogDebug($"old {oldCount} new {newCount} time {sw.ElapsedMilliseconds} ms");

#endif
                
            }
            else
            {
                SearchTabHelper.FilteredResults = false;
                SearchTabHelper.FilterString = string.Empty;
                if (FilterSticky)
                {
                    FilterStickyString = SearchTabHelper.FilterString;
                }
                ParseFilterString(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);



                // DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(SearchTabHelper.UI_SearchResponses, SearchTabHelper.SearchResponses), true);


                SearchTabHelper.UI_SearchResponses.Clear();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);
                //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.Clear();
                //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.AddRange(newList);

                //res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);




                recyclerSearchAdapter.NotifyDataSetChanged(); //does have the nice effect that if nothing changes, you dont just back to top.
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
            }

            if (oldFilterString == string.Empty && e.Text.ToString() != string.Empty)
            {
                UpdateDrawableState(sender as EditText, true);
            }
            else if (oldFilterString != string.Empty && e.Text.ToString() == string.Empty)
            {
                UpdateDrawableState(sender as EditText, true);
            }
        }

        /// <summary>
        /// !!!!!!!!!!!!!!!!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RefreshOnChipChanged()
        {
            if (this.AreChipsFiltering() || !string.IsNullOrEmpty(SearchTabHelper.FilterString))
            {
                SearchTabHelper.FilteredResults = true;
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);

                //recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);

                recyclerSearchAdapter.NotifyDataSetChanged();
                bool refSame = System.Object.ReferenceEquals(recyclerSearchAdapter.localDataSet, SearchTabHelper.UI_SearchResponses);
                bool refSame2 = System.Object.ReferenceEquals(recyclerSearchAdapter.localDataSet, SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].UI_SearchResponses);
                //SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.FilteredResponses);
                //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                //lv.Adapter = (customAdapter);
            }
            else
            {
                SearchTabHelper.FilteredResults = false;
                SearchTabHelper.UI_SearchResponses.Clear();// = SearchTabHelper.SearchResponses.ToList();
                SearchTabHelper.UI_SearchResponses.AddRange(SearchTabHelper.SearchResponses);// = SearchTabHelper.SearchResponses.ToList();
                //recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                //recyclerViewTransferItems.SetAdapter(recyclerSearchAdapter);
                recyclerSearchAdapter.NotifyDataSetChanged();
            }
        }

        //private void Actv_Click(object sender, EventArgs e)
        //{
        //    Android.Views.InputMethods.InputMethodManager im = (Android.Views.InputMethods.InputMethodManager)SoulSeekState.MainActivityRef.GetSystemService(Context.InputMethodService);
        //    im.ShowSoftInput(sender as View, 0);
        //    (sender as View).RequestFocus();
        //}

        private void SoulSeekState_ClearSearchHistory(object sender, EventArgs e)
        {
            searchHistory = new List<string>();
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchHistory, string.Empty);
                editor.Commit();
            }
            if (SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView != null)
            {
                AutoCompleteTextView actv = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                actv.Adapter = new ArrayAdapter<string>(context, Resource.Layout.autoSuggestionRow, searchHistory);
            }
        }

        private void UpdateForScreenSize()
        {
            if (!SoulSeekState.IsLowDpi()) return;
            try
            {
                //this.rootView.FindViewById<TextView>(Resource.Id.searchesQueue).SetTextSize(ComplexUnitType.Dip,8);
                this.rootView.FindViewById<TextView>(Resource.Id.searchesKbs).SetTextSize(ComplexUnitType.Sp, 8);
            }
            catch
            {
                //not worth throwing over
            }
        }

        public override void OnPause()
        {
            MainActivity.LogDebug("SearchFragmentOnPause");
            base.OnPause();

            string listOfSearchItems = string.Empty;
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(searchHistory.GetType());
                serializer.Serialize(writer, searchHistory);
                listOfSearchItems = writer.ToString();
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_SearchHistory, listOfSearchItems);
                if (FilterSticky)
                {
                    editor.PutBoolean(SoulSeekState.M_FilterSticky, FilterSticky);
                    editor.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
                }
                editor.PutInt(SoulSeekState.M_SearchResultStyle, (int)SearchResultStyle);
                editor.Commit();
            }
        }

        private static void Actv_KeyPressHELPER(object sender, View.KeyEventArgs e)
        {
            SearchFragment.Instance.Actv_KeyPress(sender, e);
        }

        public static void PerformSearchLogicFromSearchDialog(string searchTerm)
        {
            EditText editTextSearch = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            editTextSearch.Text = searchTerm;
            SearchFragment.Instance.PeformSearchLogic(null);
        }

        private void PeformSearchLogic(object sender)
        {
            var transitionDrawable = GetTransitionDrawable();
            if (SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
            {
                MainActivity.LogDebug("transitionDrawable: RESET transition");
                transitionDrawable.ReverseTransition(SearchToCloseDuration); //you cannot hit reverse twice, it will put it back to the original state...
                SearchTabHelper.CancellationTokenSource.Cancel();
                SearchTabHelper.CurrentlySearching = false;
            }
            else
            {
                transitionDrawable.StartTransition(SearchToCloseDuration);
                PerformBackUpRefresh();
                MainActivity.LogDebug("START TRANSITION");
                SearchTabHelper.CurrentlySearching = true;
            }
            SearchTabHelper.CancellationTokenSource = new CancellationTokenSource();
            EditText editTextSearch = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            SearchAPI(SearchTabHelper.CancellationTokenSource.Token, transitionDrawable, editTextSearch.Text, SearchTabHelper.CurrentTab);
            if (sender != null)
            {
                (sender as AutoCompleteTextView).DismissDropDown();
            }
            MainActivity.LogDebug("Enter Pressed..");
        }

        private void Actv_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down)
            {
                MainActivity.LogDebug("ENTER PRESSED " + e.KeyCode.ToString());
                PeformSearchLogic(sender);
            }
            else if (e.KeyCode == Keycode.Del && e.Event.Action == KeyEventActions.Down)
            {
                (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                return;
            }
            else if ((e.Event.Action == KeyEventActions.Down || e.Event.Action == KeyEventActions.Up) && (e.KeyCode == Android.Views.Keycode.Back || e.KeyCode == Android.Views.Keycode.VolumeUp || e.KeyCode == Android.Views.Keycode.VolumeDown))
            {
                //for some reason e.Handled is always true coming in.  also only on down volume press does anything.
                e.Handled = false;
            }
            else //this will only occur on unhandled keys which on the softkeyboard probably has to be in the above two categories....
            {
                if (e.Event.Action == KeyEventActions.Down)
                {
                    //MainActivity.LogDebug(e.KeyCode.ToString()); //happens on HW keyboard... event does NOT get called on SW keyboard. :)
                    //MainActivity.LogDebug((sender as AutoCompleteTextView).IsFocused.ToString());
                    (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                }
            }
        }

        private static void SoulseekClient_SearchResponseReceived(object sender, SearchResponseReceivedEventArgs e, int fromTab, bool fromWishlist)
        {
            //MainActivity.LogDebug("SoulseekClient_SearchResponseReceived");
            //MainActivity.LogDebug(e.Response.Username + " queuelength: " + e.Response.QueueLength + " free upload slots" + e.Response.FreeUploadSlots);
            //Console.WriteLine("Response Received");
            //CustomAdapter customAdapter = new CustomAdapter(Context, searchResponses);
            //ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
            //lv.Adapter = (customAdapter);
            if (e.Response.FileCount == 0 && SoulSeekState.HideLockedResultsInSearch || !SoulSeekState.HideLockedResultsInSearch && e.Response.FileCount == 0 && e.Response.LockedFileCount == 0)
            {
                MainActivity.LogDebug("Skipping Locked or 0/0");
                return;
            }
            //MainActivity.LogDebug("SEARCH RESPONSE RECEIVED");
            refreshListView(e.Response, fromTab, fromWishlist);
            //SoulSeekState.MainActivityRef.RunOnUiThread(action);

        }

        private static void clearListView(bool fromWishlist)
        {
            if (fromWishlist)
            {
                return; //we combine results...
            }


            MainActivity.LogDebug("clearListView SearchResponses.Clear()");
            SearchTabHelper.SortHelper.Clear();
            SearchTabHelper.SearchResponses.Clear();
            SearchTabHelper.LastSearchResponseCount = -1;
            SearchTabHelper.UI_SearchResponses.Clear();
            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = null;
            SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipsFilter = null;
            if (!fromWishlist)
            {
                SearchFragment.Instance.ClearFilterStringAndCached();

                SearchTabHelper.UI_SearchResponses = SearchTabHelper.SearchResponses?.ToList();
                SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.UI_SearchResponses);
                SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);

                SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems);
                SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);


            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="origResponse"></param>
        /// <returns>Whether we need to split, and if true then the split search responses</returns>
        private static Tuple<bool, List<SearchResponse>> SplitMultiDirResponse(SearchResponse origResponse)
        {
            try
            {
                bool hideLocked = SoulSeekState.HideLockedResultsInSearch;
                if (origResponse.Files.Count != 0 || (!hideLocked && origResponse.LockedFiles.Count != 0))
                {
                    Dictionary<string, List<File>> folderFilePairs = new Dictionary<string, List<File>>();
                    foreach (File f in origResponse.Files)
                    {
                        string folderName = Helpers.GetFolderNameFromFile(f.Filename);
                        if (folderFilePairs.ContainsKey(folderName))
                        {
                            //MainActivity.LogDebug("Split Foldername: " + folderName);
                            folderFilePairs[folderName].Add(f);
                        }
                        else
                        {
                            List<File> tempF = new List<File>();
                            tempF.Add(f);
                            folderFilePairs.Add(folderName, tempF);
                        }
                    }

                    //I'm not sure if locked files and unlocked files can appear in the same folder,
                    //but regardless, split them up into separate folders.
                    //even if they both have the same foldername, they will have the lock symbol to differentiate them.
                    Dictionary<string, List<File>> lockedFolderFilePairs = new Dictionary<string, List<File>>();
                    if(!hideLocked)
                    {
                        foreach (File f in origResponse.LockedFiles)
                        {
                            string folderName = Helpers.GetFolderNameFromFile(f.Filename);
                            if (lockedFolderFilePairs.ContainsKey(folderName))
                            {
                                //MainActivity.LogDebug("Split Foldername: " + folderName);
                                lockedFolderFilePairs[folderName].Add(f);
                            }
                            else
                            {
                                List<File> tempF = new List<File>();
                                tempF.Add(f);
                                lockedFolderFilePairs.Add(folderName, tempF);
                            }
                        }
                    }

                    //we took the search response and split it into more than one folder.
                    if ((folderFilePairs.Keys.Count + lockedFolderFilePairs.Keys.Count) > 1)
                    {
                        //split them
                        List<SearchResponse> splitSearchResponses = new List<SearchResponse>();
                        foreach (var pair in folderFilePairs)
                        {
                            splitSearchResponses.Add(new SearchResponse(origResponse.Username, origResponse.Token, origResponse.FreeUploadSlots, origResponse.UploadSpeed, origResponse.QueueLength, pair.Value, null));
                        }
                        foreach (var pair in lockedFolderFilePairs)
                        {
                            splitSearchResponses.Add(new SearchResponse(origResponse.Username, origResponse.Token, origResponse.FreeUploadSlots, origResponse.UploadSpeed, origResponse.QueueLength, null, pair.Value));
                        }
                        //MainActivity.LogDebug("User: " + origResponse.Username + " got split into " + folderFilePairs.Keys.Count);
                        return new Tuple<bool, List<SearchResponse>>(true, splitSearchResponses);
                    }
                    else
                    {
                        //no need to split it.
                        return new Tuple<bool, List<SearchResponse>>(false, null);
                    }

                }
                else
                {
                    return new Tuple<bool, List<SearchResponse>>(false, null);
                }
            }
            catch (System.Exception e)
            {
                MainActivity.LogFirebase(e.Message + " splitmultidirresponse");
                return new Tuple<bool, List<SearchResponse>>(false, null);
            }
        }

        public override void OnDetach() //happens whenever the fragment gets recreated.  (i.e. on rotating device).
        {
            MainActivity.LogDebug("search frag detach");
            base.OnDetach();
        }
        private AutoCompleteTextView searchEditText = null;
        public override void OnAttach(Android.App.Activity activity)
        {
            MainActivity.LogDebug("search frag attach");
            base.OnAttach(activity);
        }

        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        private SearchAdapterRecyclerVersion recyclerSearchAdapter;

        public class SearchAdapterRecyclerVersion : RecyclerView.Adapter
        {
            public List<int> oppositePositions = new List<int>();



            public List<SearchResponse> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position = -1;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {


                (holder as SearchViewHolder).getSearchItemView().setItem(localDataSet[position], position);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public void setPosition(int position)
            {
                this.position = position;
            }

            public int getPosition()
            {
                return this.position;
            }

            //public override void OnViewRecycled(Java.Lang.Object holder)
            //{
            //    base.OnViewRecycled(holder);
            //}

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                ISearchItemViewBase view = null;
                switch (this.searchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                    case SearchResultStyleEnum.CollapsedAll:
                        view = SearchItemViewExpandable.inflate(parent);
                        (view as SearchItemViewExpandable).AdapterRef = this;
                        (view as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
                        (view as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
                        break;
                    case SearchResultStyleEnum.Medium:
                        view = SearchItemViewMedium.inflate(parent);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        view = SearchItemViewMinimal.inflate(parent);
                        break;
                }
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                //view.LongClick += TransferAdapterRecyclerVersion_LongClick;
                (view as View).Click += View_Click;
                return new SearchViewHolder(view as View);

            }

            private void View_Click(object sender, EventArgs e)
            {
                GetSearchFragment().showEditDialog((sender as ISearchItemViewBase).ViewHolder.AdapterPosition);
            }

            private SearchResultStyleEnum searchResultStyle;

            public SearchAdapterRecyclerVersion(List<SearchResponse> ti)
            {
                oldList = null; // no longer valid...
                localDataSet = ti;
                searchResultStyle = SearchFragment.SearchResultStyle;
                oppositePositions = new List<int>();
            }

            private void CustomAdapter_Click1(object sender, EventArgs e)
            {
                //MainActivity.LogInfoFirebase("CustomAdapter_Click1");
                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);
                SearchFragment.Instance.showEditDialog(position);
            }


            private void CustomAdapter_Click(object sender, EventArgs e)
            {
                //throw new NotImplementedException();


                int position = ((sender as View).Parent.Parent.Parent as RecyclerView).GetChildAdapterPosition((sender as View).Parent.Parent as View);

                //int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
                var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
                var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
                if (v.Visibility == ViewStates.Gone)
                {
                    img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Visible;
                    SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, this.localDataSet[position]);
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                    else
                    {
                        oppositePositions.Remove(position);
                    }
                }
                else
                {
                    img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
                    v.Visibility = ViewStates.Gone;
                    if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
                    {
                        oppositePositions.Remove(position);
                    }
                    else
                    {
                        oppositePositions.Add(position);
                        oppositePositions.Sort();
                    }
                }
            }
        }

        public class SearchViewHolder : RecyclerView.ViewHolder
        {
            private ISearchItemViewBase searchItemView;

            public SearchViewHolder(View view) : base(view)
            {
                //super(view);
                // Define click listener for the ViewHolder's View

                searchItemView = (ISearchItemViewBase)view;
                searchItemView.ViewHolder = this;
                //searchItemView.SetOnCreateContextMenuListener(this);
            }

            public ISearchItemViewBase getSearchItemView()
            {
                return searchItemView;
            }
        }

        public class SearchDiffCallback : DiffUtil.Callback
        {
            private List<SearchResponse> oldList;
            private List<SearchResponse> newList;

            public SearchDiffCallback(List<SearchResponse> _oldList, List<SearchResponse> _newList)
            {
                oldList = _oldList;
                newList = _newList;
            }

            public override int NewListSize => newList.Count;

            public override int OldListSize => oldList.Count;

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition].Equals(newList[newItemPosition]); //my override
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                return oldList[oldItemPosition] == newList[newItemPosition];
            }
        }



        //public override void OnStop()
        //{
        //    searchEditText.KeyPress -= Actv_KeyPress;
        //    searchEditText.EditorAction -= Search_EditorAction;

        //    base.OnStop();
        //}

        //public override void OnResume()
        //{

        //    base.OnResume();
        //    searchEditText = (this.Activity as Android.Support.V7.App.AppCompatActivity).SupportActionBar.CustomView.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);

        //    searchEditText.KeyPress -= Actv_KeyPress;
        //    searchEditText.KeyPress += Actv_KeyPress;
        //    searchEditText.EditorAction -= Search_EditorAction;
        //    searchEditText.EditorAction += Search_EditorAction;
        //}

        private static List<SearchResponse> GetOldList(string filter)
        {
            if(filter == oldListCondition)
            {
                return oldList;
            }
            return null;
        }

        private static void SetOldList(string filter, List<SearchResponse> searchResponses)
        {
            oldListCondition = !string.IsNullOrEmpty(filter) ? filter : null;
            oldList = searchResponses;
        }

        private static List<SearchResponse> oldList = new List<SearchResponse>();
        private static string oldListCondition = string.Empty;
        private static List<SearchResponse> newList = new List<SearchResponse>();

        /// <summary>
        /// To add a search response to the list view
        /// </summary>
        /// <param name="resp"></param>
        private static void refreshListView(SearchResponse resp, int fromTab, bool fromWishlist)
        {
            //sort before adding.

            //if search response has multiple directories, we want to split it up just like 
            //how desktop client does.
            //if(SearchTabHelper.CurrentTab == fromTab) //we want to sort 
            //{


            lock (SearchTabHelper.SearchTabCollection[fromTab].SortHelperLockObject) //lock object for the sort helper in question. this used to be the current tab one. I think thats wrong.
            {
                Tuple<bool, List<SearchResponse>> splitResponses = SplitMultiDirResponse(resp);
                try
                {

                    if (splitResponses.Item1)
                    { //we have multiple to add
                        foreach (SearchResponse splitResponse in splitResponses.Item2)
                        {
                            if (fromWishlist && WishlistController.OldResultsToCompare[fromTab].Contains(splitResponse))
                            {
                                continue;
                            }
                            SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Add(splitResponse, null);
                        }
                    }
                    else
                    {
                        if (fromWishlist && WishlistController.OldResultsToCompare[fromTab].Contains(resp))
                        {
                        }
                        else
                        {
                            SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Add(resp, null); //before I added an .Equals method I would get Duplicate Key Exceptions...
                        }
                    }
                }
                catch (System.Exception e)
                {
                    MainActivity.LogDebug(e.Message);
                }

                SearchTabHelper.SearchTabCollection[fromTab].SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Keys.ToList();
                SearchTabHelper.SearchTabCollection[fromTab].LastSearchResultsCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
            }

            //if (fromTab == SearchTabHelper.CurrentTab)
            //{
            //    newList = SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Keys.ToList();
            //}
            //only do fromWishlist if SearchFragment.Instance is not null...

            if ((!fromWishlist || SearchFragment.Instance != null) && fromTab == SearchTabHelper.CurrentTab)
            {
                Action a = new Action(() =>
                {
                    #if DEBUG
                    AndriodApp1.SearchFragment.StopWatch.Stop();
                    //MainActivity.LogDebug("time between start and stop " + AndriodApp1.SearchFragment.StopWatch.ElapsedMilliseconds);
                    AndriodApp1.SearchFragment.StopWatch.Reset();
                    AndriodApp1.SearchFragment.StopWatch.Start();
                    #endif
                    //SearchResponses.Add(resp);
                    //MainActivity.LogDebug("UI - SEARCH RESPONSE RECEIVED");
                    if (fromTab != SearchTabHelper.CurrentTab)
                    {
                        return;
                    }
                    //int total = newList.Count;
                    int total = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                    //MainActivity.LogDebug("START _ ui thread response received - search collection: " + total);
                    if (SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount == total)
                    {
                        //MainActivity.LogDebug("already did it..: " + total);
                        //we already updated for this one.
                        //the UI marshelled calls are delayed.  as a result there will be many all coming in with the final search response count of say 751.  
                        return;
                    }

                    //MainActivity.LogDebug("refreshListView SearchResponses.Count = " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);

                    if (SearchTabHelper.SearchTabCollection[fromTab].FilteredResults)
                    {
                        //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses = newList;
                        oldList = GetOldList(SearchTabHelper.SearchTabCollection[fromTab].FilterString);
                        if(oldList == null)
                        {
                            SearchFragment.Instance.UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                                                                                                                            //todo: diffutil.. was filtered -> now filtered...
                            SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                            SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);
                        }
                        else
                        {
                            //todo: place back
                            var recyclerViewState = SearchFragment.Instance.recycleLayoutManager.OnSaveInstanceState();//  recyclerView.getLayoutManager().onSaveInstanceState();


                            SearchFragment.Instance.UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);
                            MainActivity.LogDebug("refreshListView  oldList: " + oldList.Count + " newList " + newList.Count);
                            DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses), true);
                            //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.Clear();
                            //SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses.AddRange(newList);
                            res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);


                            SearchFragment.Instance.recycleLayoutManager.OnRestoreInstanceState(recyclerViewState);
                        }
                        SetOldList(SearchTabHelper.SearchTabCollection[fromTab].FilterString, SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses.ToList());
                        //SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses);
                        //SearchFragment.Instance.listView.Adapter = (customAdapter);
                    }
                    else
                    {
                        oldList = GetOldList(null);
                        List<SearchResponse> newListx = null;
                        if (oldList == null)
                        {
                            SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                            newListx = SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses;
                            SearchFragment.Instance.recyclerSearchAdapter = new SearchAdapterRecyclerVersion(SearchTabHelper.SearchTabCollection[fromTab].UI_SearchResponses);
                            SearchFragment.Instance.recyclerViewTransferItems.SetAdapter(SearchFragment.Instance.recyclerSearchAdapter);
                        }
                        else
                        {
                            //the SaveInstanceState and RestoreInstanceState are needed, else autoscroll... even when animations are off...
                            //https://stackoverflow.com/questions/43458146/diffutil-in-recycleview-making-it-autoscroll-if-a-new-item-is-added
                            var recyclerViewState = SearchFragment.Instance.recycleLayoutManager.OnSaveInstanceState();//  recyclerView.getLayoutManager().onSaveInstanceState();

                            newListx = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.ToList();
                            #if DEBUG
                            if(oldList.Count==0)
                            {
                            MainActivity.LogDebug("refreshListView  oldList: " + oldList.Count + " newList " + newListx.Count);
                            }
                            #endif
                            DiffUtil.DiffResult res = DiffUtil.CalculateDiff(new SearchDiffCallback(oldList, newListx), true); //race condition where gototab sets oldList to empty and so in DiffUtil we get an index out of range.... or maybe a wishlist happening at thte same time does it??????
                            //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Clear();
                            //SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.AddRange(newList);
                            SearchFragment.Instance.recyclerSearchAdapter.localDataSet.Clear();
                            SearchFragment.Instance.recyclerSearchAdapter.localDataSet.AddRange(newListx);
                            res.DispatchUpdatesTo(SearchFragment.Instance.recyclerSearchAdapter);

                            SearchFragment.Instance.recycleLayoutManager.OnRestoreInstanceState(recyclerViewState);
                        }

                        //when I was adding an empty list here updates only took 1 millisecond (though updating was choppy and weird)... whereas with an actual diff it takes 10 - 50ms but looks a lot nicer.
                        SetOldList(null, newListx); 
                        // 

                        //SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        //SearchFragment.Instance.listView.Adapter = (customAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = total;
                    AndriodApp1.SearchFragment.StopWatch.Stop();
                    //MainActivity.LogDebug("time it takes to set adapter for " + total + " results: " + AndriodApp1.SearchFragment.StopWatch.ElapsedMilliseconds);
                    #if DEBUG
                    AndriodApp1.SearchFragment.StopWatch.Reset();
                    AndriodApp1.SearchFragment.StopWatch.Start();
                    #endif

//                    oldList = newList.ToList();

                    //MainActivity.LogDebug("END _ ui thread response received - search collection: " + total);
                });

                  SoulSeekState.MainActivityRef?.RunOnUiThread(a);

            }

        }

        public static System.Diagnostics.Stopwatch StopWatch = new System.Diagnostics.Stopwatch();

        private void Lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            showEditDialog(e.Position);
            //throw new NotImplementedException();
        }

        //private long lastClickTime = 0;
        public static bool dlDialogShown = false;

        public void showEditDialog(int pos)
        {
            try
            {
                //if (SystemClock.ElapsedRealtime() - lastClickTime < 500)
                //{
                //    return;
                //}
                //lastClickTime = SystemClock.ElapsedRealtime();
                if (dlDialogShown)
                {
                    dlDialogShown = false; //just in the worst case we dont want to prevent too badly.
                    return;
                }
                dlDialogShown = true;
                SearchResponse dlDiagResp = null;
                if (SearchTabHelper.FilteredResults)
                {
                    dlDiagResp = SearchTabHelper.UI_SearchResponses.ElementAt<SearchResponse>(pos);
                }
                else
                {
                    dlDiagResp = SearchTabHelper.SearchResponses.ElementAt<SearchResponse>(pos);
                }
                DownloadDialog downloadDialog = DownloadDialog.CreateNewInstance(pos, dlDiagResp);
                downloadDialog.Show(FragmentManager, "tag_download_test");
                // When creating a DialogFragment from within a Fragment, you must use the Fragment's CHILD FragmentManager to ensure that the state is properly restored after configuration changes. ????
            }
            catch (System.Exception e)
            {
                System.String msg = string.Empty;
                if (SearchTabHelper.FilteredResults)
                {
                    msg = "Filtered.Count " + SearchTabHelper.UI_SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }
                else
                {
                    msg = "SearchResponses.Count = " + SearchTabHelper.SearchResponses.Count.ToString() + " position selected = " + pos.ToString();
                }

                MainActivity.LogFirebase(msg + " showEditDialog" + e.Message);
                Action a = new Action(() => { Toast.MakeText(SoulSeekState.MainActivityRef, "Error, please try again: " + msg, ToastLength.Long); });
                SoulSeekState.MainActivityRef.RunOnUiThread(a);
            }
        }

        private void PerformBackUpRefresh()
        {
            Handler h = new Handler(Looper.MainLooper);
            h.PostDelayed(new Action(() =>
            {
                var menuItem = ActionBarMenu?.FindItem(Resource.Id.action_search);
                if (menuItem != null)
                {
                    menuItem.SetVisible(false);
                    menuItem.SetVisible(true);
                    MainActivity.LogDebug("perform backup refresh");
                }

            }), 310);
        }

        public const int SearchToCloseDuration = 300;

        private static void SearchLogic(CancellationToken cancellationToken, Android.Graphics.Drawables.TransitionDrawable transitionDrawable, string searchString, int fromTab, bool fromWishlist)
        {
            try
            {
                if (!fromWishlist)
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)SearchFragment.Instance.context.GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(SearchFragment.Instance.rootView.WindowToken, 0);
                }
            }
            catch
            {
                //not worth throwing over
            }
            //MainActivity.ShowAlert(new System.Exception("test"),this.Context);
            //EditText editTextSearch = null;
            try
            {
                //all click event handlers occur on UI thread.
                clearListView(fromWishlist);
                //editTextSearch = SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
            }
            catch (System.Exception e)
            {
                //if(SoulSeekState.MainActivityRef==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: MainActivityRef is null");
                //}
                //else if(SoulSeekState.MainActivityRef.SupportActionBar==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: Support Action Bar");
                //}
                //else if(SoulSeekState.MainActivityRef.SupportActionBar.CustomView == null)
                //{
                //    MainActivity.LogFirebase("Search Logic: SupportActionBar.CustomView");
                //}
                //else if(SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere)==null)
                //{
                //    MainActivity.LogFirebase("Search Logic: searchHere");
                //}
                //throw e;
            }
            //in my testing:
            // if someone has 0 free upload slots I could never download from them (even if queue was 68, 250) the progress bar just never moves, though no error, nothing.. ****
            // if someone has 1 free upload slot and a queue size of 100, 143, 28, 5, etc. it worked just fine.
            int searchTimeout = SearchTabHelper.SearchTarget == SearchTarget.AllUsers ? 5000 : 12000;

            Action<SearchResponseReceivedEventArgs> searchResponseReceived = new Action<SearchResponseReceivedEventArgs>((SearchResponseReceivedEventArgs e) =>
            {
                SoulseekClient_SearchResponseReceived(null, e, fromTab, fromWishlist);
            });

            SearchOptions searchOptions = new SearchOptions(responseLimit: SoulSeekState.NumberSearchResults, searchTimeout: searchTimeout, maximumPeerQueueLength: int.MaxValue, minimumPeerFreeUploadSlots: SoulSeekState.FreeUploadSlotsOnly ? 1 : 0, responseReceived: searchResponseReceived);
            SearchScope scope = null;
            if (fromWishlist)
            {
                scope = new SearchScope(SearchScopeType.Wishlist); //this is the same as passing no option for search scope 
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.AllUsers || SearchTabHelper.SearchTarget == SearchTarget.Wishlist) //this is like a manual wishlist search...
            {
                scope = new SearchScope(SearchScopeType.Network); //this is the same as passing no option for search scope
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.UserList)
            {
                if (SoulSeekState.UserList == null || SoulSeekState.UserList.Count == 0)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.user_list_empty, ToastLength.Short).Show();
                    }
                    ));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, SoulSeekState.UserList.Select(item => item.Username).ToArray());
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.ChosenUser)
            {
                if (SearchTabHelper.SearchTargetChosenUser == string.Empty)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_user, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, new string[] { SearchTabHelper.SearchTargetChosenUser });
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.Room)
            {
                if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_room, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.Room, new string[] { SearchTabHelper.SearchTargetChosenRoom });
            }
            try
            {
                Task<IReadOnlyCollection<SearchResponse>> t = null;
                if(fromTab == SearchTabHelper.CurrentTab)
                {
                    //there was a bug where wishlist search would clear this in the middle of diffutil calculating causing out of index crash.
                    oldList?.Clear();
                }
                t = SoulSeekState.SoulseekClient.SearchAsync(SearchQuery.FromText(searchString), options: searchOptions, scope: scope, cancellationToken: cancellationToken);
                //t = TestClient.SearchAsync(searchString, searchResponseReceived, cancellationToken);
                //drawable.StartTransition() - since if we get here, the search is launched and the continue with will always happen...

                t.ContinueWith(new Action<Task<IReadOnlyCollection<SearchResponse>>>((Task<IReadOnlyCollection<SearchResponse>> t) =>
               {
                   SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;

                   if (!t.IsCompletedSuccessfully && t.Exception != null)
                   {
                       MainActivity.LogDebug("search exception: " + t.Exception.Message);
                   }

                   if (t.IsCanceled)
                   {
                       //then the user pressed the button so we dont need to change it back...
                       //GetSearchFragment().GetTransitionDrawable().ResetTransition(); //this does it immediately.
                    }
                   else
                   {

                       SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                       {
                           try
                           {
                               if (fromTab == SearchTabHelper.CurrentTab && !fromWishlist)
                               {
                                   MainActivity.LogDebug("transitionDrawable: ReverseTransition transition");
                                   //this can be stale, not part of anything anymore....
                                   //no real way to test that.  IsVisible returns true...
                                   try
                                   {
                                        GetSearchFragment().GetTransitionDrawable().ReverseTransition(SearchToCloseDuration);
                                   }
                                   catch
                                   {

                                   }
                                   SearchFragment.Instance.PerformBackUpRefresh();


                               }
                           }
                           catch (System.ObjectDisposedException e)
                           {
                                //since its disposed when you go back to the screen it will be the correct search icon again..
                                //noop
                            }
                       }));

                   }
                   if ((!t.IsCanceled) && t.Result.Count == 0 && !fromWishlist) //if t is cancelled, t.Result throws..
                    {
                       SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                       {
                           Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_search_results, ToastLength.Short).Show();
                       }));
                   }
                   SearchTabHelper.SearchTabCollection[fromTab].LastSearchResultsCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                   if (fromWishlist)
                   {
                       WishlistController.SearchCompleted(fromTab);
                   }
                   else if (SearchTabHelper.SearchTabCollection[fromTab].SearchTarget == SearchTarget.Wishlist)
                   {
                       //this is if the search was not automatic (i.e. wishlist timer elapsed) but was performed in the wishlist tab..
                       //therefore save the new results...
                       SearchTabHelper.SaveHeadersToSharedPrefs();
                       SearchTabHelper.SaveSearchResultsToDisk(fromTab, SoulSeekState.ActiveActivityRef);
                   }

                   if (fromTab == SearchTabHelper.CurrentTab)
                   {
                       if (SoulSeekState.ShowSmartFilters)
                       {
#if DEBUG
                            try
                            {
                                var df = SoulSeekState.RootDocumentFile.CreateFile("text/plain", SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm.Replace(' ','_'));
                                var outputStream = SoulSeekState.ActiveActivityRef.ContentResolver.OpenOutputStream(df.Uri);
                                foreach (var sr in SearchTabHelper.SearchTabCollection[fromTab].SearchResponses)
                                {
                                    byte[] bytesText = System.Text.Encoding.ASCII.GetBytes(sr.Files.First().Filename + System.Environment.NewLine);
                                    outputStream.Write(bytesText, 0, bytesText.Length);
                                }
                                outputStream.Close();
                            }
                            catch
                            {

                            }

#endif
                            List<ChipDataItem> chipDataItems = ChipsHelper.GetChipDataItemsFromSearchResults(SearchTabHelper.SearchTabCollection[fromTab].SearchResponses, SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm, SoulSeekState.SmartFilterOptions);
                           SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab].ChipDataItems = chipDataItems;
                           SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                           {
                               SearchFragment.Instance.recyclerChipsAdapter = new ChipsItemRecyclerAdapter(SearchTabHelper.SearchTabCollection[fromTab].ChipDataItems);
                               SearchFragment.Instance.recyclerViewChips.SetAdapter(SearchFragment.Instance.recyclerChipsAdapter);
                           }));
                       }

                   }

               }));



                if (SearchTabHelper.FilteredResults && FilterSticky && !fromWishlist)
                {
                    //remind the user that the filter is ON.
                    t.ContinueWith(new Action<Task>(
                        (Task t) =>
                        {
                            SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
                            {

                                RelativeLayout rel = SearchFragment.Instance.rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
                                BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
                                if (bsb.State == BottomSheetBehavior.StateHidden)
                                {

                                    View BSButton = SearchFragment.Instance.rootView.FindViewById<View>(Resource.Id.bsbutton);
                                    ObjectAnimator objectAnimator = new ObjectAnimator();
                                    ObjectAnimator anim1 = ObjectAnimator.OfFloat(BSButton, "scaleX", 2.0f);
                                    anim1.SetInterpolator(new Android.Views.Animations.LinearInterpolator());
                                    anim1.SetDuration(200);
                                    ObjectAnimator anim2 = ObjectAnimator.OfFloat(BSButton, "scaleY", 2.0f);
                                    anim2.SetInterpolator(new Android.Views.Animations.LinearInterpolator());
                                    anim2.SetDuration(200);

                                    ObjectAnimator anim3 = ObjectAnimator.OfFloat(BSButton, "scaleX", 1.0f);
                                    anim3.SetInterpolator(new Android.Views.Animations.BounceInterpolator());
                                    anim3.SetDuration(1000);
                                    ObjectAnimator anim4 = ObjectAnimator.OfFloat(BSButton, "scaleY", 1.0f);
                                    anim4.SetInterpolator(new Android.Views.Animations.BounceInterpolator());
                                    anim4.SetDuration(1000);

                                    AnimatorSet set1 = new AnimatorSet();
                                    set1.PlayTogether(anim1, anim2);
                                    //set1.Start();

                                    AnimatorSet set2 = new AnimatorSet();
                                    set2.PlayTogether(anim3, anim4);
                                    //set2.Start();

                                    AnimatorSet setTotal = new AnimatorSet();
                                    setTotal.PlaySequentially(set1, set2);
                                    setTotal.Start();

                                }

                            }));
                        }
                        ));
                }
            }
            catch (ArgumentNullException ane)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    string errorMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_wish_text);
                    }

                    Toast.MakeText(SoulSeekState.ActiveActivityRef, errorMsg, ToastLength.Short).Show();
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }
                ));
                //MainActivity.ShowAlert(ane, this.Context);
                return;
            }
            catch (ArgumentException ae)
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    string errorMsg = SoulSeekState.MainActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_wish_text);
                    }
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, errorMsg, ToastLength.Short).Show();
                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }));
                return;
            }
            //catch(InvalidOperationException)
            //{
            //    //this means that we lost connection to the client.  lets re-login and try search again..
            //    //idealy we do this on a separate thread but for testing..

            //    //SoulSeekState.SoulseekClient.SearchAsync(SearchQuery.FromText(editTextSearch.Text), options: searchOptions);
            //}
            catch (System.Exception ue)
            {

                SoulSeekState.ActiveActivityRef.RunOnUiThread(new Action(() =>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    MainActivity.LogDebug("transitionDrawable: RESET transition");

                    Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.search_error_unspecified, ToastLength.Short).Show();
                    MainActivity.LogFirebase("tabpageradapter searchclick: " + ue.Message);

                    if (!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
                    {
                        transitionDrawable.ResetTransition();
                    }
                }));
                return;
            }
            if (!fromWishlist)
            {
                //add a new item to our search history
                if (SoulSeekState.RememberSearchHistory)
                {
                    if (!searchHistory.Contains(searchString))
                    {
                        searchHistory.Add(searchString);
                    }
                }
                var actv = SoulSeekState.MainActivityRef.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere); // lot of nullrefs with actv before this change....
                if (actv == null)
                {
                    actv = (SearchFragment.Instance.Activity as Android.Support.V7.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                    if (actv == null)
                    {
                        MainActivity.LogFirebase("actv stull null, cannot refresh adapter");
                        return;
                    }
                }
                actv.Adapter = new ArrayAdapter<string>(SearchFragment.Instance.context, Resource.Layout.autoSuggestionRow, searchHistory); //refresh adapter
            }
        }

        public static void SearchAPI(CancellationToken cancellationToken, Android.Graphics.Drawables.TransitionDrawable transitionDrawable, string searchString, int fromTab, bool fromWishlist = false)
        {
            SearchTabHelper.SearchTabCollection[fromTab].LastSearchTerm = searchString;
            SearchTabHelper.SearchTabCollection[fromTab].LastRanTime = DateTime.Now;
            if (!fromWishlist)
            {
                //try to clearFocus on the search if you can (gets rid of blinking cursor)
                ClearFocusSearchEditText();
                MainActivity.LogDebug("Search_Click");
            }
            //#if !DEBUG
            if (!SoulSeekState.currentlyLoggedIn)
            {
                if (!fromWishlist)
                {
                    Toast tst = Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.must_be_logged_to_search, ToastLength.Long);
                    tst.Show();
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    transitionDrawable.ResetTransition();
           
                }
           
                SearchTabHelper.CurrentlySearching = false;
                return;
            }
            else if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                //re-connect if from wishlist as well. just do it quietly.
                //if (fromWishlist)
                //{
                //    return;
                //}
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SoulSeekState.ActiveActivityRef, fromWishlist, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        if(!fromWishlist)
                        {
                            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                            {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            });
                        }
                        return;
                    }
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist); });
           
                }));
            }
            else
            {
            //#endif
                SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist);
            //#if !DEBUG
            }
            //#endif
        }
    }

    public class SearchTabItemRecyclerAdapter : RecyclerView.Adapter
    {
        private List<int> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public bool ForWishlist = false;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            SearchTabView view = SearchTabView.inflate(parent);
            view.setupChildren();
            // .inflate(R.layout.text_row_item, viewGroup, false);
            (view as SearchTabView).searchTabLayout.Click += SearchTabLayout_Click;
            (view as SearchTabView).removeSearch.Click += RemoveSearch_Click;
            return new SearchTabViewHolder(view as View);


        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as SearchTabViewHolder).searchTabView.setItem(localDataSet[position]);
        }

        private void RemoveSearch_Click(object sender, EventArgs e)
        {
            position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
            if (position == -1) //in my case this happens if you delete too fast...
            {
                return;
            }
            int tabToRemove = localDataSet[position];
            bool isWishlist = (SearchTabHelper.SearchTabCollection[tabToRemove].SearchTarget == SearchTarget.Wishlist);
            SearchTabHelper.SearchTabCollection[tabToRemove].CancellationTokenSource?.Cancel();
            if (isWishlist)
            {
                if (tabToRemove == SearchTabHelper.CurrentTab)
                {
                    //remove it for real
                    SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                    localDataSet.RemoveAt(position);
                    SearchTabDialog.Instance.recycleWishesAdapter.NotifyItemRemoved(position);


                    //go to search tab instead (there is always one)
                    string listOfKeys2 = System.String.Join(",", SearchTabHelper.SearchTabCollection.Keys);
                    MainActivity.LogInfoFirebase("list of Keys: " + listOfKeys2);
                    int tabToGoTo = SearchTabHelper.SearchTabCollection.Keys.Where(key => key >= 0).First();
                    SearchFragment.Instance.GoToTab(tabToGoTo, true);
                }
                else
                {
                    //remove it for real
                    SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                    localDataSet.RemoveAt(position);
                    SearchTabDialog.Instance.recycleWishesAdapter.NotifyItemRemoved(position);
                }
            }
            else
            {
                if (tabToRemove == SearchTabHelper.CurrentTab)
                {
                    SearchTabHelper.SearchTabCollection[tabToRemove] = new SearchTab(); //clear it..
                    SearchFragment.Instance.GoToTab(tabToRemove, true);
                    SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemChanged(position);
                }
                else
                {

                    if (SearchTabHelper.SearchTabCollection.Keys.Where(key => key >= 0).Count() == 1)
                    {
                        //it is the only non wishlist tab, so just clear it...  this can happen if we are on a wishlist tab and we clear all the normal tabs.
                        SearchTabHelper.SearchTabCollection[tabToRemove] = new SearchTab();
                        SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemChanged(position);
                    }
                    else
                    {
                        //remove it for real
                        SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                        localDataSet.RemoveAt(position);
                        SearchTabDialog.Instance.recycleSearchesAdapter.NotifyItemRemoved(position);
                    }
                }
            }
            if (isWishlist)
            {
                SearchTabHelper.SaveHeadersToSharedPrefs();
                SearchTabHelper.RemoveTabFromSharedPrefs(tabToRemove, SoulSeekState.ActiveActivityRef);
            }
            SearchFragment.Instance.SetCustomViewTabNumberImageViewState();
        }

        private void SearchTabLayout_Click(object sender, EventArgs e)
        {
            position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
            int tabToGoTo = localDataSet[position];
            SearchFragment.Instance.GoToTab(tabToGoTo, false);
            SearchTabDialog.Instance.Dismiss();
        }

        public SearchTabItemRecyclerAdapter(List<int> ti)
        {
            localDataSet = ti;
        }

    }


    public class SearchTabView : LinearLayout
    {
        public LinearLayout searchTabLayout;
        public ImageView removeSearch;
        private TextView lastSearchTerm;
        private TextView numResults;
        public SearchTabViewHolder ViewHolder;
        public int SearchId = int.MaxValue;
        public SearchTabView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tab_page_item, this, true);
            setupChildren();
        }
        public SearchTabView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tab_page_item, this, true);
            setupChildren();
        }
        public static SearchTabView inflate(ViewGroup parent)
        {
            SearchTabView itemView = (SearchTabView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.tab_page_item_dummy, parent, false);
            return itemView;
        }
        public void setupChildren()
        {
            lastSearchTerm = FindViewById<TextView>(Resource.Id.lastSearchTerm);
            numResults = FindViewById<TextView>(Resource.Id.resultsText);
            removeSearch = FindViewById<ImageView>(Resource.Id.searchTabItemRemove);
            searchTabLayout = FindViewById<LinearLayout>(Resource.Id.searchTabItemMain);
        }

        public void setItem(int i)
        {
            SearchTab searchTab = SearchTabHelper.SearchTabCollection[i];
            if (searchTab.SearchTarget == SearchTarget.Wishlist)
            {
                string timeString = "-";
                if (searchTab.LastRanTime != DateTime.MinValue)
                {
                    timeString = Helpers.GetNiceDateTime(searchTab.LastRanTime);
                }
                numResults.Text = searchTab.LastSearchResultsCount.ToString() + " Results, Last Ran: " + timeString;
            }
            else
            {
                numResults.Text = searchTab.LastSearchResultsCount.ToString() + " Results";
            }
            string lastTerm = searchTab.LastSearchTerm;
            if (lastTerm != string.Empty && lastTerm != null)
            {
                lastSearchTerm.Text = searchTab.LastSearchTerm;
            }
            else
            {
                lastSearchTerm.Text = "[No Search]";
            }
        }

    }

    public class SearchTabViewHolder : RecyclerView.ViewHolder
    {
        public SearchTabView searchTabView;


        public SearchTabViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            searchTabView = (SearchTabView)view;
            searchTabView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }

        public SearchTabView getUnderlyingView()
        {
            return searchTabView;
        }
    }


    public class SearchTabDialog : Android.Support.V4.App.DialogFragment, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private RecyclerView recyclerViewSearches = null;
        private RecyclerView recyclerViewWishlists = null;

        private LinearLayoutManager recycleSearchesLayoutManager = null;
        private LinearLayoutManager recycleWishlistsLayoutManager = null;

        public SearchTabItemRecyclerAdapter recycleSearchesAdapter = null;
        public SearchTabItemRecyclerAdapter recycleWishesAdapter = null;

        private Button newSearch = null;
        private Button newWishlist = null;

        private TextView wishlistTitle = null;

        public static SearchTabDialog Instance = null;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.search_tab_layout, container); //error inflating MaterialButton
        }

        public void OnGlobalLayout()
        {
            ////onresume the dialog isnt drawn so you dont know the height.
            ////this is to give the dialog a max height of 95%
            //Window window = Dialog.Window;
            //this.View.ViewTreeObserver.RemoveOnGlobalLayoutListener(this);
            ////if (Build.VERSION.SDK_INT < Build.VERSION_CODES.JELLY_BEAN)
            ////{
            ////    yourView.getViewTreeObserver().removeGlobalOnLayoutListener(this);
            ////}
            ////else
            ////{
            ////    yourView.getViewTreeObserver().removeOnGlobalLayoutListener(this);
            ////}
            //Point size = new Point();

            //Display display = window.WindowManager.DefaultDisplay;
            //display.GetSize(size);

            //int width = size.X;
            //int height = size.Y;

            //if(this.View.Height > (height * .95))
            //{
            //    window.SetLayout((int)(width * 0.90), (int)(height * 0.95));//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            //    window.SetGravity(GravityFlags.Center);
            //}

        }

        /// <summary>
        /// Called after on create view
        /// </summary>
        /// <param name="view"></param>
        /// <param name="savedInstanceState"></param>
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            Instance = this;
            //after opening up my soulseek app on my phone, 6 hours after I last used it, I got a nullref somewhere in here....
            base.OnViewCreated(view, savedInstanceState);
            //Dialog.SetTitle("File Info"); //is this needed in any way??
            this.Dialog.Window.SetBackgroundDrawable(SeekerApplication.GetDrawableFromAttribute(SoulSeekState.ActiveActivityRef, Resource.Attribute.the_rounded_corner_dialog_background_drawable));
            this.SetStyle((int)Android.App.DialogFragmentStyle.NoTitle, 0);
            //this.Dialog.SetTitle("Search Tab");
            recyclerViewSearches = view.FindViewById<RecyclerView>(Resource.Id.searchesRecyclerView);
            recyclerViewSearches.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recyclerViewWishlists = view.FindViewById<RecyclerView>(Resource.Id.wishlistsRecyclerView);
            recyclerViewWishlists.AddItemDecoration(new DividerItemDecoration(this.Context, DividerItemDecoration.Vertical));
            recycleSearchesLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewSearches.SetLayoutManager(recycleSearchesLayoutManager);
            recycleWishlistsLayoutManager = new LinearLayoutManager(this.Activity);
            recyclerViewWishlists.SetLayoutManager(recycleWishlistsLayoutManager);
            recycleSearchesAdapter = new SearchTabItemRecyclerAdapter(GetSearchTabIds());
            var wishTabIds = GetWishesTabIds();
            recycleWishesAdapter = new SearchTabItemRecyclerAdapter(wishTabIds);
            recycleWishesAdapter.ForWishlist = true;

            wishlistTitle = view.FindViewById<TextView>(Resource.Id.wishlistTitle);
            if (wishTabIds.Count == 0)
            {
                wishlistTitle.SetText(Resource.String.wishlist_empty_bold);
            }
            else
            {
                wishlistTitle.SetText(Resource.String.wishlist_bold);
            }
            recyclerViewSearches.SetAdapter(recycleSearchesAdapter);
            recyclerViewWishlists.SetAdapter(recycleWishesAdapter);
            newSearch = view.FindViewById<Button>(Resource.Id.createNewSearch);
            newSearch.Click += NewSearch_Click;
            //newSearch.CompoundDrawablePadding = 6;
            Android.Graphics.Drawables.Drawable drawable = null;
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp, this.Context.Theme);
            }
            else
            {
                drawable = this.Context.Resources.GetDrawable(Resource.Drawable.ic_add_black_24dp);
            }
            newSearch.SetCompoundDrawablesWithIntrinsicBounds(drawable, null, null, null);

        }

        //private void NewWishlist_Click(object sender, EventArgs e)
        //{
        //    SearchTabHelper.AddWishlistSearchTab();
        //}

        private void NewSearch_Click(object sender, EventArgs e)
        {
            int tabID = SearchTabHelper.AddSearchTab();
            SearchFragment.Instance.GoToTab(tabID, false);
            SearchTabDialog.Instance.Dismiss();
            SearchFragment.Instance.SetCustomViewTabNumberImageViewState();
        }

        public override void OnResume()
        {
            base.OnResume();

            MainActivity.LogDebug("OnResume ran");
            //this.View.ViewTreeObserver.AddOnGlobalLayoutListener(this);
            //Window window = Dialog.Window;//  getDialog().getWindow();
            
            //int currentWindowHeight = window.DecorView.Height;
            //int currentWindowWidth = window.DecorView.Width;

            //int xxx = this.View.RootView.Width;
            //int xxxxx = this.View.Width;

            Point size = new Point();
            Window window = Dialog.Window;
            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;
            //int height = size.Y;

            window.SetLayout((int)(width * 0.90), Android.Views.WindowManagerLayoutParams.WrapContent);//  window.WindowManager   WindowManager.LayoutParams.WRAP_CONTENT);
            window.SetGravity(GravityFlags.Center);
            MainActivity.LogDebug("OnResume End");
        }

        private List<int> GetSearchTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget != SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            return listOFIds;
        }

        public static List<int> GetWishesTabIds()
        {
            var listOFIds = SearchTabHelper.SearchTabCollection.Where((pair1) => pair1.Value.SearchTarget == SearchTarget.Wishlist).Select((pair1) => pair1.Key).ToList();
            listOFIds.Sort();
            listOFIds.Reverse();
            return listOFIds;
        }
    }

    //    private void Search_Click(object sender, EventArgs e)
    //    {
    //        MainActivity.LogDebug("Search_Click");
    //        if (!SoulSeekState.currentlyLoggedIn)
    //        {
    //            Toast tst = Toast.MakeText(Context, "Must log in before searching", ToastLength.Long);
    //            tst.Show();
    //        }
    //        else if (MainActivity.CurrentlyLoggedInButDisconnectedState())
    //        {
    //            Task t;
    //            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
    //            {
    //                return;
    //            }
    //            t.ContinueWith(new Action<Task>((Task t) =>
    //            {
    //                if (t.IsFaulted)
    //                {
    //                    SoulSeekState.MainActivityRef.RunOnUiThread(() => { 

    //                        Toast.MakeText(SoulSeekState.MainActivityRef, "Failed to connect.", ToastLength.Short).Show(); 

    //                        });
    //                    return;
    //                }
    //                SoulSeekState.MainActivityRef.RunOnUiThread(SearchLogic);

    //            }));
    //        }
    //        else
    //        {

    //            SearchLogic();
    //        }
    //    }
    //}

    //public class SearchAdapter : ArrayAdapter<SearchResponse>
    //{
    //    List<int> oppositePositions = new List<int>();
    //    public SearchAdapter(Context c, List<SearchResponse> items) : base(c, 0, items)
    //    {
    //        oppositePositions = new List<int>();
    //    }

    //    public override View GetView(int position, View convertView, ViewGroup parent)
    //    {
    //        ISearchItemViewBase itemView = (ISearchItemViewBase)convertView;
    //        if (null == itemView)
    //        {
    //            switch (SearchFragment.SearchResultStyle)
    //            {
    //                case SearchResultStyleEnum.ExpandedAll:
    //                case SearchResultStyleEnum.CollapsedAll:
    //                    itemView = SearchItemViewExpandable.inflate(parent);
    //                    (itemView as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
    //                    (itemView as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
    //                    break;
    //                case SearchResultStyleEnum.Medium:
    //                    itemView = SearchItemViewMedium.inflate(parent);
    //                    break;
    //                case SearchResultStyleEnum.Minimal:
    //                    itemView = SearchItemViewMinimal.inflate(parent);
    //                    break;
    //            }
    //        }
    //        bool opposite = oppositePositions.Contains(position);
    //        itemView.setItem(GetItem(position), opposite); //this will do the right thing no matter what...


    //        //if(SearchFragment.SearchResultStyle==SearchResultStyleEnum.CollapsedAll)
    //        //{
    //        //    (itemView as IExpandable).Collapse();
    //        //}
    //        //else if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll)
    //        //{
    //        //    (itemView as IExpandable).Expand();
    //        //}

    //        //SETTING TOOLTIPTEXT does not allow list view item click!!! 
    //        //itemView.TooltipText = "Queue Length: " + GetItem(position).QueueLength + System.Environment.NewLine + "Free Upload Slots: " + GetItem(position).FreeUploadSlots;
    //        return itemView as View;
    //        //return base.GetView(position, convertView, parent);
    //    }

    //    private void CustomAdapter_Click1(object sender, EventArgs e)
    //    {
    //        MainActivity.LogInfoFirebase("CustomAdapter_Click1");
    //        int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
    //        SearchFragment.Instance.showEditDialog(position);
    //    }

    //    private void CustomAdapter_Click(object sender, EventArgs e)
    //    {
    //        //throw new NotImplementedException();
    //        int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
    //        var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
    //        var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
    //        if (v.Visibility == ViewStates.Gone)
    //        {
    //            img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
    //            v.Visibility = ViewStates.Visible;
    //            SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, GetItem(position));
    //            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
    //            {
    //                oppositePositions.Add(position);
    //                oppositePositions.Sort();
    //            }
    //            else
    //            {
    //                oppositePositions.Remove(position);
    //            }
    //        }
    //        else
    //        {
    //            img.Animate().RotationBy((float)(-180.0)).SetDuration(350).Start();
    //            v.Visibility = ViewStates.Gone;
    //            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
    //            {
    //                oppositePositions.Remove(position);
    //            }
    //            else
    //            {
    //                oppositePositions.Add(position);
    //                oppositePositions.Sort();
    //            }
    //        }
    //    }
    //}

    public interface ISearchItemViewBase
    {
        void setupChildren();
        SearchFragment.SearchViewHolder ViewHolder
        {
            get;set;
        }
        void setItem(SearchResponse item, int opposite);
    }

    public class SearchItemViewMinimal : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        //private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        public SearchItemViewMinimal(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }
        public SearchItemViewMinimal(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }

        public static SearchItemViewMinimal inflate(ViewGroup parent)
        {
            SearchItemViewMinimal itemView = (SearchItemViewMinimal)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewminimal_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textView1);
            viewFoldername = FindViewById<TextView>(Resource.Id.textView2);
            viewSpeed = FindViewById<TextView>(Resource.Id.textView3);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString(); //kb/s

            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }
    }

    public class SearchItemViewMedium : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }

        public static SearchItemViewMedium inflate(ViewGroup parent)
        {
            SearchItemViewMedium itemView = (SearchItemViewMedium)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmedium_dummy, parent, false);
            return itemView;
        }
        private bool hideLocked = false;
        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = SoulSeekState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameForSearchResult(item); //todo maybe also cache this...
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SlskHelp.CommonHelpers.STRINGS_KBS; //kbs
            viewFileType.Text = item.GetDominantFileType(hideLocked);
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            //line separated..
            //viewUsername.Text = item.Username + "  |  " + Helpers.GetDominantFileType(item) + "  |  " + (item.UploadSpeed / 1024).ToString() + "kbs";

        }


    }

    public interface IExpandable
    {
        void Expand();
        void Collapse();
    }

    public class ExpandableSearchItemFilesAdapter : ArrayAdapter<Soulseek.File>
    {
        public ExpandableSearchItemFilesAdapter(Context c, List<Soulseek.File> files) : base(c, 0, files.ToArray())
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView itemView = (TextView)convertView;
            if (null == itemView)
            {
                itemView = new TextView(this.Context);//ItemView.inflate(parent);
            }
            itemView.Text = GetItem(position).Filename;
            return itemView;
        }
    }

    public class SearchItemViewExpandable : RelativeLayout, ISearchItemViewBase, IExpandable
    {
        private TextView viewQueue;
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private ImageView imageViewExpandable;
        private LinearLayout viewToHideShow;

        public SearchFragment.SearchAdapterRecyclerVersion AdapterRef;


        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        //private TextView viewQueue;
        public SearchItemViewExpandable(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }
        public SearchItemViewExpandable(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }

        public static SearchItemViewExpandable inflate(ViewGroup parent)
        {
            SearchItemViewExpandable itemView = (SearchItemViewExpandable)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_result_exampandable_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = SoulSeekState.HideLockedResultsInSearch;
        }
        private bool hideLocked = false;
        public static void PopulateFilesListView(LinearLayout viewToHideShow, SearchResponse item)
        {
            viewToHideShow.RemoveAllViews();
            foreach (Soulseek.File f in item.GetFiles(SoulSeekState.HideLockedResultsInSearch))
            {
                TextView tv = new TextView(SoulSeekState.MainActivityRef);
                SetTextColor(tv, SoulSeekState.MainActivityRef);
                tv.Text = Helpers.GetFileNameFromFile(f.Filename);
                viewToHideShow.AddView(tv);
            }
        }
        
        public void setItem(SearchResponse item, int position)
        {
            bool opposite = this.AdapterRef.oppositePositions.Contains(position);
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + "kbs"; //kb/s
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            viewFileType.Text = item.GetDominantFileType(hideLocked);

            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll && opposite ||
                SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll && !opposite)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                PopulateFilesListView(viewToHideShow, item);
                //imageViewExpandable.ClearAnimation();
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_less_white_32_dp);
                //viewToHideShow.Adapter = new ExpandableSearchItemFilesAdapter(this.Context,item.Files.ToList());
            }
            else
            {
                viewToHideShow.Visibility = ViewStates.Gone;
                imageViewExpandable.Rotation = 0;
                //imageViewExpandable.ClearAnimation(); //THIS DOES NOT CLEAR THE ROTATE.
                //AFTER doing a rotation animation, the rotation is still there in the 
                //imageview state.  just check float rot = imageViewExpandable.Rotation;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_more_black_32dp);
            }
            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
        }

        public static Color GetColorFromAttribute(Context c, int attr, Resources.Theme overrideTheme = null)
        {
            var typedValue = new TypedValue();
            if(overrideTheme != null)
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
                if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
                {
                    return GetColorFromInteger(ContextCompat.GetColor(c, typedValue.ResourceId));
                }
                else
                {
                    return c.Resources.GetColor(typedValue.ResourceId);
                }
            }
        }

        public static Color GetColorFromInteger(int color)
        {
            return Color.Rgb(Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static void SetTextColor(TextView tv, Context c)
        {
            tv.SetTextColor(GetColorFromAttribute(c, Resource.Attribute.cellTextColor));
        }
    }




    //public class RowItem
    //{
    //    public string Title;
    //    public string Desc;
    //    public RowItem() { }
    //    public RowItem(string title, string desc)
    //    {
    //        Title = title;Desc = desc;
    //    }
    //}
    [Serializable]
    public class TransferItem : ITransferItem
    {
        public string GetDisplayName()
        {
            return Filename;
        }

        public string GetFolderName()
        {
            return FolderName;
        }

        public string GetDisplayFolderName()
        {
            //this is similar to QT (in the case of Seeker multiple subdirectories)
            //but not quite.
            //QT will show subdirs/complete/Soulseek Downloads/Music/H: (where everything after subdirs is your download folder)
            //whereas we just show subdirs
            //subdirs is folder name in both cases for single folder, 
            // and say (01 / 2020 / test_folder) for nested.
            if(GetDirectoryLevel()==1)
            {
                //they are the same
                return FolderName;
            }
            else
            {
                //split reverse.
                var reversedArray = this.FolderName.Split('\\').Reverse();
                return string.Join('\\',reversedArray);
            }
        }

        public int GetDirectoryLevel()
        {
            //just parent folder = level 1 (search result and browse single dir case)
            //grandparent = level 2 (browse download subdirs case - i.e. Album, Album > covers)
            //etc.
            if(this.FolderName == null || !this.FolderName.Contains('\\'))
            {
                return 1;
            }
            return this.FolderName.Split('\\').Count();
        }

        public string GetUsername()
        {
            return Username;
        }

        public TimeSpan? GetRemainingTime()
        {
            return RemainingTime;
        }

        public int GetQueueLength()
        {
            return queuelength;
        }

        public bool IsUpload()
        {
            return isUpload;
        }

        public double GetAvgSpeed()
        {
            return AvgSpeed;
        }

        public long? GetSizeForDL()
        {
            if(this.Size==-1)
            {
                return null;
            }
            else
            {
                return this.Size;
            }
        }

        public string Filename;
        public string Username;
        public string FolderName;
        public string FullFilename;
        public int Progress;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public TimeSpan? RemainingTime;
        public bool Failed;
        public TransferStates State;
        public long Size;
        
        public bool isUpload;
        private int queuelength = int.MaxValue;
        public bool CancelAndRetryFlag = false;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public double AvgSpeed = 0;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool CancelAndClearFlag = false;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool InProcessing = false; //whether its currently a task in Soulseek.Net.  so from Intialized / Queued to the end of the main download continuation task...
        public int QueueLength
        {
            get
            {
                return queuelength;
            }
            set
            {
                queuelength = value;
            }
        }
        public string FinalUri = string.Empty; //final uri of downloaded item
        public string IncompleteParentUri = null; //incomplete uri of item.  will be null if successfully downloaded or not yet created.
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public CancellationTokenSource CancellationTokenSource = null;
    }


    /**
    Notes on Queue Position:
    The default queue position should be int.MaxValue (which we display as not known) not 0.  
    This is the case on QT where we download from an offline user, 
      or in general when we are queued by a user that does not send a queue position (slskd?).
    Both QT and Nicotine display it as "Queued" and then without a queue position (rather than queue position of 0).

    If we are downloading from a user with queue and they then go offline, the QT behavior is to still show "Queued" (nothing changes),
      the nicotine behavior is to change it to "User Logged Off".  I think nicotine behavior is more descriptive and helpful.
    **/

    public interface ITransferItemView
    {
        public ITransferItem InnerTransferItem { get; set; }

        public void setupChildren();

        public void setItem(ITransferItem ti, bool isInBatchMode);

        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }

        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView();

        public TextView GetProgressSizeTextView();

        public bool GetShowProgressSize();

        public bool GetShowSpeed();
    }

    public class TransferItemViewFolder : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewCurrentFilename;
        private TextView viewNumRemaining;

        private TextView viewProgressSize;
        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return viewProgressSize;
        }

        public bool showSize;
        public bool showSpeed;
            
        public bool GetShowProgressSize()
        {
            return showSize;
        }

        public bool GetShowSpeed()
        {
            return showSpeed;
        }

        public TransferItemViewFolder(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }
        public TransferItemViewFolder(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder_showProgressSize, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_folder, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewFolder inflate(ViewGroup parent, bool _showSize, bool _showSpeed)
        {
            TransferItemViewFolder itemView = null;
            if(_showSize)
            {
               itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy_showSizeProgress, parent, false);
            }
            else
            {
                itemView = (TransferItemViewFolder)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_view_folder_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSize = _showSize;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFoldername = FindViewById<TextView>(Resource.Id.textViewFoldername);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);
            viewProgressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            viewNumRemaining = FindViewById<TextView>(Resource.Id.filesRemaining);
            viewCurrentFilename = FindViewById<TextView>(Resource.Id.currentFile);
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
        }

        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            FolderItem folderItem = item as FolderItem;
            viewFoldername.Text = folderItem.GetDisplayFolderName();
            var state = folderItem.GetState(out bool isFailed, out _);

            
            TransferViewHelper.SetViewStatusText(viewStatus, state, item.IsUpload(), true);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, item, state, true); //TODOTODO
            TransferViewHelper.SetAdditionalFolderInfoState(viewNumRemaining, viewCurrentFilename, folderItem, state);
            int prog = folderItem.GetFolderProgress(out long totalBytes, out _);
            progressBar.Progress = prog;
            if(this.showSize)
            {
                (viewProgressSize as TransfersFragment.ProgressSizeTextView).Progress = prog;
                TransferViewHelper.SetSizeText(viewProgressSize, prog, totalBytes);
            }
            

            viewUsername.Text = folderItem.Username;
            if (item.IsUpload() && state.HasFlag(TransferStates.Cancelled))
            {
                isFailed = true;
            }
            if (isFailed)//state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut))
            {
                progressBar.Progress = 100;
                if (this.showSize)
                {
                    (viewProgressSize as AndriodApp1.TransfersFragment.ProgressSizeTextView).Progress = 100;
                }
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
        }
    }




    public class TransferViewHelper
    {
        /// <summary>
        /// In Progress = InProgress proper, initializing, requested. 
        /// If In Progress or Queued you should be able to pause it (the official client lets you).
        /// </summary>
        /// <param name="transferItems"></param>
        /// <param name="numInProgress"></param>
        /// <param name="numFailed"></param>
        /// <param name="numPaused"></param>
        /// <param name="numSucceeded"></param>
        public static void GetStatusNumbers(IEnumerable<TransferItem> transferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued)
        {
            numInProgress = 0;
            numFailed = 0;
            numPaused = 0;
            numSucceeded = 0;
            numQueued = 0;
            lock (transferItems)
            {
                foreach (var ti in transferItems)
                {
                    if(ti.State.HasFlag(TransferStates.Queued))
                    {
                        numQueued++;
                    }
                    else if (ti.State.HasFlag(TransferStates.InProgress) || ti.State.HasFlag(TransferStates.Initializing) || ti.State.HasFlag(TransferStates.Requested))
                    {
                        numInProgress++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                    {
                        numFailed++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Cancelled))
                    {
                        numPaused++;
                    }
                    else if (ti.State.HasFlag(TransferStates.Succeeded))
                    {
                        numSucceeded++;
                    }
                }
            }
        }


        public static void SetAdditionalFolderInfoState(TextView filesLongStatus, TextView currentFile, FolderItem fi, TransferStates folderState)
        {
            //if in progress, X files remaining, Current File:
            //if in queue, ^ ^ (or initializing, requesting, basically if in progress in the literal sense)
            //if completed, X files suceeded - hide p2
            //if failed, X files suceeded (if applicable), X files failed. - hide p2
            //if paused, X files suceeded, X failed, X paused. - hide p2
            if (folderState.HasFlag(TransferStates.InProgress) || folderState.HasFlag(TransferStates.Queued) || folderState.HasFlag(TransferStates.Initializing) || folderState.HasFlag(TransferStates.Requested))
            {
                int numRemaining = 0;
                string currentFilename = string.Empty;
                int total = 0;
                lock (fi.TransferItems)
                {
                    foreach (var ti in fi.TransferItems)
                    {
                        total++;
                        if (!(ti.State.HasFlag(TransferStates.Completed)))
                        {
                            numRemaining++;
                        }
                        if (ti.State.HasFlag(TransferStates.InProgress))
                        {
                            currentFilename = ti.Filename;
                        }
                    }
                    if (currentFilename == string.Empty) //init or requested case
                    {
                        currentFilename = fi.TransferItems.First().Filename;
                    }
                }

                filesLongStatus.Text = string.Format("{0} of {1} remaining", numRemaining, total);
                currentFile.Visibility = ViewStates.Visible;
                currentFile.Text = string.Format("Current: {0}", currentFilename);
            }
            else if (folderState.HasFlag(TransferStates.Succeeded))
            {
                int numSucceeded = fi.TransferItems.Count;

                filesLongStatus.Text = string.Format("All {0} succeeded", numSucceeded);
                currentFile.Visibility = ViewStates.Gone;
            }
            else if (folderState.HasFlag(TransferStates.Errored) || folderState.HasFlag(TransferStates.Rejected) || folderState.HasFlag(TransferStates.TimedOut))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored) || ti.State.HasFlag(TransferStates.Rejected) || ti.State.HasFlag(TransferStates.TimedOut))
                        {
                            numFailed++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                    }
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else if (folderState.HasFlag(TransferStates.Cancelled))
            {
                int numFailed = 0;
                int numSucceeded = 0;
                int numPaused = 0;
                lock (fi.TransferItems)
                {

                    foreach (var ti in fi.TransferItems)
                    {
                        if (ti.State.HasFlag(TransferStates.Succeeded))
                        {
                            numSucceeded++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Cancelled))
                        {
                            numPaused++;
                        }
                        else if (ti.State.HasFlag(TransferStates.Errored))
                        {
                            numFailed++;
                        }
                    }
                }

                if (numPaused == 0)
                {
                    //error
                }

                SetFilesLongStatusIfNotInProgress(filesLongStatus, fi, numFailed, numSucceeded, numPaused);
                currentFile.Visibility = ViewStates.Gone;
                //set views + visi
            }
            else
            {

            }

        }

        private static void SetFilesLongStatusIfNotInProgress(TextView filesLongStatus, FolderItem fi, int numFailed, int numSucceeded, int numPaused)
        {
            string cancelledString = fi.IsUpload() ? "aborted" : "paused";
            // 0 0 0 isnt one.
            if (numSucceeded == 0 && numFailed == 0 && numPaused != 0) //all paused
            {
                filesLongStatus.Text = string.Format("All {0} {1}", numPaused, cancelledString);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused == 0) //all failed
            {
                filesLongStatus.Text = string.Format("All {0} failed", numFailed);
            }
            else if (numSucceeded == 0 && numFailed != 0 && numPaused != 0) //all failed or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} failed", numPaused, cancelledString, numFailed);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused == 0) //all succeeded
            {
                filesLongStatus.Text = string.Format("All {0} succeeded", numSucceeded);
            }
            else if (numSucceeded != 0 && numFailed == 0 && numPaused != 0) //all succeeded or paused
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} succeeded", numPaused, cancelledString, numSucceeded);
            }
            else if (numSucceeded != 0 && numFailed != 0 && numPaused == 0) //all succeeded or failed
            {
                filesLongStatus.Text = string.Format("{0} failed, {1} succeeded", numFailed, numSucceeded);
            }
            else //all
            {
                filesLongStatus.Text = string.Format("{0} {1}, {2} succeeded, {3} failed", numPaused, cancelledString, numSucceeded, numFailed);
            }
        }



        public static void SetSizeText(TextView size, int progress, long sizeBytes)
        {
            if(progress == 100)
            {
                if(sizeBytes > 1024*1024)
                {
                    size.Text = System.String.Format("{0:F1}mb", sizeBytes / 1048576.0);
                }
                else if(sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}kb", sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
            }
            else
            {
                long bytesTransferred = progress * sizeBytes;
                if (sizeBytes > 1024 * 1024)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}mb", bytesTransferred / (1048576.0 * 100.0), sizeBytes / 1048576.0);
                }
                else if (sizeBytes >= 0)
                {
                    size.Text = System.String.Format("{0:F1}/{1:F1}kb", bytesTransferred / (1024.0 * 100.0), sizeBytes / 1024.0);
                }
                else
                {
                    size.Text = "??";
                }
            }
        }


        public static void SetViewStatusText(TextView viewStatus, TransferStates state, bool isUpload, bool isFolder)
        {
            if (state.HasFlag(TransferStates.Queued))
            {
                viewStatus.SetText(Resource.String.in_queue);
            }
            else if (state.HasFlag(TransferStates.Cancelled))
            {
                if (isUpload)
                {
                    viewStatus.Text = "Aborted";
                }
                else
                {
                    viewStatus.SetText(Resource.String.paused);
                }
            }
            else if(isFolder && state.HasFlag(TransferStates.Rejected)) //if is folder we put the extra info here, else we put it in the additional status TextView
            {
                if (isUpload)
                {
                    viewStatus.Text = "Failed - Cancelled"; //if the user on the other end cancelled / paused / removed it.
                }
                else
                {
                    viewStatus.SetText(Resource.String.failed_denied);
                }
            }
            else if (isFolder && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatus.SetText(Resource.String.failed_user_offline);
            }
            else if (isFolder && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatus.Text = "Failed - Cannot Connect"; 
                //"cannot connect" is too long for average screen. but the root problem needs to be fixed (for folder combine two TextView into one with padding???? TODO)
            }
            else if (state.HasFlag(TransferStates.Rejected) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Errored))
            {
                viewStatus.SetText(Resource.String.failed);
            }
            else if (state.HasFlag(TransferStates.Initializing) || state.HasFlag(TransferStates.Requested))  //item.State.HasFlag(TransferStates.None) captures EVERYTHING!!
            {
                viewStatus.SetText(Resource.String.not_started);
            }
            else if (state.HasFlag(TransferStates.InProgress))
            {
                viewStatus.SetText(Resource.String.in_progress);
            }
            else if (state.HasFlag(TransferStates.Succeeded))
            {
                viewStatus.SetText(Resource.String.completed);
            }
            else
            {

            }
        }


        public static string GetTimeRemainingString(TimeSpan? timeSpan)
        {
            if (timeSpan == null)
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.unknown);
            }
            else
            {
                string[] hms = timeSpan.ToString().Split(':');
                string h = hms[0].TrimStart('0');
                if (h == string.Empty)
                {
                    h = "0";
                }
                string m = hms[1].TrimStart('0');
                if (m == string.Empty)
                {
                    m = "0";
                }
                string s = hms[2].TrimStart('0');
                if (s.Contains('.'))
                {
                    s = s.Substring(0, s.IndexOf('.'));
                }
                if (s == string.Empty)
                {
                    s = "0";
                }
                //it will always be length 3.  if the seconds is more than a day it will be like "[13.21:53:20]" and if just 2 it will be like "[00:00:02]"
                if (h != "0")
                {
                    //we have hours
                    return h + "h:" + m + "m:" + s + "s";
                }
                else if (m != "0")
                {
                    return m + "m:" + s + "s";
                }
                else
                {
                    return s + "s";
                }
            }
        }

        public static void SetAdditionalStatusText(TextView viewStatusAdditionalInfo, ITransferItem item, TransferStates state, bool showSpeed)
        {
            if (state.HasFlag(TransferStates.InProgress))
            {
                //Helpers.GetTransferSpeedString(avgSpeedBytes);
                if(showSpeed)
                {
                    viewStatusAdditionalInfo.Text = Helpers.GetTransferSpeedString(item.GetAvgSpeed()) + "  •  " + GetTimeRemainingString(item.GetRemainingTime());
                }
                else
                {
                    viewStatusAdditionalInfo.Text = GetTimeRemainingString(item.GetRemainingTime());
                }
            }
            else if (state.HasFlag(TransferStates.Queued) && !(item.IsUpload()))
            {
                int queueLen = item.GetQueueLength();
                if(queueLen == int.MaxValue) //i.e. unknown
                {
                    viewStatusAdditionalInfo.Text = string.Empty;
                }
                else
                {
                    viewStatusAdditionalInfo.Text = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.Rejected))
            {
                if(item.IsUpload())
                {
                    viewStatusAdditionalInfo.Text = "Cancelled";
                }
                else
                {
                    viewStatusAdditionalInfo.Text = "Denied";
                }
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.TimedOut))
            {
                viewStatusAdditionalInfo.Text = "Timed Out";
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.UserOffline))
            {
                viewStatusAdditionalInfo.Text = "User is Offline";
            }
            else if (item is TransferItem && state.HasFlag(TransferStates.CannotConnect))
            {
                viewStatusAdditionalInfo.Text = "Cannot Connect";
            }
            else
            {
                viewStatusAdditionalInfo.Text = "";
            }
        }
    }


    public class TransferItemViewDetails : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewFilename;

        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.
        private TextView progressSize; //if in Queue then show position, if In Progress show time remaining.

        public ITransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar { get; set; }

        public TextView GetAdditionalStatusInfoView()
        {
            return viewStatusAdditionalInfo;
        }

        public TextView GetProgressSizeTextView()
        {
            return progressSize;
        }

        public bool GetShowProgressSize()
        {
            return showSizes;
        }
        public bool GetShowSpeed()
        {
            return showSpeed;
        }


        public bool showSpeed;
        public bool showSizes;
        public TransferItemViewDetails(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if(_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }
            
            setupChildren();
        }
        public TransferItemViewDetails(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            bool _showSizes = attrs.GetAttributeBooleanValue("http://schemas.android.com/apk/res-auto", "show_progress_size", false);

            if (_showSizes)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed_sizeProgressBar, this, true);
            }
            else
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            }

            setupChildren();
        }

        public static TransferItemViewDetails inflate(ViewGroup parent, bool _showSizes, bool _showSpeed)
        {
            
            TransferItemViewDetails itemView = null;
            if(_showSizes)
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy_showProgressSize, parent, false);
            }
            else
            {
                itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy, parent, false);
            }
            itemView.showSpeed = _showSpeed;
            itemView.showSizes = _showSizes;
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFilename = FindViewById<TextView>(Resource.Id.textViewFileName);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);

            progressSize = FindViewById<TextView>(Resource.Id.textViewProgressSize);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);

        }




        public void setItem(ITransferItem item, bool isInBatchMode)
        {
            InnerTransferItem = item;
            TransferItem ti = item as TransferItem;
            viewFilename.Text = ti.Filename;
            progressBar.Progress = ti.Progress;
            if(this.showSizes)
            {
                TransferViewHelper.SetSizeText(progressSize, ti.Progress, ti.Size);
            }
            TransferViewHelper.SetViewStatusText(viewStatus, ti.State, ti.IsUpload(), false);
            TransferViewHelper.SetAdditionalStatusText(viewStatusAdditionalInfo, ti, ti.State, this.showSpeed);
            viewUsername.Text = ti.Username;
            bool isFailedOrAborted = ti.Failed;
            if (item.IsUpload() && ti.State.HasFlag(TransferStates.Cancelled))
            {
                isFailedOrAborted = true;
            }
            if (isFailedOrAborted)
            {
                progressBar.Progress = 100;
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618
            }
            else
            {
#pragma warning disable 0618
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
                }
                else
                {
                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
                }
#pragma warning restore 0618

            }

            if (isInBatchMode && TransfersFragment.BatchSelectedItems.Contains(this.ViewHolder.AbsoluteAdapterPosition))
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, null);
                    //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
            else
            {
                //this.Background
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                                           //e.View.Background = Resources.GetDrawable(Resource.Drawable.cell_shape_dldiag, null);
                }
                else
                {
                    this.Background = null;//Resources.GetDrawable(Resource.Color.cellback);
                                           //e.View.Background = Resources.GetDrawable(Resource.Color.cellback);
                }
            }
        }

        public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu);
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo) menuInfo;
            menu.Add(0, 0, 0, Resource.String.retry_dl);
            menu.Add(1, 1, 1, Resource.String.clear_from_list);
            menu.Add(2, 2, 2, Resource.String.cancel_and_clear);
        }
    }




    public class CustomLinearLayoutManager : LinearLayoutManager
    {
        public CustomLinearLayoutManager(Context c) : base(c)
        {

        }
        //Generate constructors

        public override bool SupportsPredictiveItemAnimations()
        {
            bool old = base.SupportsPredictiveItemAnimations();
            return false;
        }

    }




    public class TransfersFragment : Fragment, PopupMenu.IOnMenuItemClickListener
    {
        private View rootView = null;

        /// <summary>
        /// We add these to this dict so we can (1) AddUser to get their statuses and (2) efficiently check them when
        /// we get user status changed events.
        /// This dict may contain a greater number of users than strictly necessary.  as it is just used to save time, 
        /// and having additional users here will not cause any issues.  (i.e. in case where other user was offline then the 
        /// user cleared that download, no harm as it will check and see there are no downloads to retry and just remove user)
        /// </summary>
        public static Dictionary<string, byte> UsersWhereDownloadFailedDueToOffline = new Dictionary<string, byte>();
        public static void AddToUserOffline(string username)
        {
            if(UsersWhereDownloadFailedDueToOffline.ContainsKey(username))
            {
                return;
            }
            else
            {
                lock (TransfersFragment.UsersWhereDownloadFailedDueToOffline)
                {
                    UsersWhereDownloadFailedDueToOffline[username] = 0x0;
                }
                try
                {
                    SoulSeekState.SoulseekClient.AddUserAsync(username);
                }
                catch(System.Exception)
                {
                    // noop
                    // if user is not logged in then next time they log in the user will be added...
                }
            }
        }

        public static TransferItemManager TransferItemManagerDL; //for downloads
        public static TransferItemManager TransferItemManagerUploads; //for uploads
        public static TransferItemManagerWrapper TransferItemManagerWrapped;

        public static bool GroupByFolder = false;

        public static int ScrollPositionBeforeMovingIntoFolder = int.MinValue;
        public static int ScrollOffsetBeforeMovingIntoFolder = int.MinValue;
        public static FolderItem CurrentlySelectedDLFolder = null;
        public static FolderItem CurrentlySelectedUploadFolder = null;
        public static bool CurrentlyInFolder()
        {
            if (CurrentlySelectedDLFolder == null && CurrentlySelectedUploadFolder == null)
            {
                return false;
            }
            return true;
        }
        public static FolderItem GetCurrentlySelectedFolder()
        {
            if (InUploadsMode)
            {
                return CurrentlySelectedUploadFolder;
            }
            else
            {
                return CurrentlySelectedDLFolder;
            }
        }
        public static volatile bool InUploadsMode = false;

        //private ListView primaryListView = null;
        private TextView noTransfers = null;
        private ISharedPreferences sharedPreferences = null;
        private static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> ProgressUpdatedThrottler = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
        public static int THROTTLE_PROGRESS_UPDATED_RATE = 200;//in ms;

        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if (menuVisible)
            {
                var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if (navigator != null)
                {
                    navigator.Menu.GetItem(2).SetCheckable(true);
                    navigator.Menu.GetItem(2).SetChecked(true);
                }
            }
            base.SetMenuVisibility(menuVisible);
        }


        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.transfers_menu, menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            if (InUploadsMode)
            {
                menu.FindItem(Resource.Id.action_clear_all_complete_and_aborted).SetVisible(true);
                menu.FindItem(Resource.Id.action_abort_all).SetVisible(true);
                if (CurrentlySelectedUploadFolder == null)
                {
                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(true);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(true);


                    //todo: menu options.  clear all completed and aborted
                    //todo: menu options.  abort all - are you sure? dialog

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);

                }
                else
                {


                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(false);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(false);

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);
                }
            }
            else
            {
                menu.FindItem(Resource.Id.action_abort_all).SetVisible(false); //nullref here...
                menu.FindItem(Resource.Id.action_clear_all_complete_and_aborted).SetVisible(false);
                if (CurrentlySelectedDLFolder == null)
                {
                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(true);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(true);

                    menu.FindItem(Resource.Id.action_resume_all).SetVisible(true);
                    menu.FindItem(Resource.Id.action_pause_all).SetVisible(true);
                    menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(true);
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(true);
                    menu.FindItem(Resource.Id.retry_all_failed).SetVisible(true);
                }
                else
                {

                    TransferViewHelper.GetStatusNumbers(CurrentlySelectedDLFolder.TransferItems, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);
                    //var state = CurrentlySelectedDLFolder.GetState(out bool isFailed);

                    if (numPaused != 0)
                    {
                        menu.FindItem(Resource.Id.action_resume_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_resume_all).SetVisible(false);
                    }

                    if (numInProgress != 0 || numQueued != 0)
                    {
                        menu.FindItem(Resource.Id.action_pause_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_pause_all).SetVisible(false);
                    }

                    if(numSucceeded != 0)
                    {
                        menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_clear_all_complete).SetVisible(false);
                    }

                    if(numFailed != 0)
                    {
                        menu.FindItem(Resource.Id.retry_all_failed).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.retry_all_failed).SetVisible(false);
                    }

                    //i.e. if there are any not in the succeeded state that clear all complete would clear
                    if (numFailed != 0 || numInProgress != 0 || numPaused != 0)
                    {
                        menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(true);
                    }
                    else
                    {
                        menu.FindItem(Resource.Id.action_cancel_and_clear_all).SetVisible(false);
                    }

                    menu.FindItem(Resource.Id.action_toggle_group_by).SetVisible(false);
                    menu.FindItem(Resource.Id.action_toggle_download_upload).SetVisible(false);
                }
            }

            if (SoulSeekState.TransferViewShowSizes)
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle("Hide Size");
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_size).SetTitle("Show Size");
            }

            if (SoulSeekState.TransferViewShowSpeed)
            {
                menu.FindItem(Resource.Id.action_show_speed).SetTitle("Hide Speed");
            }
            else
            {
                menu.FindItem(Resource.Id.action_show_speed).SetTitle("Show Speed");
            }

            base.OnPrepareOptionsMenu(menu);
        }

        public void RefreshForModeSwitch()
        {
            SetRecyclerAdapter();
            SoulSeekState.MainActivityRef.SetTransferSupportActionBarState();
            SetNoTransfersMessage();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    SoulSeekState.MainActivityRef.OnBackPressed();
                    return true;
                case Resource.Id.action_clear_all_complete: //clear all complete
                    MainActivity.LogInfoFirebase("Clear All Complete Pressed");
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerDL.ClearAllCompleteFromFolder(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_group_by: //toggle group by. group / ungroup by folder.
                    GroupByFolder = !GroupByFolder;
                    SetRecyclerAdapter();
                    return true;
                case Resource.Id.action_show_size:
                    SoulSeekState.TransferViewShowSizes = !SoulSeekState.TransferViewShowSizes;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_show_speed:
                    SoulSeekState.TransferViewShowSpeed = !SoulSeekState.TransferViewShowSpeed;
                    SetRecyclerAdapter(true);
                    return true;
                case Resource.Id.action_clear_all_complete_and_aborted:
                    MainActivity.LogInfoFirebase("Clear All Complete Pressed");
                    if (CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.ClearAllComplete();
                    }
                    else
                    {
                        TransferItemManagerUploads.ClearAllCompleteFromFolder(CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_toggle_download_upload:
                    InUploadsMode = !InUploadsMode;
                    RefreshForModeSwitch();
                    return true;
                case Resource.Id.action_cancel_and_clear_all: //cancel and clear all
                    MainActivity.LogInfoFirebase("action_cancel_and_clear_all Pressed");
                    SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll(true);
                        TransferItemManagerDL.ClearAllAndClean();
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(CurrentlySelectedDLFolder, true);
                        TransferItemManagerDL.ClearAllFromFolderAndClean(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_abort_all: //abort all
                    MainActivity.LogInfoFirebase("action_abort_all_pressed");
                    SoulSeekState.AbortAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedUploadFolder == null)
                    {
                        TransferItemManagerUploads.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerUploads.CancelFolder(CurrentlySelectedUploadFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_pause_all:
                    MainActivity.LogInfoFirebase("pause all Pressed");
                    SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if (CurrentlySelectedDLFolder == null)
                    {
                        TransferItemManagerDL.CancelAll();
                    }
                    else
                    {
                        TransferItemManagerDL.CancelFolder(CurrentlySelectedDLFolder);
                    }
                    refreshListView();
                    return true;
                case Resource.Id.action_resume_all:
                    MainActivity.LogInfoFirebase("resume all Pressed");
                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        Task t;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                        {
                            return base.OnContextItemSelected(item);
                        }
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                                {
                                    if (Context != null)
                                    {
                                        Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                    }
                                    else
                                    {
                                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                    }

                                });
                                return;
                            }
                            if (CurrentlySelectedDLFolder == null)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false, true, null, false); });
                            }
                            else
                            {
                                DownloadRetryAllConditionLogic(false, false, CurrentlySelectedDLFolder, false);
                            }
                        }));
                    }
                    else
                    {
                        if (CurrentlySelectedDLFolder == null)
                        {
                            DownloadRetryAllConditionLogic(false, true, null, false);
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(false, false, CurrentlySelectedDLFolder, false);
                        }
                    }
                    return true;
                case Resource.Id.retry_all_failed:
                    RetryAllConditionEntry(true, false);
                    return true;
                case Resource.Id.batch_select:
                    TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                    ForceOutIfZeroSelected = false;
                    //Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)SoulSeekState.MainActivityRef.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                    //TransfersActionMode = myToolbar.StartActionMode(TransfersActionModeCallback);
                    TransfersActionMode = SoulSeekState.MainActivityRef.StartActionMode(TransfersActionModeCallback);
                    recyclerTransferAdapter.IsInBatchSelectMode = true;
                    TransfersActionMode.Title = string.Format("0 Selected");
                    TransfersActionMode.Invalidate();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void RetryAllConditionEntry(bool failed, bool batchSelectedOnly)
        {
            //get the batch selected transfer items ahead of time, because the next thing we do is clear the batch selected indices
            //AND also the user can add or clear transfers in the case where we continue on logging in for this, so the positions will be wrong..
            var listTi = batchSelectedOnly ? GetBatchSelectedItemsForRetryCondition(failed) : null;
            MainActivity.LogInfoFirebase("retry all failed Pressed batch? " + batchSelectedOnly);
            if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                        {
                            if (Context != null)
                            {
                                Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            }
                            else
                            {
                                Toast.MakeText(SoulSeekState.ActiveActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                            }

                        });
                        return;
                    }
                    if (CurrentlySelectedDLFolder == null)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi); });
                    }
                    else
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(failed, false, CurrentlySelectedDLFolder, batchSelectedOnly, listTi); });
                    }
                }));
            }
            else
            {
                if (CurrentlySelectedDLFolder == null)
                {
                    DownloadRetryAllConditionLogic(failed, true, null, batchSelectedOnly, listTi);
                }
                else
                {
                    DownloadRetryAllConditionLogic(failed, false, CurrentlySelectedDLFolder, batchSelectedOnly, listTi);
                }
            }
        }


        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        public TransferAdapterRecyclerVersion recyclerTransferAdapter;

        public override void OnDestroy()
        {
            try
            {
                TransfersActionMode?.Finish();
            }
            catch (System.Exception)
            {

            }
            base.OnDestroy();
        }

        public static void RestoreUploadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(SoulSeekState.M_Upload_TransferList_v2, string.Empty); //TODO !!! replace
            if (transferListv2 == string.Empty)
            {
                RestoreUploadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItemManagerUploads = new TransferItemManager(true);
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerUploads.GetType());
                    TransferItemManagerUploads = serializer.Deserialize(stream) as TransferItemManager;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    TransferItemManagerUploads.OnRelaunch();
                }
            }
        }

        public static void RestoreUploadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(SoulSeekState.M_TransferListUpload, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItemManagerUploads = new TransferItemManager(true);
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    OnRelaunch(transferItemsLegacy);
                }

                TransferItemManagerUploads = new TransferItemManager(true);
                //populate the new data structure.
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItemManagerUploads.Add(ti);
                }

            }
        }



        public static void RestoreDownloadTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferListv2 = string.Empty;//sharedPreferences.GetString(SoulSeekState.M_TransferList_v2, string.Empty);
            if (transferListv2 == string.Empty)
            {
                RestoreDownloadTransferItemsLegacy(sharedPreferences);
            }
            else
            {
                //restore the simple way via deserializing...
                TransferItemManagerDL = new TransferItemManager();
                using (var stream = new System.IO.StringReader(transferListv2))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerDL.GetType());
                    TransferItemManagerDL = serializer.Deserialize(stream) as TransferItemManager;
                    //BackwardsCompatFunction(transferItemsLegacy);
                    TransferItemManagerDL.OnRelaunch();
                }
            }


        }

        public static void RestoreDownloadTransferItemsLegacy(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(SoulSeekState.M_TransferList, string.Empty);
            if (transferList == string.Empty)
            {
                TransferItemManagerDL = new TransferItemManager();
            }
            else
            {
                var transferItemsLegacy = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItemsLegacy.GetType());
                    transferItemsLegacy = serializer.Deserialize(stream) as List<TransferItem>;
                    BackwardsCompatFunction(transferItemsLegacy);
                    OnRelaunch(transferItemsLegacy);
                }

                TransferItemManagerDL = new TransferItemManager();
                //populate the new data structure.
                foreach (var ti in transferItemsLegacy)
                {
                    TransferItemManagerDL.Add(ti);
                }

            }
        }

        public static void OnRelaunch(List<TransferItem> transfers)
        {   //transfers that were previously InProgress before we shut down should now be considered paused (cancelled). or aborted (cancelled upload)
            foreach (var ti in transfers)
            {
                if (ti.State.HasFlag(TransferStates.InProgress))
                {
                    ti.State = TransferStates.Cancelled;
                    ti.RemainingTime = null;
                }

                if (ti.State.HasFlag(TransferStates.UserOffline))
                {
                    TransfersFragment.UsersWhereDownloadFailedDueToOffline[ti.Username] = 0x0;
                }
            }
        }

        public static void BackwardsCompatFunction(List<TransferItem> transfers)
        {
            //this should eventually be removed.  its just for the old transfers which have state == None
            foreach (var ti in transfers)
            {
                if (ti.State == TransferStates.None)
                {
                    if (ti.Failed)
                    {
                        ti.State = TransferStates.Errored;
                    }
                    else if (ti.Progress == 100)
                    {
                        ti.State = TransferStates.Succeeded;
                    }
                }
            }
        }

        private void SetNoTransfersMessage()
        {
            if (TransfersFragment.InUploadsMode)
            {
                if (!(TransferItemManagerUploads.IsEmpty()))
                {
                    noTransfers.Visibility = ViewStates.Gone;
                }
                else
                {
                    noTransfers.Visibility = ViewStates.Visible;
                    if (MainActivity.MeetsSharingConditions())
                    {
                        noTransfers.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet);
                    }
                    else
                    {
                        noTransfers.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_uploads_yet_not_sharing);
                    }
                }
            }
            else
            {
                if (!(TransferItemManagerDL.IsEmpty()))
                {
                    noTransfers.Visibility = ViewStates.Gone;
                }
                else
                {
                    noTransfers.Visibility = ViewStates.Visible;
                    noTransfers.Text = SoulSeekState.ActiveActivityRef.GetString(Resource.String.no_transfers_yet);
                }
            }
        }


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            StaticHacks.TransfersFrag = this;
            HasOptionsMenu = true;
            MainActivity.LogDebug("TransfersFragment OnCreateView");
            if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
            {
                AndroidX.AppCompat.App.AppCompatDelegate.CompatVectorFromResourcesEnabled = true;
                this.rootView = inflater.Inflate(Resource.Layout.transfers, container, false);
            }
            else
            {
                this.rootView = inflater.Inflate(Resource.Layout.transfers, container, false);
            }
            //this.primaryListView = rootView.FindViewById<ListView>(Resource.Id.listView1);
            recyclerViewTransferItems = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerView1);
            this.noTransfers = rootView.FindViewById<TextView>(Resource.Id.noTransfersView);

            //View transferOptions = rootView.FindViewById<View>(Resource.Id.transferOptions);
            //transferOptions.Click += TransferOptions_Click;
            this.RegisterForContextMenu(recyclerViewTransferItems); //doesnt work for recycle views
            sharedPreferences = SoulSeekState.SharedPreferences;
            //if (TransferItemManagerDL == null)//bc our sharedPref string can be older than the transferItems
            //{
            //    RestoreDownloadTransferItems(sharedPreferences);
            //    RestoreUploadTransferItems(sharedPreferences);
            //    TransferItemManagerWrapped = new TransferItemManagerWrapper(TransfersFragment.TransferItemManagerUploads, TransfersFragment.TransferItemManagerDL);
            //}

            SetNoTransfersMessage();


            //TransferAdapter customAdapter = new TransferAdapter(Context, transferItems);
            //primaryListView.Adapter = (customAdapter);

            recycleLayoutManager = new CustomLinearLayoutManager(Activity);
            SetRecyclerAdapter();
            //if (savedInstanceState != null)
            //{
            //    // Restore saved layout manager type.
            //    mCurrentLayoutManagerType = (LayoutManagerType)savedInstanceState
            //            .getSerializable(KEY_LAYOUT_MANAGER);
            //}

            recyclerViewTransferItems.SetLayoutManager(recycleLayoutManager);

            //// If a layout manager has already been set, get current scroll position.
            //if (mRecyclerView.getLayoutManager() != null)
            //{
            //    scrollPosition = ((LinearLayoutManager)mRecyclerView.getLayoutManager())
            //            .findFirstCompletelyVisibleItemPosition();
            //}
            //recyclerViewTransferItems.ScrollToPosition()

            //// If a layout manager has already been set, get current scroll position.
            //if (mRecyclerView.getLayoutManager() != null)
            //{
            //    scrollPosition = ((LinearLayoutManager)mRecyclerView.getLayoutManager())
            //            .findFirstCompletelyVisibleItemPosition();
            //}



            MainActivity.LogInfoFirebase("AutoClear: " + SoulSeekState.AutoClearCompleteDownloads);
            MainActivity.LogInfoFirebase("AutoRetry: " + SoulSeekState.AutoRetryDownload);

            return rootView;
        }

        public void SaveScrollPositionOnMovingIntoFolder()
        {
            ScrollPositionBeforeMovingIntoFolder = ((LinearLayoutManager)recycleLayoutManager).FindFirstVisibleItemPosition();
            View v = recyclerViewTransferItems.GetChildAt(0);
            if (v == null)
            {
                ScrollOffsetBeforeMovingIntoFolder = 0;
            }
            else
            {
                ScrollOffsetBeforeMovingIntoFolder = v.Top - recyclerViewTransferItems.Top;
            }
        }

        public void RestoreScrollPosition()
        {
            ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(ScrollPositionBeforeMovingIntoFolder, ScrollOffsetBeforeMovingIntoFolder); //if you dont do with offset, it scrolls it until the visible item is simply in view (so it will be at bottom, almost a whole screen off)
        }

        public static ActionModeCallback TransfersActionModeCallback = null;
        //public static Android.Support.V7.View.ActionMode TransfersActionMode = null;
        public static ActionMode TransfersActionMode = null;
        public static List<int> BatchSelectedItems = new List<int>();

        public void SetRecyclerAdapter(bool restoreState = false)
        {
            lock (TransferItemManagerWrapped.GetUICurrentList())
            {
                int prevScrollPos =0;
                int scrollOffset = 0;
                if (restoreState)
                {
                    prevScrollPos = ((LinearLayoutManager)recycleLayoutManager).FindFirstVisibleItemPosition();
                    View v = recyclerViewTransferItems.GetChildAt(0);
                    if (v == null)
                    {
                        scrollOffset = 0;
                    }
                    else
                    {
                        scrollOffset = v.Top - recyclerViewTransferItems.Top;
                    }
                }


                if (GroupByFolder && !CurrentlyInFolder())
                {
                    recyclerTransferAdapter = new TransferAdapterRecyclerFolderItem(TransferItemManagerWrapped.GetUICurrentList() as List<FolderItem>);
                }
                else
                {
                    recyclerTransferAdapter = new TransferAdapterRecyclerIndividualItem(TransferItemManagerWrapped.GetUICurrentList() as List<TransferItem>);
                }
                recyclerTransferAdapter.TransfersFragment = this;
                recyclerTransferAdapter.IsInBatchSelectMode = (TransfersActionMode != null);
                recyclerViewTransferItems.SetAdapter(recyclerTransferAdapter);

                if(restoreState)
                {
                    ((LinearLayoutManager)recycleLayoutManager).ScrollToPositionWithOffset(prevScrollPos, scrollOffset);
                }
            }
        }

        /// <summary>
        /// This is a UI event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TranferQueueStateChanged(object sender, TransferItem e)
        {
            SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() =>
            {
                UpdateQueueState(e.FullFilename);
            }));
        }

        private void TransferOptions_Click(object sender, EventArgs e)
        {
            try
            {
                PopupMenu popup = new PopupMenu(SoulSeekState.MainActivityRef, sender as View);
                popup.SetOnMenuItemClickListener(this);//  setOnMenuItemClickListener(MainActivity.this);
                popup.Inflate(Resource.Menu.transfers_options);
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

        public bool OnMenuItemClick(IMenuItem item)
        {
            return false;
            //switch (item.ItemId)
            //{
            //    case Resource.Id.clearAllComplete:
            //        //TransferItem[] tempArry = new TransferItem[transferItems.Count]();
            //        //transferItems.CopyTo(tempArry);
            //        lock (transferItems)
            //        {
            //            //Occurs on UI thread.
            //            transferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
            //            //for (int i=0; i< tempArry.Count;i++)
            //            //{
            //            //    if(tempArry[i].Progress>99)
            //            //    {
            //            //        transferItems.Remove(tempArry[i]);
            //            //    }
            //            //}
            //            refreshListView();
            //        }
            //        return true;
            //    case Resource.Id.cancelAndClearAll:
            //        lock (transferItems)
            //        {
            //            SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            //            for (int i = 0; i < transferItems.Count; i++)
            //            {
            //                //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
            //                CancellationTokens.TryGetValue(ProduceCancellationTokenKey(transferItems[i]), out CancellationTokenSource token);
            //                token?.Cancel();
            //                //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
            //            }
            //            CancellationTokens.Clear();
            //            transferItems.Clear();
            //            refreshListView();
            //        }
            //        return true;
            //    //case Resource.Id.openDownloadsDir:
            //    //    Intent chooser = new Intent(Intent.ActionView); //not get content
            //    //    //actionview - no apps can perform this action.
            //    //    //chooser.AddCategory(Intent.CategoryOpenable);

            //    //    if (SoulSeekState.UseLegacyStorage())
            //    //    {
            //    //        if (SoulSeekState.SaveDataDirectoryUri == null || SoulSeekState.SaveDataDirectoryUri == string.Empty)
            //    //        {
            //    //            string rootDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
            //    //            chooser.SetDataAndType(Android.Net.Uri.Parse(rootDir), "*/*");
            //    //        }
            //    //        else
            //    //        {
            //    //            chooser.SetDataAndType(Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri), "*/*");
            //    //        }
            //    //    }
            //    //    else
            //    //    {
            //    //        if (SoulSeekState.SaveDataDirectoryUri==null || SoulSeekState.SaveDataDirectoryUri==string.Empty)
            //    //        {
            //    //            Toast tst = Toast.MakeText(Context, "Download Directory is not set.  Please set it to enable downloading.", ToastLength.Short);
            //    //            tst.Show();
            //    //            return true;
            //    //        }

            //    //        chooser.SetData(Android.Net.Uri.Parse(SoulSeekState.SaveDataDirectoryUri));
            //    //    }

            //    //    try
            //    //    {
            //    //        StartActivity(Intent.CreateChooser(chooser,"Download Directory"));
            //    //    }
            //    //    catch
            //    //    {
            //    //        Toast tst = Toast.MakeText(Context, "No compatible File Browser app installed", ToastLength.Short);
            //    //        tst.Show();
            //    //    }
            //    //    return true;
            //    default:
            //        return false;

            //}
        }


        //public override void OnDestroyView()
        //{
        //    try
        //    {
        //        SoulSeekState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
        //        SoulSeekState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
        //        MainActivity.TransferItemQueueUpdated -= TranferQueueStateChanged;
        //    }
        //    catch (System.Exception)
        //    {

        //    }
        //    base.OnDestroyView();
        //}

        //public override void OnDestroy()
        //{
        //    //SoulSeekState.DownloadAdded -= SoulSeekState_DownloadAdded;
        //    //SoulSeekState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
        //    //SoulSeekState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
        //    base.OnDestroy();
        //}

        public override void OnResume()
        {
            StaticHacks.TransfersFrag = this;
            if (MainActivity.fromNotificationMoveToUploads)
            {
                MainActivity.fromNotificationMoveToUploads = false;
                this.MoveToUploadForNotif();
            }
            base.OnResume();
        }

        public void MoveToUploadForNotif()
        {
            InUploadsMode = true;
            CurrentlySelectedDLFolder = null;
            CurrentlySelectedUploadFolder = null;
            this.RefreshForModeSwitch();
            SoulSeekState.MainActivityRef.InvalidateOptionsMenu();
        }

        public override void OnPause()
        {
            base.OnPause();
            MainActivity.LogDebug("TransferFragment OnPause");  //this occurs when we move to the Account Tab or if we press the home button (i.e. to later kill the process)
                                                                //so this is a good place to do it.
            SaveTransferItems(sharedPreferences);
        }

        public static object TransferStateSaveLock = new object();

        public static void SaveTransferItems(ISharedPreferences sharedPreferences, bool force=true, int maxSecondsUpdate=0)
        {
            MainActivity.LogDebug("---- saving transfer items enter ----");
#if DEBUG
            var sw = System.Diagnostics.Stopwatch.StartNew();
            sw.Start();
#endif

            if(force || (SeekerApplication.TransfersDownloadsCompleteStale && DateTime.UtcNow.Subtract(SeekerApplication.TransfersLastSavedTime).TotalSeconds > maxSecondsUpdate)) //stale and we havent updated too recently..
            {
                MainActivity.LogDebug("---- saving transfer items actual save ----");
                if (TransferItemManagerDL?.AllTransferItems == null)
                {
                    return;
                }
                string listOfDownloadItems = string.Empty;
                string listOfUploadItems = string.Empty;
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerDL.AllTransferItems.GetType());
                    serializer.Serialize(writer, TransferItemManagerDL.AllTransferItems);
                    listOfDownloadItems = writer.ToString();
                }
                using (var writer = new System.IO.StringWriter())
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(TransferItemManagerUploads.AllTransferItems.GetType());
                    serializer.Serialize(writer, TransferItemManagerUploads.AllTransferItems);
                    listOfUploadItems = writer.ToString();
                }
                lock (MainActivity.SHARED_PREF_LOCK)
                    lock (TransferStateSaveLock)
                    {
                        var editor = sharedPreferences.Edit();
                        editor.PutString(SoulSeekState.M_TransferList, listOfDownloadItems);
                        editor.PutString(SoulSeekState.M_TransferListUpload, listOfUploadItems);
                        editor.Commit();
                    }

                SeekerApplication.TransfersDownloadsCompleteStale = false;
                SeekerApplication.TransfersLastSavedTime = DateTime.UtcNow;
            }

#if DEBUG
            sw.Stop();
            MainActivity.LogDebug("saving time: " + sw.ElapsedMilliseconds);
#endif


        }

        private List<TransferItem> GetBatchSelectedItemsForRetryCondition(bool selectFailed)
        {
            bool folderItems = false;
            if (GroupByFolder && !CurrentlyInFolder())
            {
                folderItems = true;
            }
            List<TransferItem> tis = new List<TransferItem>();
            foreach (int pos in BatchSelectedItems)
            {
                if (folderItems)
                {
                    var fi = TransferItemManagerDL.GetItemAtUserIndex(pos) as FolderItem;
                    foreach (TransferItem ti in fi.TransferItems)
                    {
                        if (selectFailed && ti.Failed)
                        {
                            tis.Add(ti);
                        }
                        else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                        {
                            tis.Add(ti);
                        }
                    }
                }
                else
                {
                    var ti = TransferItemManagerDL.GetItemAtUserIndex(pos) as TransferItem;
                    if (selectFailed && ti.Failed)
                    {
                        tis.Add(ti);
                    }
                    else if (!selectFailed && (ti.State.HasFlag(TransferStates.Cancelled) || ti.State.HasFlag(TransferStates.Queued)))
                    {
                        tis.Add(ti);
                    }
                }
            }
            return tis;
        }


        public static void DownloadRetryAllConditionLogic(bool selectFailed, bool all, FolderItem specifiedFolderOnly, bool batchSelectedOnly, List<TransferItem> batchSelectedTis=null) //if true DownloadRetryAllFailed if false Resume All Paused. if not all then specified folder
        {
            IEnumerable<TransferItem> transferItemConditionList = new List<TransferItem>();
            if(batchSelectedOnly)
            {
                if(batchSelectedTis==null)
                {
                    throw new System.Exception("No batch selected transfer items provided");
                }
                transferItemConditionList = batchSelectedTis;
            }
            else if (all)
            {
                if (selectFailed)
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfFailed().Select(tup => tup.Item1);
                }
                else
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfPaused().Select(tup => tup.Item1);
                }
            }
            else
            {
                if (selectFailed)
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfFailedFromFolder(specifiedFolderOnly).Select(tup => tup.Item1);
                }
                else
                {
                    transferItemConditionList = TransferItemManagerDL.GetListOfPausedFromFolder(specifiedFolderOnly).Select(tup => tup.Item1);
                }
            }
            bool exceptionShown = false;
            foreach (TransferItem item in transferItemConditionList)
            {
                //TransferItem item1 = transferItems[info.Position];  
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    Android.Net.Uri incompleteUri = null;
                    SetupCancellationToken(item, cancellationTokenSource, out _);
                    Task task = DownloadDialog.DownloadFileAsync(item.Username, item.FullFilename, item.GetSizeForDL(), cancellationTokenSource);
                    task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item.Username, item.FullFilename, item.Size, task, cancellationTokenSource, item.QueueLength, 0, item.GetDirectoryLevel()) { TransferItemReference = item })));
                }
                catch (DuplicateTransferException)
                {
                    //happens due to button mashing...
                    return;
                }
                catch (System.Exception error)
                {
                    Action a = new Action(() => { Toast.MakeText(SoulSeekState.ActiveActivityRef, SoulSeekState.ActiveActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long).Show(); });
                    if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                    {

                    }
                    else
                    {
                        MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
                    }
                    if (!exceptionShown)
                    {
                        SoulSeekState.ActiveActivityRef.RunOnUiThread(a);
                        exceptionShown = true;
                    }
                    return; //otherwise null ref with task!
                }
                //save to disk, update ui.
                //task.ContinueWith(SoulSeekState.MainActivityRef.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username,item1.FullFilename,item1.Size,task, cancellationTokenSource))));
                item.Progress = 0; //no longer red... some good user feedback
                item.Failed = false;

            }

            var refreshOnlySelected = new Action(() =>
            {
                HashSet<int> indicesToUpdate = new HashSet<int>();
                foreach(TransferItem ti in transferItemConditionList)
                {
                    int pos = TransferItemManagerDL.GetUserIndexForTransferItem(ti);
                    if(pos==-1)
                    {
                        MainActivity.LogDebug("pos == -1!!");
                        continue;
                    }
                    
                    if(indicesToUpdate.Contains(pos))
                    {
                        //this is for if we are in a folder.  since previously we would update a folder of 10 items, 10 times which looked quite glitchy...
                        MainActivity.LogDebug($"skipping same pos {pos}");
                    }
                    else
                    {
                        indicesToUpdate.Add(pos);
                    }
                }
                if (InUploadsMode)
                {
                    return;
                }
                foreach(int i in indicesToUpdate)
                {
                    MainActivity.LogDebug($"updating {i}");
                    if(StaticHacks.TransfersFrag != null)
                    {
                        StaticHacks.TransfersFrag.recyclerTransferAdapter?.NotifyItemChanged(i);
                    }
                }


            });
            lock (TransferItemManagerDL.GetUICurrentList()) //TODO: test
            { //also can update this to do a partial refresh...
                if (StaticHacks.TransfersFrag != null)
                {
                    StaticHacks.TransfersFrag.refreshListView(refreshOnlySelected);
                }
            }
        }


        private void DownloadRetryLogic(ITransferItem transferItem)
        {
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
            //its possible that in between creating this contextmenu and getting here that the transfer items changed especially if maybe a re-login was done...
            //either position changed, or someone just straight up cleared them in the meantime...
            //maybe we can add the filename in the menuinfo to match it with, rather than the index..

            //NEVER USE GetChildAt!!!  IT WILL RETURN NULL IF YOU HAVE MORE THAN ONE PAGE OF DATA
            //FindViewByPosition works PERFECTLY.  IT RETURNS THE VIEW CORRESPONDING TO THE TRANSFER LIST..

            //ITransferItemView targetView = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(position) as ITransferItemView;

            //TransferItem item1 = null;
            //MainActivity.LogDebug("targetView is null? " + (targetView == null).ToString());
            //if (targetView == null)
            //{
            //    SeekerApplication.ShowToast(SoulSeekState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
            //    return;
            //}
            string chosenFname = (transferItem as TransferItem).FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            string chosenUname = (transferItem as TransferItem).Username; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            MainActivity.LogDebug("chosenFname? " + chosenFname);
            TransferItem item1 = TransferItemManagerDL.GetTransferItemWithIndexFromAll(chosenFname, chosenUname, out int _);
            int indexToRefresh = TransferItemManagerDL.GetUserIndexForTransferItem(item1);
            MainActivity.LogDebug("item1 is null?" + (item1 == null).ToString());//tested
            if (item1 == null || indexToRefresh==-1)
            {
                SeekerApplication.ShowToast(SoulSeekState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
                return;
            }

            //int tokenNum = int.MinValue;
            if (SoulSeekState.SoulseekClient.IsTransferInDownloads(item1.Username, item1.FullFilename/*, out tokenNum*/))
            {
                MainActivity.LogDebug("transfer is in Downloads !!! " + item1.FullFilename);
                item1.CancelAndRetryFlag = true;
                ClearTransferForRetry(item1, indexToRefresh);
                if (item1.CancellationTokenSource != null)
                {
                    if (!item1.CancellationTokenSource.IsCancellationRequested)
                    {
                        item1.CancellationTokenSource.Cancel();
                    }
                }
                else
                {
                    MainActivity.LogFirebase("CTS is null. this should not happen. we should always set it before downloading.");
                }
                return; //the dl continuation method will take care of it....
            }

            //TransferItem item1 = transferItems[info.Position];  
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {

                Android.Net.Uri incompleteUri = null;
                SetupCancellationToken(item1, cancellationTokenSource, out _);
                Task task = DownloadDialog.DownloadFileAsync(item1.Username, item1.FullFilename, item1.GetSizeForDL(), cancellationTokenSource);
                //SoulSeekState.SoulseekClient.DownloadAsync(
                //username: item1.Username,
                //filename: item1.FullFilename,
                //size: item1.Size,
                //cancellationToken: cancellationTokenSource.Token);
                task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, task, cancellationTokenSource, item1.QueueLength, item1.Failed ? 1 : 0, item1.GetDirectoryLevel()) { TransferItemReference = item1 }))); //if paused do retry counter 0.
            }
            catch (DuplicateTransferException)
            {
                //happens due to button mashing...
                return;
            }
            catch (System.Exception error)
            {
                Action a = new Action(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                if (error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                {

                }
                else
                {
                    MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
                }
                SoulSeekState.MainActivityRef.RunOnUiThread(a);
                return; //otherwise null ref with task!
            }
            ClearTransferForRetry(item1, indexToRefresh);
        }

        private void ClearTransferForRetry(TransferItem item1, int position)
        {
            item1.Progress = 0; //no longer red... some good user feedback
            item1.QueueLength = int.MaxValue; //let the State Changed update this for us...
            item1.Failed = false;
            var refreshOnlySelected = new Action(() =>
            {

                MainActivity.LogDebug("notifyItemChanged " + position);

                recyclerTransferAdapter.NotifyItemChanged(position);


            });
            lock (TransferItemManagerDL.GetUICurrentList())
            { //also can update this to do a partial refresh...
                refreshListView(refreshOnlySelected);
            }
        }

        private bool NotLoggedInShowMessageGaurd(string msg)
        {
            if(!SoulSeekState.currentlyLoggedIn)
            {
                Toast.MakeText(SoulSeekState.ActiveActivityRef, "Must be logged in to " + msg, ToastLength.Short).Show();
                return true;
            }
            return false;
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            if(item.GroupId == UNIQUE_TRANSFER_GROUP_ID)
            {
                if(recyclerTransferAdapter==null)
                {
                    MainActivity.LogInfoFirebase("recyclerTransferAdapter is null");
                }

                ITransferItem ti = recyclerTransferAdapter.getSelectedItem();

                if (ti == null)
                {
                    MainActivity.LogInfoFirebase("ti is null");
                }

                int position = TransferItemManagerWrapped.GetUserIndexForITransferItem(ti);
                //MainActivity.LogDebug($"position: {position} ti name: {ti.GetDisplayName()}");
                //try
                //{
                //    ti = TransferItemManagerWrapped.GetItemAtUserIndex(position); //UI
                //}
                //catch (ArgumentOutOfRangeException)
                //{
                //    //MainActivity.LogFirebase("case1: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                //    Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                //    return base.OnContextItemSelected(item);
                //}
                
                if(position == -1)
                {
                    Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                    return base.OnContextItemSelected(item);
                }

                if (Helpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), ti.GetUsername(), SoulSeekState.ActiveActivityRef, this.View))
                {
                    MainActivity.LogDebug("handled by commons");
                    return base.OnContextItemSelected(item);
                }

                switch (item.ItemId)
                {
                    case 0: //single transfer only
                        //retry download (resume download)
                        if(NotLoggedInShowMessageGaurd("start transfer"))
                        {
                            return true;
                        }

                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryLogic(ti); });
                            }));
                        }
                        else
                        {
                            DownloadRetryLogic(ti);
                        }
                        //Toast.MakeText(Applicatio,"Retrying...",ToastLength.Short).Show();
                        break;
                    case 1:
                        //clear complete?
                        //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                        MainActivity.LogInfoFirebase("Clear Complete item pressed");
                        lock (TransferItemManagerWrapped.GetUICurrentList()) //TODO: test
                        {
                            try
                            {
                                if (InUploadsMode)
                                {
                                    TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //UI
                                }
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                //MainActivity.LogFirebase("case1: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                                Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                                return base.OnContextItemSelected(item);
                            }
                            recyclerTransferAdapter.NotifyItemRemoved(position);  //UI
                            //refreshListView();
                        }
                        break;
                    case 2: //cancel and clear (downloads) OR abort and clear (uploads)
                        //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                        MainActivity.LogInfoFirebase("Cancel and Clear item pressed");
                        ITransferItem tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        if (tItem is TransferItem tti)
                        {
                            bool wasInProgress = tti.State.HasFlag(TransferStates.InProgress);
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(tti), out CancellationTokenSource uptoken);
                            uptoken?.Cancel();
                            //CancellationTokens[ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                            CancellationTokens.Remove(ProduceCancellationTokenKey(tti), out _);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                if (InUploadsMode)
                                {
                                    TransferItemManagerWrapped.RemoveAtUserIndex(position);
                                }
                                else
                                {
                                    TransferItemManagerWrapped.RemoveAndCleanUpAtUserIndex(position); //this means basically, wait for the stream to be closed. no race conditions..
                                }
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else if (tItem is FolderItem fi)
                        {
                            TransferItemManagerWrapped.CancelFolder(fi, true);
                            TransferItemManagerWrapped.ClearAllFromFolderAndClean(fi);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                //TransferItemManagerDL.RemoveAtUserIndex(position); we already removed
                                recyclerTransferAdapter.NotifyItemRemoved(position);
                            }
                        }
                        else
                        {
                            MainActivity.LogInfoFirebase("Cancel and Clear item pressed - bad item");
                        }
                        break;
                    case 3:
                        if (NotLoggedInShowMessageGaurd("get queue position"))
                        {
                            return true;
                        }
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case3: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }

                        //the folder implementation will re-request all queued files
                        //recently I thought of just doing the lowest, but then if the lowest is ready, it will download leaving the other transfers behind.


                        if (tItem is TransferItem)
                        {
                            GetQueuePosition(tItem as TransferItem);
                        }
                        else if (tItem is FolderItem folderItem)
                        {
                            lock (folderItem.TransferItems)
                            {
                                foreach (TransferItem transferItem in folderItem.TransferItems.Where(ti => ti.State == TransferStates.Queued))
                                {
                                    GetQueuePosition(transferItem);
                                }
                            }
                        }
                        break;
                    case 4:
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerDL.GetItemAtUserIndex(position) as TransferItem;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case4: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        try
                        {
                            //tested on API25 and API30
                            //AndroidX.Core.Content.FileProvider
                            Android.Net.Uri uriToUse = null;
                            if (SoulSeekState.UseLegacyStorage() && Helpers.IsFileUri((tItem as TransferItem).FinalUri)) //i.e. if it is a FILE URI.
                            {
                                uriToUse = AndroidX.Core.Content.FileProvider.GetUriForFile(this.Context, this.Context.ApplicationContext.PackageName + ".provider", new Java.IO.File(Android.Net.Uri.Parse((tItem as TransferItem).FinalUri).Path));
                            }
                            else
                            {
                                uriToUse = Android.Net.Uri.Parse((tItem as TransferItem).FinalUri);
                            }
                            Intent playFileIntent = new Intent(Intent.ActionView);
                            //playFileIntent.SetDataAndType(uriToUse,"audio/*");  
                            playFileIntent.SetDataAndType(uriToUse, Helpers.GetMimeTypeFromFilename((tItem as TransferItem).FullFilename));   //works
                            playFileIntent.AddFlags(ActivityFlags.GrantReadUriPermission | /*ActivityFlags.NewTask |*/ ActivityFlags.GrantWriteUriPermission); //works.  newtask makes it go to foobar and immediately jump back
                            //Intent chooser = Intent.CreateChooser(playFileIntent, "Play song with");
                            this.StartActivity(playFileIntent); //also the chooser isnt needed.  if you show without the chooser, it will show you the options and you can check Only Once, Always.
                        }
                        catch (System.Exception e)
                        {
                            MainActivity.LogFirebase(e.Message + e.StackTrace);
                            Toast.MakeText(this.Context, Resource.String.failed_to_play, ToastLength.Short).Show(); //normally bc no player is installed.
                        }
                        break;
                    case 7: //browse at location (browse at folder)
                        if (NotLoggedInShowMessageGaurd("browse folder"))
                        {
                            return true;
                        }
                        if (ti is TransferItem ttti)
                        {
                            string startingDir = Helpers.GetDirectoryRequestFolderName(ttti.FullFilename);
                            Action<View> action = new Action<View>((v) => {
                                ((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                            });

                            DownloadDialog.RequestFilesApi(ttti.Username, this.View, action, startingDir);
                        }
                        else if(ti is FolderItem fi)
                        {
                            if(fi.IsEmpty())
                            {
                                //since if auto clear is on, and the menu is already up, the final item in this folder can clear before we end up selecting something.
                                Toast.MakeText(SoulSeekState.ActiveActivityRef,"Folder is empty.",ToastLength.Short).Show();
                                return true;
                            }
                            string startingDir = Helpers.GetDirectoryRequestFolderName(fi.TransferItems[0].FullFilename);
                            for (int i=0; i < fi.GetDirectoryLevel() - 1; i++)
                            {
                                startingDir = Helpers.GetDirectoryRequestFolderName(startingDir); //keep going up..
                            }

                            
                            Action<View> action = new Action<View>((v) => {
                                ((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                            });

                            DownloadDialog.RequestFilesApi(fi.Username, this.View, action, startingDir);
                        }
                        break;
                    case 100: //resume folder
                        if (NotLoggedInShowMessageGaurd("resume folder"))
                        {
                            return true;
                        }
                        MainActivity.LogInfoFirebase("resume folder Pressed");
                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false, false, ti as FolderItem, false); });
                            }));
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(false, false, ti as FolderItem, false);
                        }
                        break;
                    case 101: //pause folder or abort uploads (uploads)
                        TransferItemManagerWrapped.CancelFolder(ti as FolderItem);
                        int index = TransferItemManagerWrapped.GetIndexForFolderItem(ti as FolderItem);
                        recyclerTransferAdapter.NotifyItemChanged(index);
                        break;
                    case 102: //retry failed downloads from folder
                        if (NotLoggedInShowMessageGaurd("retry folder"))
                        {
                            return true;
                        }
                        MainActivity.LogInfoFirebase("retry folder Pressed");
                        if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                        {
                            Task t;
                            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, false, out t))
                            {
                                return base.OnContextItemSelected(item);
                            }
                            t.ContinueWith(new Action<Task>((Task t) =>
                            {
                                if (t.IsFaulted)
                                {
                                    SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                                    {
                                        if (Context != null)
                                        {
                                            Toast.MakeText(Context, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }
                                        else
                                        {
                                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();
                                        }

                                    });
                                    return;
                                }
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(true, false, ti as FolderItem, false); });
                            }));
                        }
                        else
                        {
                            DownloadRetryAllConditionLogic(true, false, ti as FolderItem, false);
                        }
                        break;
                    case 103: //abort upload
                        MainActivity.LogInfoFirebase("Abort Upload item pressed");
                        tItem = null;
                        try
                        {
                            tItem = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //MainActivity.LogFirebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        TransferItem uploadToCancel = tItem as TransferItem;

                        CancellationTokens.TryGetValue(ProduceCancellationTokenKey(uploadToCancel), out CancellationTokenSource token);
                        token?.Cancel();
                        //CancellationTokens[ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                        CancellationTokens.Remove(ProduceCancellationTokenKey(uploadToCancel), out _);
                        lock (TransferItemManagerWrapped.GetUICurrentList())
                        {
                            recyclerTransferAdapter.NotifyItemChanged(position);
                        }
                        break;
                    case 104: //ignore (unshare) user
                        MainActivity.LogInfoFirebase("Unshare User item pressed");
                        IEnumerable<TransferItem> tItems = TransferItemManagerWrapped.GetTransferItemsForUser(ti.GetUsername());
                        foreach (var tiToCancel in tItems)
                        {
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(tiToCancel), out CancellationTokenSource token1);
                            token1?.Cancel();
                            CancellationTokens.Remove(ProduceCancellationTokenKey(tiToCancel), out _);
                            lock (TransferItemManagerWrapped.GetUICurrentList())
                            {
                                int posOfCancelled = TransferItemManagerWrapped.GetUserIndexForTransferItem(tiToCancel);
                                if (posOfCancelled != -1)
                                {
                                    recyclerTransferAdapter.NotifyItemChanged(posOfCancelled);
                                }
                            }
                        }
                        SeekerApplication.AddToIgnoreListFeedback(SoulSeekState.ActiveActivityRef, ti.GetUsername());
                        break;
                    case 105: //batch selection mode
                        TransfersActionModeCallback = new ActionModeCallback() { Adapter = recyclerTransferAdapter, Frag = this };
                        ForceOutIfZeroSelected = true;
                        //Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)SoulSeekState.MainActivityRef.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
                        //TransfersActionMode = myToolbar.StartActionMode(TransfersActionModeCallback);
                        TransfersActionMode = SoulSeekState.MainActivityRef.StartActionMode(TransfersActionModeCallback);
                        recyclerTransferAdapter.IsInBatchSelectMode = true;
                        ToggleItemBatchSelect(recyclerTransferAdapter, position);
                        break;
                }
            }
            return base.OnContextItemSelected(item);
        }

        public static bool ForceOutIfZeroSelected = true;
        public static void ToggleItemBatchSelect(TransferAdapterRecyclerVersion recyclerTransferAdapter, int pos)
        {
            if (BatchSelectedItems.Contains(pos))
            {
                BatchSelectedItems.Remove(pos);
            }
            else
            {
                BatchSelectedItems.Add(pos);
            }
            recyclerTransferAdapter.NotifyItemChanged(pos);
            int cnt = BatchSelectedItems.Count;
            if (cnt == 0 && ForceOutIfZeroSelected)
            {
                TransfersActionMode.Finish();
            }
            else
            {
                TransfersActionMode.Title = string.Format("{0} Selected", cnt.ToString());
                TransfersActionMode.Invalidate();
            }
        }

        public static void UpdateBatchSelectedItemsIfApplicable(TransferItem ti)
        {
            if(TransfersActionMode==null)
            {
                return;
            }
            int userPostionBeingRemoved = TransferItemManagerWrapped.GetUserIndexForTransferItem(ti);
            if(userPostionBeingRemoved == -1)
            {
                //it is not currently on our screen, perhaps it is in uploads (and we are in downloads) or we are inside a folder (and it is outside)
                MainActivity.LogDebug("batch on, different screen item removed");
                return;
            }
            MainActivity.LogDebug("batch on, updating: " + userPostionBeingRemoved);
            //adjust numbers
            int cnt = BatchSelectedItems.Count;
            for (int i = cnt - 1; i >= 0; i--)
            {
                int position = BatchSelectedItems[i];
                if (position < userPostionBeingRemoved)
                {
                    continue;
                }
                else if (position == userPostionBeingRemoved)
                {
                    BatchSelectedItems.RemoveAt(i);
                }
                else
                {
                    BatchSelectedItems[i] = position - 1;
                }
            }
            //if there was only 1 and its the one that just finished then take us out of batchSelectedItems
            if(BatchSelectedItems.Count == 0)
            {
                TransfersActionMode.Finish();
            }
            else if(BatchSelectedItems.Count != cnt) //if we have 1 less now.
            {
                TransfersActionMode.Title = string.Format("{0} Selected", BatchSelectedItems.Count.ToString());
                TransfersActionMode.Invalidate();
            }
        }

        public class ActionModeCallback : Java.Lang.Object, ActionMode.ICallback
        {
            public TransferAdapterRecyclerVersion Adapter;
            public TransfersFragment Frag;
            public bool OnCreateActionMode(ActionMode mode, IMenu menu)
            {
                mode.MenuInflater.Inflate(Resource.Menu.transfers_menu_batch, menu);
                return true;
            }

            public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
            {
                if (BatchSelectedItems.Count == 0)
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(false);
                }
                else
                {
                    menu.FindItem(Resource.Id.action_cancel_and_clear_all_batch).SetVisible(true);
                }


                if (TransfersFragment.InUploadsMode)
                {
                    //the only thing you can do is clear and abort the selected
                    menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(false);
                    return false;
                }
                else
                {
                    menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(false);
                    menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(false);

                    TransferStates transferStates = TransferStates.None;
                    bool failed = false;
                    List<TransferItem> transfersSelected = new List<TransferItem>();
                    foreach(int position in BatchSelectedItems)
                    {
                        var ti = TransferItemManagerWrapped.GetItemAtUserIndex(position);
                        if(ti is TransferItem singleTi)
                        {
                            transfersSelected.Add(singleTi);
                        }
                        else if(ti is FolderItem folderTi)
                        {
                            transfersSelected.AddRange(folderTi.TransferItems);
                        }
                    }
                    TransferViewHelper.GetStatusNumbers(transfersSelected, out int numInProgress, out int numFailed, out int numPaused, out int numSucceeded, out int numQueued);

                    if(numPaused!=0)
                    {
                        menu.FindItem(Resource.Id.resume_selected_batch).SetVisible(true);
                    }
                    if(numInProgress!=0 || numQueued!=0)
                    {
                        menu.FindItem(Resource.Id.pause_selected_batch).SetVisible(true);
                    }
                    if(numFailed!=0)
                    {
                        menu.FindItem(Resource.Id.retry_all_failed_batch).SetVisible(true);
                    }

                    //clear all complete??

                }
                return false;
            }

            public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
            {
                switch(item.ItemId)
                {
                    //this is the only option that uploads gets
                    case Resource.Id.action_cancel_and_clear_all_batch:
                        MainActivity.LogInfoFirebase("action_cancel_and_clear_batch Pressed");
                        SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        TransferItemManagerWrapped.CancelSelectedItems(true);
                        TransferItemManagerWrapped.ClearSelectedItemsAndClean();
                        var selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach(int pos in selected)
                        {
                            Adapter.NotifyItemRemoved(pos);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        TransfersActionMode.Finish(); //TransfersActionMode can be null!
                        break;
                    case Resource.Id.pause_selected_batch:
                        TransferItemManagerWrapped.CancelSelectedItems(false);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        //since all selected stuff is going away. its what Gmail action mode does.
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.resume_selected_batch:
                        Frag.RetryAllConditionEntry(false, true);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.retry_all_failed_batch:
                        Frag.RetryAllConditionEntry(true, true);
                        selected = BatchSelectedItems.ToArray();
                        BatchSelectedItems.Clear();
                        foreach (int pos in selected)
                        {
                            Adapter.NotifyItemChanged(pos);
                        }
                        TransfersActionMode.Finish();
                        break;
                    case Resource.Id.select_all:
                        BatchSelectedItems.Clear();
                        int cnt = TransfersActionModeCallback.Adapter.ItemCount;
                        for (int i = 0; i < cnt; i ++)
                        {
                            BatchSelectedItems.Add(i);
                        }

                        TransfersActionModeCallback.Adapter.NotifyDataSetChanged();

                        TransfersActionMode.Title = string.Format("{0} Selected", cnt.ToString());
                        TransfersActionMode.Invalidate();
                        return true;
                    case Resource.Id.invert_selection:
                        ForceOutIfZeroSelected = false;
                        List<int> oldOnes = BatchSelectedItems.ToList();
                        BatchSelectedItems.Clear();
                        List<int> all = new List<int>();
                        int cnt1 = TransfersActionModeCallback.Adapter.ItemCount;
                        for (int i = 0; i < cnt1; i++)
                        {
                            all.Add(i);
                        }
                        BatchSelectedItems = all.Except(oldOnes).ToList();

                        TransfersActionModeCallback.Adapter.NotifyDataSetChanged();

                        TransfersActionMode.Title = string.Format("{0} Selected", BatchSelectedItems.Count.ToString());
                        TransfersActionMode.Invalidate();
                        return true;
                }
                return true;
            }

            public void OnDestroyActionMode(ActionMode mode)
            {

                int[] prevSelectedItems = new int[BatchSelectedItems.Count];
                BatchSelectedItems.CopyTo(prevSelectedItems);
                TransfersActionMode = null;
                BatchSelectedItems.Clear();
                this.Adapter.IsInBatchSelectMode = false;
                foreach (int i in prevSelectedItems)
                {
                    this.Adapter.NotifyItemChanged(i);
                }

                //SoulSeekState.MainActivityRef.SupportActionBar.Show();
            }

        }


        public void GetQueuePosition(TransferItem ttItem)
        {
            int queueLenOld = ttItem.QueueLength;

            Func<TransferItem, object> actionOnComplete = new Func<TransferItem, object>((TransferItem t) =>
            {
                try
                {
                    if (queueLenOld == t.QueueLength) //always true bc its a reference...
                    {
                        if(queueLenOld == int.MaxValue)
                        {
                            Toast.MakeText(SoulSeekState.MainActivityRef, "Position in queue is unknown.", ToastLength.Short).Show();
                        }
                        else
                        { 
                            Toast.MakeText(SoulSeekState.MainActivityRef, string.Format(SoulSeekState.MainActivityRef.GetString(Resource.String.position_is_still_), t.QueueLength), ToastLength.Short).Show();
                        }
                    }
                    else
                    {
                        int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(t);
                        if (indexOfItem == -1 && InUploadsMode)
                        {
                            return null;
                        }
                        recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
                    }
                }
                catch (System.Exception e)
                {
                    MainActivity.LogFirebase("actionOnComplete" + e.Message + e.StackTrace);
                }

                return null;
            });

            MainActivity.GetDownloadPlaceInQueue(ttItem.Username, ttItem.FullFilename, true, false, ttItem, actionOnComplete);
        }

        public void UpdateQueueState(string fullFilename) //Add this to the event handlers so that when downloads are added they have their queue position.
        {
            try
            {
                if(InUploadsMode)
                {
                    return;
                }
                int indexOfItem = TransferItemManagerDL.GetUserIndexForTransferItem(fullFilename);
                MainActivity.LogDebug("NotifyItemChanged + UpdateQueueState" + indexOfItem);
                MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: " + fullFilename);
                if (recyclerTransferAdapter.ItemCount == indexOfItem)
                {

                }
                MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
                recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
            }
            catch (System.Exception)
            {

            }
        }

        //private void SoulseekClient_UI_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        //{

        //    if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut))
        //    {

        //        //this gets called regardless of UI lifecycle, so I am guessing that this.transferItems is null....
        //        //maybe better to make static and independent... bc you should still be able to update an item as failed even if UI is disposed
        //        //if(transferItems==null)
        //        //{
        //        //    MainActivity.LogFirebase(("transferItems is null" + " SoulseekClient_TransferStateChanged"));
        //        //    return;
        //        //} 
        //        if (relevantItem == null)
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            relevantItem.Failed = true;
        //            Action action = new Action(()=>{refreshListViewSpecificItem(indexOfItem); });
        //            SoulSeekState.MainActivityRef.RunOnUiThread(action);
        //            //Activity.RunOnUiThread(action); //this is probably the cause of the nullref.  since Activity is null whenever Context is null i.e. onDeattach or not yet attached..
        //        }
        //    }
        //    else if(e.Transfer.State.HasFlag(TransferStates.Queued))
        //    {
        //        //if (relevantItem == null)
        //        //{
        //        //    return;
        //        //}
        //        //relevantItem.Queued = true;
        //        //if(relevantItem.QueueLength!=0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
        //        //{
        //        //    //nothing to do, bc its already set..
        //        //    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, null);
        //        //}
        //        //else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
        //        //{
        //        //    //GET QUEUE LENGTH AND UPDATE...
        //        //    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename,null);
        //        //}
        //        Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
        //        SoulSeekState.MainActivityRef.RunOnUiThread(action);
        //    }
        //    else if(e.Transfer.State.HasFlag(TransferStates.Initializing))
        //    {
        //        if (relevantItem == null)
        //        {
        //            return;
        //        }
        //        //clear queued flag...
        //        relevantItem.Queued = false;
        //        relevantItem.QueueLength = 0;
        //        Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
        //        SoulSeekState.MainActivityRef.RunOnUiThread(action);
        //    }
        //    else if(e.Transfer.State.HasFlag(TransferStates.Completed))
        //    {
        //        if (relevantItem == null)
        //        {
        //            return;
        //        }
        //        if(!e.Transfer.State.HasFlag(TransferStates.Cancelled))
        //        {
        //            //clear queued flag...
        //            relevantItem.Progress = 100;
        //            Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
        //            SoulSeekState.MainActivityRef.RunOnUiThread(action);
        //        }
        //    }
        //    else
        //    {
        //        Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
        //        SoulSeekState.MainActivityRef.RunOnUiThread(action);
        //    }
        //}



        private void refreshListViewSafe()
        {
            try
            {
                refreshListView();
            }
            catch (System.Exception)
            {

            }
        }

        private void ClearProgressBarColor(ProgressBar pb)
        {
#pragma warning disable 0618
            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                pb.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
            }
            else
            {
                pb.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
            }
#pragma warning restore 0618
        }


        private void refreshItemProgress(int indexToRefresh, int progress, TransferItem relevantItem, bool wasFailed, double avgSpeedBytes)
        {
            //View v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh);


            //METHOD 1 of updating... (causes flicker)
            //recyclerViewTransferItems.GetAdapter().NotifyItemChanged(indexToRefresh); //this index is the index in the transferItem list...

            //METHOD 2 of updating (no flicker)

            //so this guys adapter is good and up to date.  but anything regarding View is bogus. Including the Views TextViews and InnerTransferItems. but its Adapter is good and visually everything looks fine...

            ITransferItemView v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh) as ITransferItemView; //its doing the wrong one!!! also its a bogus view, not shown anywhere on screen...
            if (v != null) //it scrolled out of view which is find bc it will get updated when it gets rebound....
            {
                if (v is TransferItemViewFolder)
                {

                    int prog = (v.InnerTransferItem as FolderItem).GetFolderProgress(out long totalBytes, out long completedBytes);
                    v.progressBar.Progress = prog;
                    if(v.GetShowProgressSize())
                    {
                        (v.GetProgressSizeTextView() as ProgressSizeTextView).Progress = prog;
                        TransferViewHelper.SetSizeText(v.GetProgressSizeTextView(), prog, completedBytes);
                    }

                    TimeSpan? timeRemaining = null;
                    long bytesRemaining = totalBytes - completedBytes;
                    if (avgSpeedBytes != 0)
                    {
                        timeRemaining = TimeSpan.FromSeconds(bytesRemaining / avgSpeedBytes);
                    }
                    (v.InnerTransferItem as FolderItem).RemainingFolderTime = timeRemaining;
                    //TODO chain avg speeds so that its per folder rather than per transfer.

                    if (relevantItem.State.HasFlag(TransferStates.InProgress))
                    {
                        if(v.GetShowSpeed())
                        {
                            v.GetAdditionalStatusInfoView().Text = Helpers.GetTransferSpeedString(avgSpeedBytes) + "  •  " + TransferViewHelper.GetTimeRemainingString(timeRemaining);
                        }
                        else
                        {
                            v.GetAdditionalStatusInfoView().Text = TransferViewHelper.GetTimeRemainingString(timeRemaining);
                        }
                        
                    }
                    else if (relevantItem.State.HasFlag(TransferStates.Queued) && !(relevantItem.IsUpload()))
                    {
                        int queueLen = v.InnerTransferItem.GetQueueLength();
                        if(queueLen == int.MaxValue) //unknown
                        {
                            v.GetAdditionalStatusInfoView().Text = "";
                        }
                        else
                        {
                            v.GetAdditionalStatusInfoView().Text = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.position_), queueLen.ToString());
                        }
                    }
                    else
                    {
                        v.GetAdditionalStatusInfoView().Text = "";
                    }


                    //TransferViewHelper.SetAdditionalStatusText(v.GetAdditionalStatusInfoView(), v.InnerTransferItem, relevantItem.State);
                    if (wasFailed)
                    {
                        ClearProgressBarColor(v.progressBar);
                    }
                }
                else
                {
                    v.progressBar.Progress = progress;
                    if(v.GetShowProgressSize())
                    {
                        TransferViewHelper.SetSizeText(v.GetProgressSizeTextView(), relevantItem.Progress, relevantItem.Size);
                    }
                    TransferViewHelper.SetAdditionalStatusText(v.GetAdditionalStatusInfoView(), relevantItem, relevantItem.State, v.GetShowSpeed());
                    if (wasFailed)
                    {
                        ClearProgressBarColor(v.progressBar);
                    }
                }
            }
        }
        private void refreshListViewSpecificItem(int indexOfItem)
        {
            //creating the TransferAdapter can cause a Collection was Modified error due to transferItems.
            //maybe a better way to do this is .ToList().... rather than locking...
            //TransferAdapter customAdapter = null;
            if (Context == null)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
                //customAdapter = new TransferAdapter(SoulSeekState.MainActivityRef, transferItems);
            }
            else
            {
                //customAdapter = new TransferAdapter(Context, transferItems);
            }
            if (this.noTransfers == null)
            {
                MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            MainActivity.LogDebug("NotifyItemChanged" + indexOfItem);
            MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: ");
            MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
            if (recyclerTransferAdapter.ItemCount == indexOfItem)
            {

            }
            recyclerTransferAdapter.NotifyItemChanged(indexOfItem);

        }

        public void refreshListView(Action specificRefreshAction = null)
        {
            //creating the TransferAdapter can cause a Collection was Modified error due to transferItems.
            //maybe a better way to do this is .ToList().... rather than locking...
            //TransferAdapter customAdapter = null;
            if (Context == null)
            {
                if (SoulSeekState.MainActivityRef == null)
                {
                    MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, MainActivityRef and Context are null");
                    return;
                }
                //customAdapter = new TransferAdapter(SoulSeekState.MainActivityRef, transferItems);
            }
            else
            {
                //customAdapter = new TransferAdapter(Context, transferItems);
            }
            if (this.noTransfers == null)
            {
                MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            SetNoTransfersMessage();
            if (specificRefreshAction == null)
            {
                //primaryListView.Adapter = (customAdapter); try with just notifyDataSetChanged...

                //int oldCount = recyclerTransferAdapter.ItemCount;
                //int newCount = transferItems.Count;
                //MainActivity.LogDebug("!!! recyclerTransferAdapter.NotifyDataSetChanged() old:: " + oldCount + " new: " + newCount);
                ////old 73, new 73...
                //recyclerTransferAdapter.NotifyItemRangeRemoved(0,oldCount);
                //recyclerTransferAdapter.NotifyItemRangeInserted(0,newCount);

                recyclerTransferAdapter.NotifyDataSetChanged();
                //(primaryListView.Adapter as TransferAdapter).NotifyDataSetChanged();

            }
            else
            {
                specificRefreshAction();
            }
        }

        //public string const IndividualItemType = 1;
        //public string const FolderItemType = 2;

        public class TransferAdapterRecyclerIndividualItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerIndividualItem(System.Collections.IList ti) : base(ti)
            {
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as TransferItem, this.IsInBatchSelectMode);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                bool TYPE_TO_USE = true;
                ITransferItemView view = TransferItemViewDetails.inflate(parent, this.showSizes, this.showSpeed);

                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += TransferAdapterRecyclerIndividualItem_Click;
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);
            }

            private void TransferAdapterRecyclerIndividualItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
            }

            protected void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                if (!IsInBatchSelectMode)
                {
                    setSelectedItem((sender as ITransferItemView).InnerTransferItem);
                    (sender as View).ShowContextMenu();
                }
                else
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
            }

        }

        



        public class TransferAdapterRecyclerFolderItem : TransferAdapterRecyclerVersion
        {
            public TransferAdapterRecyclerFolderItem(System.Collections.IList ti) : base(ti)
            {
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as FolderItem, this.IsInBatchSelectMode);
                //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                ITransferItemView view = TransferItemViewFolder.inflate(parent, this.showSizes, this.showSpeed);
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).Click += TransferAdapterRecyclerFolderItem_Click;
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);
            }

            private void TransferAdapterRecyclerFolderItem_Click(object sender, EventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
                else
                {
                    FolderItem f = (sender as ITransferItemView).InnerTransferItem as FolderItem;
                    setSelectedItem(f);
                    if (InUploadsMode)
                    {
                        CurrentlySelectedUploadFolder = f;
                    }
                    else
                    {
                        CurrentlySelectedDLFolder = f;
                    }

                    TransfersFragment.SaveScrollPositionOnMovingIntoFolder();
                    TransfersFragment.SetRecyclerAdapter();
                    SoulSeekState.MainActivityRef.SetTransferSupportActionBarState();
                    SoulSeekState.MainActivityRef.InvalidateOptionsMenu();
                }
            }

            protected void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                if (IsInBatchSelectMode)
                {
                    ToggleItemBatchSelect(this, (sender as ITransferItemView).ViewHolder.AdapterPosition);
                }
                else
                {
                    //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
                    //pop.Inflate(Resource.Menu.download_diag_options);
                    //pop.Show();
                    setSelectedItem((sender as ITransferItemView).InnerTransferItem);
                    (sender as View).ShowContextMenu();
                }
            }

        }

        public class ProgressSizeTextView : TextView
        {
            public int Progress = 0;
            private readonly bool isInNightMode = false;
            public ProgressSizeTextView(Context context, IAttributeSet attrs) : base(context, attrs)
            {
                isInNightMode = DownloadDialog.InNightMode(context);
            }
            protected override void OnDraw(Canvas canvas)
            {
                if(isInNightMode)
                {
                    canvas.Save();
                    this.SetTextColor(Color.White);
                    base.OnDraw(canvas);
                    canvas.Restore();
                }
                else
                {
                    Rect rect = new Rect();
                    this.GetDrawingRect(rect);
                    rect.Right = (int)(rect.Left + (Progress * .01) * (rect.Right - rect.Left));
                    canvas.Save();
                    canvas.ClipRect(rect, Region.Op.Difference);
                    this.SetTextColor(Color.Black);
                    base.OnDraw(canvas);
                    canvas.Restore();    

                    canvas.Save();
                    canvas.ClipRect(rect, Region.Op.Intersect); // lets draw inside center rect only
                    this.SetTextColor(Color.White);
                    base.OnDraw(canvas);
                    canvas.Restore();
                }
            }
        }


        public abstract class TransferAdapterRecyclerVersion : RecyclerView.Adapter //<TransferAdapterRecyclerVersion.TransferViewHolder>
        {
            protected System.Collections.IList localDataSet;
            public override int ItemCount => localDataSet.Count;
            protected ITransferItem selectedItem = null;
            public bool IsInBatchSelectMode;

            public TransfersFragment TransfersFragment;
#if DEBUG
            public void SelectedDebugInfo(ITransferItem iti)
            {

                int position = TransferItemManagerWrapped.GetUserIndexForITransferItem(iti);
                MainActivity.LogDebug($"position: {position} ti name: {iti.GetDisplayName()}");

            }
#endif

            public void setSelectedItem(ITransferItem item)
            {
                this.selectedItem = item;
#if DEBUG
                SelectedDebugInfo(item);
#endif
                if(this.selectedItem == null)
                {
                    MainActivity.LogInfoFirebase("selected item was set as null");
                }
            }

            public ITransferItem getSelectedItem()
            {
                return this.selectedItem;
            }

            //public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            //{
            //    (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position] as TransferItem);
            //    //(holder as TransferViewHolder).getTransferItemView().LongClick += TransferAdapterRecyclerVersion_LongClick; //I dont think we should be adding this here.  you get 3 after a short time...
            //}




            protected readonly bool showSpeed = false;
            protected readonly bool showSizes = false;
            public TransferAdapterRecyclerVersion(System.Collections.IList tranfersList)
            {
                localDataSet = tranfersList;
                showSpeed = SoulSeekState.TransferViewShowSpeed;
                showSizes = SoulSeekState.TransferViewShowSizes;
            }

        }

        public const int UNIQUE_TRANSFER_GROUP_ID = 303;
        public class TransferViewHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
        {
            private ITransferItemView transferItemView;


            public TransferViewHolder(View view) : base(view)
            {
                //super(view);
                // Define click listener for the ViewHolder's View

                transferItemView = (ITransferItemView)view;
                transferItemView.ViewHolder = this;
                (transferItemView as View).SetOnCreateContextMenuListener(this);
            }

            public ITransferItemView getTransferItemView()
            {
                return transferItemView;
            }

            public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
            {
                //base.OnCreateContextMenu(menu, v, menuInfo);
                ITransferItemView tvh = v as ITransferItemView;
                TransferItem ti = null;
                FolderItem fi = null;
                TransferStates folderItemState = TransferStates.None;
                bool isTransferItem = false;
                bool anyFailed = false;
                //bool anyOffline = false;
                bool isUpload = false;
                if (tvh?.InnerTransferItem is TransferItem tvhi)
                {
                    isTransferItem = true;
                    ti = tvhi;
                    isUpload = ti.IsUpload();
                }
                else if (tvh?.InnerTransferItem is FolderItem tvhf)
                {
                    fi = tvhf;
                    folderItemState = fi.GetState(out anyFailed, out _);
                    isUpload = fi.IsUpload();
                }
                //else
                //{
                    //shouldnt happen....
                    AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)menuInfo;
                    int pos1 = info?.Position ?? -1;
                //}


                //if somehow we got here without setting the transfer item. then set it now...  you have menuInfo.Position, AND tvh.InnerTransferItem. and recyclerTransfer.GetSelectedItem() to check for null.

                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && ti.State.HasFlag(TransferStates.Cancelled) /*&& ti.Progress > 0*/) //progress > 0 doesnt work if someone queues an item as paused...
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 0, 0, Resource.String.resume_dl);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 0, 0, Resource.String.retry_dl);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && folderItemState.HasFlag(TransferStates.Cancelled)  /*&& fi.GetFolderProgress() > 0*/)
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 100, 0, "Resume Folder");
                        }
                        else if (tvh != null && fi != null && (!folderItemState.HasFlag(TransferStates.Completed) && !folderItemState.HasFlag(TransferStates.Succeeded) && !folderItemState.HasFlag(TransferStates.Errored) && !folderItemState.HasFlag(TransferStates.TimedOut) && !folderItemState.HasFlag(TransferStates.Rejected)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 101, 0, "Pause Folder");
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && !(Helpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 103, 0, "Abort Upload");
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && !(Helpers.IsUploadCompleteOrAborted(folderItemState)));
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 101, 0, "Abort Uploads");
                        }
                    }
                }
                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (ti.State.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.cancel_and_clear);
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (folderItemState.HasFlag(TransferStates.Succeeded)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, Resource.String.cancel_and_clear);
                        }
                    }
                }
                else
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (Helpers.IsUploadCompleteOrAborted(ti.State)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                            //if completed then we dont need to show the cancel option...
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, "Abort and Clear Upload");
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null && (Helpers.IsUploadCompleteOrAborted(folderItemState)))
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 1, 1, Resource.String.clear_from_list);
                        }
                        else
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 2, 2, "Abort and Clear Uploads");
                        }
                    }
                }

                if (!isUpload)
                {
                    if (isTransferItem)
                    {

                        if (tvh != null && ti != null)
                        {
                            if (ti.QueueLength > 0)
                            {
                                //the queue length of a succeeded download can be 183......
                                //bc queue length AND free upload slots!!
                                if (ti.State.HasFlag(TransferStates.Succeeded) ||
                                    ti.State.HasFlag(TransferStates.Completed))
                                {
                                    //no op
                                }
                                else
                                {
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 3, 3, Resource.String.refresh_queue_pos);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (tvh != null && fi != null)
                        {
                            if (fi.GetQueueLength() > 0)
                            {
                                //the queue length of a succeeded download can be 183......
                                //bc queue length AND free upload slots!!
                                if (folderItemState.HasFlag(TransferStates.Succeeded) ||
                                    folderItemState.HasFlag(TransferStates.Completed))
                                {
                                    //no op
                                }
                                else
                                {
                                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 3, 3, Resource.String.refresh_queue_pos);
                                }
                            }
                        }
                    }
                }

                if (!isUpload)
                {
                    if (isTransferItem)
                    {
                        if (tvh != null && ti != null && (ti.State.HasFlag(TransferStates.Succeeded)) && ti.FinalUri != string.Empty)
                        {
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 4, 4, Resource.String.play_file);
                        }
                    }
                    else
                    {
                        if (folderItemState.HasFlag(TransferStates.TimedOut) || folderItemState.HasFlag(TransferStates.Rejected) || folderItemState.HasFlag(TransferStates.Errored) || anyFailed)
                        {
                            //no op
                            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 102, 4, "Retry Failed Files");
                        }
                    }
                }
                var subMenu = menu.AddSubMenu(UNIQUE_TRANSFER_GROUP_ID, 5, 5, "User Options");
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 6, 6, Resource.String.browse_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 7, 7, "Browse At Location");
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 8, 8, Resource.String.search_user_files);
                Helpers.AddAddRemoveUserMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, 9, 9, tvh.InnerTransferItem.GetUsername(), false);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 10, 10, Resource.String.msg_user);
                subMenu.Add(UNIQUE_TRANSFER_GROUP_ID, 11, 11, Resource.String.get_user_info);
                Helpers.AddUserNoteMenuItem(subMenu, UNIQUE_TRANSFER_GROUP_ID, 12, 12, tvh.InnerTransferItem.GetUsername());
                Helpers.AddGivePrivilegesIfApplicable(subMenu, 13);

                if (isUpload)
                {
                    menu.Add(UNIQUE_TRANSFER_GROUP_ID, 104, 6, "Ignore (Unshare) User");
                }
                //finally batch selection mode
                menu.Add(UNIQUE_TRANSFER_GROUP_ID, 105, 16, "Batch Select");

                //if (!isUpload)
                //{
                //    if (isTransferItem)
                //    {
                //        if(ti.State.HasFlag(TransferStates.UserOffline))
                //        {

                //        }
                //    }
                //    else
                //    {
                //        if(anyOffline)
                //        {
                //            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 106, 17, "Do Not Auto-Retry When User Goes Back Online");
                //            menu.Add(UNIQUE_TRANSFER_GROUP_ID, 106, 17, "Auto-Retry When User Goes Back Online");
                //        }
                //    }
                //}
            }

        }

        private void TransferProgressUpdated(object sender, SeekerApplication.ProgressUpdatedUI e)
        {
            bool needsRefresh = (e.ti.IsUpload() && TransfersFragment.InUploadsMode) || (!(e.ti.IsUpload()) && !(TransfersFragment.InUploadsMode));
            if (!needsRefresh)
            {
                return;
            }
            if (e.percentComplete != 0)
            {
                if (e.fullRefresh)
                {

                    Action action = refreshListViewSafe; //notify data set changed...
                                                         //if (indexRemoved!=-1)
                                                         //{

                    //    var refreshOnlySelected = new Action(() => {

                    //        MainActivity.LogDebug("notifyItemRemoved " + indexRemoved + "count: " + recyclerTransferAdapter.ItemCount);
                    //        if(indexRemoved == recyclerTransferAdapter.ItemCount)
                    //        {

                    //        }
                    //        recyclerTransferAdapter?.NotifyItemRemoved(indexRemoved);


                    //    });

                    //    var refresh1 = new Action(()  => refreshListView(refreshOnlySelected) );

                    //    action = refreshOnlySelected;
                    //}
                    //else
                    //{
                    //    action = refreshListViewSafe;
                    //}
                    Activity?.RunOnUiThread(action); //in case of rotation it is the ACTIVITY which will be null!!!!
                }
                else
                {
                    try
                    {
                        bool isNew = !ProgressUpdatedThrottler.ContainsKey(e.ti.FullFilename + e.ti.Username);

                        DateTime now = DateTime.UtcNow;
                        DateTime lastUpdated = ProgressUpdatedThrottler.GetOrAdd(e.ti.FullFilename + e.ti.Username, now); //this returns now if the key is not in the dictionary!
                        if (now.Subtract(lastUpdated).TotalMilliseconds > THROTTLE_PROGRESS_UPDATED_RATE || isNew)
                        {
                            ProgressUpdatedThrottler[e.ti.FullFilename + e.ti.Username] = now;
                        }
                        else if (e.wasFailed)
                        {
                            //still update..
                        }
                        else
                        {
                            //there was a bug where there were multiple instances of tabspageradapter and one would always get their event handler before the other
                            //basically updating a recyclerview that wasnt even visible, while the other was never getting to update due to the throttler.
                            //this is fixed by attaching and dettaching the event handlers on start / stop.
                            return;
                        }


                        //partial refresh just update progress..
                        //TransferItemManagerDL.GetTransferItemWithIndexFromAll(e.ti.FullFilename, out index);

                        //MainActivity.LogDebug("Index is "+index+" TransferProgressUpdated"); //tested!

                        //int indexToUpdate = transferItems.IndexOf(relevantItem);

                        Activity?.RunOnUiThread(() =>
                        {
                            int index = -1;
                            index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e.ti);
                            if (index == -1)
                            {
                                MainActivity.LogDebug("Index is -1 TransferProgressUpdated");
                                return;
                            }
                            MainActivity.LogDebug("UI THREAD TRANSFER PROGRESS UPDATED"); //this happens every 20ms.  so less often then tranfer progress updated.  usually 6 of those can happen before 2 of these.
                            refreshItemProgress(index, e.ti.Progress, e.ti, e.wasFailed, e.avgspeedBytes);

                        });



                    }
                    catch (System.Exception error)
                    {
                        MainActivity.LogFirebase(error.Message + " partial update");
                    }
                }
            }
        }

        private void TransferStateChangedItem(object sender, TransferItem ti)
        {
            Action action = new Action(() =>
            {

                int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(ti); //todo null ti
                if (index == -1)
                {
                    return; //this is likely an upload when we are on downloads page or vice versa.
                }
                refreshListViewSpecificItem(index);

            });
            SoulSeekState.MainActivityRef.RunOnUiThread(action);
        }

        private void TransferStateChanged(object sender, int index)
        {
            Action action = new Action(() =>
            {

                refreshListViewSpecificItem(index);

            });
            SoulSeekState.MainActivityRef.RunOnUiThread(action);
        }


        public override void OnStart()
        {
            SeekerApplication.StateChangedAtIndex += TransferStateChanged;
            SeekerApplication.StateChangedForItem += TransferStateChangedItem;
            SeekerApplication.ProgressUpdated += TransferProgressUpdated;
            MainActivity.TransferAddedUINotify += MainActivity_TransferAddedUINotify; ; //todo this should eventually be for downloads too.
            MainActivity.TransferItemQueueUpdated += TranferQueueStateChanged;

            if (recyclerTransferAdapter != null)
            {
                recyclerTransferAdapter.NotifyDataSetChanged();
            }

            base.OnStart();
        }

        private void MainActivity_TransferAddedUINotify(object sender, TransferItem e)
        {
            if (MainActivity.OnUIthread())
            {
                if (e.IsUpload() && InUploadsMode)
                {
                    lock (TransferItemManagerWrapped.GetUICurrentList())
                    { //todo can update this to do a partial refresh... just the index..
                        if (GroupByFolder && !CurrentlyInFolder())
                        {
                            //folderview - so we may insert or update
                            //int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
                            refreshListView(); //just to be safe...

                        }
                        else
                        {
                            //int index = TransferItemManagerWrapped.GetUserIndexForTransferItem(e);
                            //refreshListView(()=>{recyclerTransferAdapter.NotifyItemInserted(index); });
                            refreshListView(); //just to be safe...
                        }

                    }
                }
            }
            else
            {
                SoulSeekState.ActiveActivityRef.RunOnUiThread(() => { MainActivity_TransferAddedUINotify(null, e); });
            }
        }

        public override void OnStop()
        {
            SeekerApplication.StateChangedAtIndex -= TransferStateChanged;
            SeekerApplication.ProgressUpdated -= TransferProgressUpdated;
            SeekerApplication.StateChangedForItem -= TransferStateChangedItem;
            MainActivity.TransferItemQueueUpdated -= TranferQueueStateChanged;
            MainActivity.TransferAddedUINotify -= MainActivity_TransferAddedUINotify;
            base.OnStop();
        }

        //NOTE: there can be several TransfersFragment at a time.
        //this can be done by having many MainActivitys in the back stack
        //i.e. go to UserList > press browse files > go to UserList > press browse files --> 3 TransferFragments, 2 of which are stopped but not Destroyed.
        public override void OnCreate(Bundle savedInstanceState)
        {

            MainActivity.ClearDownloadAddedEventsFromTarget(this);
            MainActivity.DownloadAddedUINotify += SoulSeekState_DownloadAddedUINotify;
            //todo I dont think this should be here.  I think the only reason its not causing a problem is because the user cannot add a download from the transfer page.
            //if they could then the download might not show because this is OnCreate!! so it will only update the last one you created.  
            //so you can create a second one, back out of it, and the first one will not get recreated and so it will not have an event. 


            base.OnCreate(savedInstanceState);
        }


        private void SoulSeekState_DownloadAddedUINotify(object sender, DownloadAddedEventArgs e)
        {
            SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
            {
                //occurs on nonUI thread...
                //if there is any deadlock due to this, then do Thread.Start().
                lock (TransferItemManagerDL.GetUICurrentList())
                { //todo can update this to do a partial refresh... just the index..
                    refreshListView();
                }
            });
        }

        public static void SetupCancellationToken(TransferItem transferItem, CancellationTokenSource cts, out CancellationTokenSource oldToken)
        {
            transferItem.CancellationTokenSource = cts;
            if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), cts)) //returns false if it already exists in dict
            {
                //likely old already exists so just replace the old one
                oldToken = CancellationTokens[ProduceCancellationTokenKey(transferItem)];
                CancellationTokens[ProduceCancellationTokenKey(transferItem)] = cts;
            }
            else
            {
                oldToken = null;
            }
        }

        public static string ProduceCancellationTokenKey(TransferItem i)
        {
            return ProduceCancellationTokenKey(i.FullFilename, i.Size, i.Username);
        }

        public static string ProduceCancellationTokenKey(string fullFilename, long size, string username)
        {
            return fullFilename + size.ToString() + username;
        }


        public static System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource> CancellationTokens = new System.Collections.Concurrent.ConcurrentDictionary<string, CancellationTokenSource>();




    }
}
