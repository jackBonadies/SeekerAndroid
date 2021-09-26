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
        private volatile View rootView;
        private Button cbttn;
        private TextView cWelcome;
        private TextView cLoading;
        private bool refreshView = false;


        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.account_menu,menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }



        //private bool firstTime = true;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            HasOptionsMenu = true;
            MainActivity.LogDebug("LoginFragmentOnCreateView");
            StaticHacks.LoginFragment = this;
            if ((!SoulSeekState.currentlyLoggedIn) || SoulSeekState.Username==null || SoulSeekState.Password==null || SoulSeekState.Username==string.Empty)//you are not logged in if username or password is null
            {
                SoulSeekState.currentlyLoggedIn=false;
                this.rootView = inflater.Inflate(Resource.Layout.login, container, false);


                Button bttn = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                bttn.Click += LogInClick;

                rootView.FindViewById<EditText>(Resource.Id.editText).FocusChange += SearchFragment.MainActivity_FocusChange;
                rootView.FindViewById<EditText>(Resource.Id.editText2).FocusChange += SearchFragment.MainActivity_FocusChange;
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
                        login = SoulSeekState.SoulseekClient.ConnectAsync(SoulSeekState.Username, SoulSeekState.Password);
                    }
                    catch (InvalidOperationException)
                    {
                        Toast.MakeText(this.Context, Resource.String.we_are_already_logging_in, ToastLength.Short).Show();
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
                welcome.Text = string.Format(SoulSeekState.ActiveActivityRef.GetString(Resource.String.welcome),SoulSeekState.Username);
                welcome.Visibility = ViewStates.Gone;
                bttn.Visibility = ViewStates.Gone;

                Button settings = rootView.FindViewById<Button>(Resource.Id.settingsButton);
                settings.Visibility = ViewStates.Gone;
                settings.Click += Settings_Click;

                Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn, 0);
                this.cbttn = bttn;
                this.cLoading = rootView.FindViewById<TextView>(Resource.Id.loadingView);
                this.cWelcome = welcome;
                //firstTime = false;
                return rootView;
            }
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
                    MainActivity.LogFirebase("DNS Lookup of Server Failed. Falling back on hardcoded IP succeeded.");
                });
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
                SoulseekClient.DNS_LOOKUP_FAILED = false; // dont have to keep showing this... wait for next failure for it to be set...
            }

            Console.WriteLine("Update Login UI");
            bool cannotLogin = false;
            string msg = string.Empty;
            string msgToLog = string.Empty;
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
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_unreachable);
                        }
                        else if (t.Exception.InnerExceptions[0].Message.Contains("Connection refused"))
                        {
                            cannotLogin = true;
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.network_unreachable);
                        }
                        else
                        {
                            cannotLogin = true;
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                            msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;

                        }
                    }
                    else
                    {
                        msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                        cannotLogin = true;
                        msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                    }
                }
                else
                {
                    if(t.Exception!=null)
                    {
                        msgToLog = t.Exception.Message + t.Exception.StackTrace;
                    }
                    cannotLogin = true;
                    msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                }

                if(msgToLog!=string.Empty)
                {
                    MainActivity.LogDebug(msgToLog);
                    MainActivity.LogFirebase(msgToLog);
                }

                MainActivity.AddLoggedInLayout(this.rootView);
                MainActivity.BackToLogInLayout(this.rootView, LogInClick);
            }

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

                //View bttn = StaticHacks.RootView?.FindViewById<Button>(Resource.Id.buttonLogout);
                //View bttnTryTwo = this.rootView?.FindViewById<Button>(Resource.Id.buttonLogout);
                //if (bttn == null && bttnTryTwo == null)
                //{
                //    //THIS MEANS THAT WE STILL HAVE THE LOGINFRAGMENT NOT THE LOGGEDIN FRAGMENT
                //    RelativeLayout relLayout =  SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, this.rootView as ViewGroup, false) as RelativeLayout;
                //    relLayout.LayoutParameters = new ViewGroup.LayoutParams(this.rootView.LayoutParameters);
                //    var action1 = new Action(() => {
                //        (this.rootView as RelativeLayout).AddView(SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin, this.rootView as ViewGroup, false));
                //    });
                //    SoulSeekState.MainActivityRef.RunOnUiThread(action1);
                //}





                //Android.Support.Design.Widget.TabLayout.Tab tab = tabLayout.GetTabAt(0);
                //tab.Select();
                //Fragment frg = null;
                //System.Type fragmentClass;
                //fragmentClass = typeof(LoginFragment);


                //    frg = (Android.Support.V4.App.Fragment)Activator.CreateInstance(fragmentClass);//.N();

                //getFragmentManager()
                //    .beginTransaction()
                //    .replace(Resource.Layout.login, frg)
                //    .commit();
                //View x = SoulSeekState.MainActivityRef.LayoutInflater.Inflate(Resource.Layout.loggedin,null);
                //(this.rootView as ViewGroup).AddView(x);
                //SoulSeekState.MainActivityRef.RecreateFragment(this);
                //LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
                //View rootView = inflater.Inflate(Resource.Layout.loggedin,null);

                MainActivity.UpdateUIForLoggedIn(this.rootView, LogoutClick, cWelcome, cbttn, cLoading, Settings_Click);


                //var action = new Action(() => {
                //    //this is the case where it already has the loggedin fragment loaded.
                //    Button bttn = null;
                //    TextView welcome = null;
                //    TextView loading = null;
                //    EditText editText = null;
                //    EditText editText2 = null;
                //    TextView textView = null;
                //    Button buttonLogin = null;
                //    try
                //    {
                //        if(StaticHacks.RootView != null)
                //        {
                //             bttn = StaticHacks.RootView.FindViewById<Button>(Resource.Id.buttonLogout);
                //             welcome = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.userNameView);
                //             loading = StaticHacks.RootView.FindViewById<TextView>(Resource.Id.loadingView);
                //        }
                //        else
                //        {
                //            bttn = this.rootView.FindViewById<Button>(Resource.Id.buttonLogout);
                //            welcome = this.rootView.FindViewById<TextView>(Resource.Id.userNameView);
                //            loading = this.rootView.FindViewById<TextView>(Resource.Id.loadingView);
                //            editText = this.rootView.FindViewById<EditText>(Resource.Id.editText);
                //            editText2 = this.rootView.FindViewById<EditText>(Resource.Id.editText2);
                //            textView = this.rootView.FindViewById<TextView>(Resource.Id.textView);
                //            buttonLogin = this.rootView.FindViewById<Button>(Resource.Id.buttonLogin);
                //        }
                //    }
                //    catch
                //    {

                //    }
                //    if(welcome!=null)
                //    {
                //        welcome.Visibility = ViewStates.Visible;
                //        bttn.Visibility = ViewStates.Visible;
                //        Android.Support.V4.View.ViewCompat.SetTranslationZ(bttn,90);
                //        bttn.Click-= Bttn_Click1;
                //        bttn.Click += Bttn_Click1;
                //        loading.Visibility = ViewStates.Gone;
                //        welcome.Text = "Welcome, " + SoulSeekState.Username;
                //    }
                //    else if(cWelcome != null)
                //    {
                //        //cWelcome.Visibility = ViewStates.Visible;
                //        //cbttn.Visibility = ViewStates.Visible;
                //        //cLoading.Visibility = ViewStates.Gone;
                //    }
                //    else
                //    {
                //        StaticHacks.UpdateUI = true;//if we arent ready rn then do it when we are..
                //    }
                //    if(editText != null)
                //    {
                //        editText.Visibility = ViewStates.Gone;
                //        editText2.Visibility = ViewStates.Gone;
                //        textView.Visibility = ViewStates.Gone;
                //        buttonLogin.Visibility = ViewStates.Gone;
                //    }

                //});
                //SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
            else
            {
                var action = new Action(() =>
                {
                    string message = msg;
                    Toast.MakeText(SoulSeekState.MainActivityRef, msg, ToastLength.Long).Show();
                    SoulSeekState.currentlyLoggedIn = false;
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


            EditText user = this.Activity.FindViewById<EditText>(Resource.Id.editText); //nullref
            EditText pass = this.Activity.FindViewById<EditText>(Resource.Id.editText2);
            Task login = null;
            try
            {
                //System.Net.IPAddress ipaddr = null;
                //try
                //{
                //MUST DO THIS ON A SEPARATE THREAD !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //ipaddr = System.Net.Dns.GetHostEntry("vps.slsknet.org").AddressList[0];
                //Android.Net.DnsResolver.Instance.Query(null, "vps.slsknet.org",Android.Net.DnsResolverFlag.Empty,this.Context.MainExecutor,null,this);
                if (user.Text == null || user.Text == string.Empty || pass.Text == null || pass.Text == string.Empty)
                {
                    Toast.MakeText(this.Activity, Resource.String.no_empty_user_pass, ToastLength.Long).Show();
                    return;
                }
                login = SoulSeekState.SoulseekClient.ConnectAsync(user.Text, pass.Text);
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
                SoulSeekState.Username = user.Text;
                SoulSeekState.Password = pass.Text;
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
        public override int Compare(SearchResponse x, SearchResponse y)
        {
            if(x.Username==y.Username)
            {
                if(x.Files.Count == y.Files.Count)
                {
                    if(x.Files.First().Filename == y.Files.First().Filename)
                    {
                        return 0;
                    }
                }
            }
            return base.Compare(x,y); //the actual comparison for which is "better"
        }
    }

    public class SearchResultComparable : IComparer<SearchResponse>
    {
        public virtual int Compare(SearchResponse x, SearchResponse y)
        {
            //highest precedence. locked files.
            if ((x.LockedFileCount != 0 && y.LockedFileCount == 0) || (x.LockedFileCount == 0 && y.LockedFileCount != 0))
            {
                if (x.LockedFileCount == 0)
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
            return 0;
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

    public class SearchTab
    {
        public List<SearchResponse> SearchResponses = new List<SearchResponse>();
        public SortedDictionary<SearchResponse, object> SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparable());
        public bool FilteredResults = false;
        public bool FilterSticky = false;
        public string FilterString = string.Empty;
        public List<string> WordsToAvoid = new List<string>();
        public List<string> WordsToInclude = new List<string>();
        public FilterSpecialFlags FilterSpecialFlags = new FilterSpecialFlags();
        public List<SearchResponse> FilteredResponses = new List<SearchResponse>();
        public SearchTarget SearchTarget = SearchTarget.AllUsers;
        public bool CurrentlySearching = false;
        public string SearchTargetChosenRoom = string.Empty;
        public string SearchTargetChosenUser = string.Empty;
        public int LastSearchResponseCount = -1; //this tell us how many we have filtered.  since we only filter when its the Current UI Tab.
        public CancellationTokenSource CancellationTokenSource = null;
        public DateTime LastRanTime = DateTime.MinValue;

        public string LastSearchTerm = string.Empty;
        public int LastSearchResultsCount = 0;

        public SearchTab Clone(bool forWishlist)
        {
            SearchTab clone = new SearchTab();
            clone.SearchResponses = this.SearchResponses.ToList();
            SortedDictionary<SearchResponse, object> cloned = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist());
            foreach (var entry in SortHelper)
            {
                if(!cloned.ContainsKey(entry.Key))
                {
                    cloned.Add(entry.Key, entry.Value);
                }
            }
            clone.SortHelper = cloned;
            clone.FilteredResults = this.FilteredResults;
            clone.FilterSticky = this.FilterSticky;
            clone.FilterString = this.FilterString;
            clone.WordsToAvoid = this.WordsToAvoid.ToList();
            clone.WordsToInclude = this.WordsToInclude.ToList();
            clone.FilterSpecialFlags = this.FilterSpecialFlags;
            clone.FilteredResponses = this.FilteredResponses.ToList();
            clone.CurrentlySearching = this.CurrentlySearching;
            clone.SearchTarget = this.SearchTarget;
            clone.SearchTargetChosenRoom = this.SearchTargetChosenRoom;
            clone.SearchTargetChosenUser = this.SearchTargetChosenUser;
            clone.LastSearchResponseCount = this.LastSearchResponseCount;
            clone.LastSearchTerm = this.LastSearchTerm;
            clone.LastSearchResultsCount = this.LastSearchResultsCount;
            clone.LastRanTime = this.LastRanTime;
            return clone;
        }
    }

    [Serializable]
    public class SavedStateSearchTab
    {
        //there are all of the things we must save in order to later restore a SearchTab
        List<SearchResponse> searchResponses;
        string LastSearchTerm;
        long LastRanTime;
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
        public static SearchTab GetTabFromSavedState(SavedStateSearchTab savedState)
        {
            SearchTab searchTab = new SearchTab();
            searchTab.SearchResponses = savedState.searchResponses;
            searchTab.LastSearchTerm = savedState.LastSearchTerm;
            searchTab.LastRanTime = new DateTime(savedState.LastRanTime);
            searchTab.SearchTarget = SearchTarget.Wishlist;
            searchTab.LastSearchResultsCount = searchTab.SearchResponses.Count;
            if(SearchFragment.FilterSticky)
            {
                searchTab.FilterSticky = SearchFragment.FilterSticky;
                searchTab.FilterString = SearchFragment.FilterStickyString;
                SearchFragment.ParseFilterString(searchTab);
            }
            searchTab.SortHelper = new SortedDictionary<SearchResponse, object>(new SearchResultComparableWishlist());
            foreach (SearchResponse resp in searchTab.SearchResponses)
            {
                if(!searchTab.SortHelper.ContainsKey(resp))
                {
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
        public static void SaveStateToSharedPreferences()
        {
            string stringToSave = string.Empty;
            //we should only save things we need for the wishlist searches.
            List<int> tabsToSave = SearchTabDialog.GetWishesTabIds();
            if(tabsToSave.Count==0)
            {
                MainActivity.LogDebug("Nothing to Save");
            }
            else
            {
                Dictionary<int,SavedStateSearchTab> savedStates = new Dictionary<int, SavedStateSearchTab>();
                foreach(int tabIndex in tabsToSave)
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
                editor.PutString(SoulSeekState.M_SearchTabsState, stringToSave);
                editor.Commit();
            }
        }

        public static void RestoreStateFromSharedPreferences()
        {
            string savedState = SoulSeekState.SharedPreferences.GetString(SoulSeekState.M_SearchTabsState, string.Empty);
            if(savedState==string.Empty)
            {
                return;
            }
            else
            {
                using(System.IO.MemoryStream memStream = new System.IO.MemoryStream(Convert.FromBase64String(savedState)))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    var savedStateDict = formatter.Deserialize(memStream) as Dictionary<int, SavedStateSearchTab>;
                    int lowestID = int.MaxValue;
                    foreach(var pair in savedStateDict)
                    {
                        if(pair.Key<lowestID)
                        {
                            lowestID = pair.Key;
                        }
                        SearchTabCollection[pair.Key] = SavedStateSearchTab.GetTabFromSavedState(pair.Value);
                    }
                    if(lowestID!=int.MaxValue)
                    {
                        lastWishlistID = lowestID;
                    }
                }
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
            SearchTabHelper.SaveStateToSharedPreferences();
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

        public static List<SearchResponse> FilteredResponses
        {
            get
            {
                return SearchTabCollection[CurrentTab].FilteredResponses;
            }
            set
            {
                SearchTabCollection[CurrentTab].FilteredResponses = value;
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
            if(!FilterSticky || force)
            {
                SearchTabHelper.FilterString = string.Empty;
                SearchTabHelper.FilteredResults = false;
                SearchTabHelper.WordsToAvoid.Clear();
                SearchTabHelper.WordsToInclude.Clear();
                SearchTabHelper.FilterSpecialFlags.Clear();
                EditText filterText = rootView.FindViewById<EditText>(Resource.Id.filterText);
                filterText.Text = string.Empty;
            }
        }

        public static void SetSearchResultStyle(int style)
        {
            //in case its out of range bc we add / rm enums in the future...
            foreach (int i in System.Enum.GetValues(typeof(SearchResultStyleEnum)))
            {
                if(i==style)
                {
                    SearchResultStyle = (SearchResultStyleEnum)(i);
                    break;
                }
            }
        }

        public override void SetMenuVisibility(bool menuVisible)
        {
            //this is necessary if programmatically moving to a tab from another activity..
            if(menuVisible)
            {
                var navigator = SoulSeekState.MainActivityRef?.FindViewById<BottomNavigationView>(Resource.Id.navigation);
                if(navigator!=null)
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
            if(SearchTabHelper.SearchTarget==SearchTarget.Wishlist)
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(false);
            }
            else
            {
                menu.FindItem(Resource.Id.action_add_to_wishlist).SetVisible(true);
            }

            ActionBarMenu = menu;
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
            if (SearchTabHelper.SearchTarget==SearchTarget.Wishlist)
            {
                numTabs = -1;
            }
            else
            {
                numTabs = SearchTabHelper.SearchTabCollection.Keys.Count;
            }
            int idOfDrawable = int.MinValue;
            if(numTabs>10)
            {
                numTabs=10;
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

            SetCustomViewTabNumberInner(imgView,c);
        }

        public EditText GetCustomViewSearchHere()
        {
            if(this.Activity is Android.Support.V7.App.AppCompatActivity appCompat)
            {
                var editText = appCompat.SupportActionBar?.CustomView?.FindViewById<EditText>(Resource.Id.searchHere);
                if(editText==null)
                {

                }
                return editText;
            }
            else
            {
                var editText =  SoulSeekState.MainActivityRef.SupportActionBar.CustomView.FindViewById<EditText>(Resource.Id.searchHere);
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
            
        }

        private void SetTransitionDrawableState()
        {
            if(SearchTabHelper.CurrentlySearching)
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

        public void GoToTab(int tabToGoTo, bool force, bool fromIntent =false)
        {
            if(force || tabToGoTo != SearchTabHelper.CurrentTab)
            {
                int lastTab = SearchTabHelper.CurrentTab;
                SearchTabHelper.CurrentTab = tabToGoTo;

                //update for current tab
                //set icon state
                //set search text
                //set results
                //set filter if not sticky
                int fromTab = SearchTabHelper.CurrentTab;

                Action a = new Action(() => {

                    if(!SearchTabHelper.SearchTabCollection.ContainsKey(tabToGoTo))
                    {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.search_tab_error, ToastLength.Long).Show();
                        SearchTabHelper.CurrentTab = lastTab;
                        fromTab = lastTab;
                        return;
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
                        if(SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount != SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count)
                        {
                            MainActivity.LogDebug("filtering...");
                            UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                        }
                        SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                        SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses); //this throws, its not ready..
                        ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        lv.Adapter = (customAdapter);
                    }
                    else
                    {
                        SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        MainActivity.LogDebug("new tab refresh " + tabToGoTo + " count " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);
                        ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        lv.Adapter = (customAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if (!fromIntent)
                    {
                        GetTransitionDrawable().InvalidateSelf();
                    }
                    this.SetCustomViewTabNumberImageViewState();
                    if(this.Activity == null)
                    {
                        GetSearchFragmentMoreDiag();
                    }
                    this.Activity.InvalidateOptionsMenu(); //this wil be the new nullref if fragment isnt ready...
                });

                SoulSeekState.MainActivityRef?.RunOnUiThread(a);
            }
        }




        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            
            switch(item.ItemId)
            {
                case Resource.Id.action_search_target:
                    if(SearchTabHelper.SearchTarget==SearchTarget.Wishlist)
                    {
                        Toast.MakeText(this.Context, Resource.String.wishlist_tab_target, ToastLength.Long).Show();
                        return true;
                    }
                    ShowChangeTargetDialog();
                    return true;
                case Resource.Id.action_search:
                    if(SearchTabHelper.CurrentlySearching) //that means the user hit the "X" button
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
                        if(editText == null)
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
            if(SearchTabHelper.LastSearchTerm==string.Empty || SearchTabHelper.LastSearchTerm == null)
            {
                Toast.MakeText(this.Context,Resource.String.perform_search_first, ToastLength.Long).Show();
                return;
            }
            SearchTabHelper.AddWishlistSearchTabFromCurrent();
            Toast.MakeText(this.Context, string.Format(this.Context.GetString(Resource.String.added_to_wishlist),SearchTabHelper.LastSearchTerm), ToastLength.Long).Show();
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
            if(SoulSeekState.ActiveActivityRef is MainActivity)
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

        public static void UpdateDrawableState(EditText actv)
        {
            if(actv.Text==string.Empty || actv.Text == null)
            {
                actv.SetCompoundDrawables(null, null, null, null);
            }
            else
            {
                var cancel = ContextCompat.GetDrawable(SoulSeekState.MainActivityRef,Resource.Drawable.ic_cancel_black_24dp);
                cancel.SetBounds(0, 0, cancel.IntrinsicWidth, cancel.IntrinsicHeight);
                actv.SetCompoundDrawables(null,null,cancel,null);
            }
        }
        

        public static void ConfigureSupportCustomView(View customView/*, string initSearchText*/)
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
            catch(System.ArgumentException e)
            {
                MainActivity.LogFirebase("ArugmentException Value does not fall within range: " + SearchingText + " " + e.Message);
            }
            catch(System.Exception e)
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
            if(searchHistory==null||searchHistory.Count==0) // i think we just have to deserialize once??
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

            actv.Adapter = new ArrayAdapter<string>(SoulSeekState.ActiveActivityRef, Resource.Layout.autoSuggestionRow, searchHistory);
            actv.KeyPress -= Actv_KeyPressHELPER;
            actv.KeyPress += Actv_KeyPressHELPER;
            actv.FocusChange += MainActivity_FocusChange;
            actv.TextChanged += Actv_TextChanged;

            SetCustomViewTabNumberInner(iv, SoulSeekState.ActiveActivityRef);
        }

        private static void Actv_Touch(object sender, View.TouchEventArgs e)
        {
            EditText editText = sender as EditText;
            e.Handled = false;
            if(e.Event.GetX() >= (editText.Width - editText.TotalPaddingRight))
            {
                if(e.Event.Action == MotionEventActions.Up)
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
            if(SearchingText == string.Empty && e.Text.ToString() != string.Empty)
            {
                UpdateDrawableState(sender as EditText);
            }
            else if(SearchingText != string.Empty && e.Text.ToString() == string.Empty)
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
            ListView lv = rootView.FindViewById<ListView>(Resource.Id.listView1);

            if (SearchTabHelper.FilteredResults)
            {
                SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.FilteredResponses);
                lv.Adapter = (customAdapter);
            }
            else
            {
                SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.SearchResponses);
                lv.Adapter = (customAdapter);
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
                View rootView = inflater.Inflate(Resource.Layout.search_results_expandablexml,container);
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
                if(prev!= SearchFragment.SearchResultStyle)
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
                if(f==null)
                {
                    MainActivity.LogInfoFirebase("search fragment not on activities fragment manager");
                }
                else if(!f.IsAdded)
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is not added");
                }
                else
                {
                    MainActivity.LogInfoFirebase("search fragment from activities fragment manager is good, setting it");
                    //SearchFragment.Instance = f; Todo add back...
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

            ListView lv = rootView.FindViewById<ListView>(Resource.Id.listView1);

            RelativeLayout rel = rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
            BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
            bsb.Hideable = true;
            bsb.PeekHeight = 320;
            bsb.State = BottomSheetBehavior.StateHidden;

            CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
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

            Button clearFilter = rootView.FindViewById<Button>(Resource.Id.clearFilter);
            clearFilter.Click += ClearFilter_Click;

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
            if (SearchTabHelper.FilteredResults)
            {
                SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.FilteredResponses);
                lv.Adapter = (customAdapter);
            }
            else
            {
                SearchAdapter customAdapter = new SearchAdapter(Context, SearchTabHelper.SearchResponses);
                lv.Adapter = (customAdapter);
            }
            lv.ItemClick -= Lv_ItemClick;
            lv.ItemClick += Lv_ItemClick;
            lv.Clickable = true;
            lv.Focusable = true;
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
            if(FilterSticky)
            {
                filterText.Text = FilterStickyString;
            }

            return rootView;
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

        private void ClearFilter_Click(object sender, EventArgs e)
        {
            CheckBox filterSticky = rootView.FindViewById<CheckBox>(Resource.Id.stickyFilterCheckbox);
            filterSticky.Checked = false;
            ClearFilterStringAndCached(true);
        }

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

                if (test.IsFocused && (this.Resources.Configuration.HardKeyboardHidden==Android.Content.Res.HardKeyboardHidden.Yes)) //it can still be focused without the keyboard up...
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
            if(FilterSticky)
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
                    if(searchHere!=null)
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

        public static void SetSearchHintTarget(SearchTarget target, AutoCompleteTextView actv=null)
        {
            if(actv==null)
            {
                actv = SoulSeekState.MainActivityRef?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
            }
            if(actv!=null)
            {
                switch(target)
                {
                    case SearchTarget.AllUsers:
                        actv.Hint = SoulSeekState.MainActivityRef.GetString(Resource.String.search_here);
                        break;
                    case SearchTarget.UserList:
                        actv.Hint = SoulSeekState.MainActivityRef.GetString(Resource.String.saerch_user_list);
                        break;
                    case SearchTarget.Room:
                        actv.Hint = string.Format(SoulSeekState.MainActivityRef.GetString(Resource.String.search_room_),SearchTabHelper.SearchTargetChosenRoom);
                        break;
                    case SearchTarget.ChosenUser:
                        actv.Hint = string.Format(SoulSeekState.MainActivityRef.GetString(Resource.String.search_user_),SearchTabHelper.SearchTargetChosenUser); 
                        break;
                    case SearchTarget.Wishlist:
                        actv.Hint = SoulSeekState.MainActivityRef.GetString(Resource.String.wishlist_search);
                        break;
                }
            }
        }

        public Android.Graphics.Drawables.TransitionDrawable GetTransitionDrawable()
        {
            Android.Graphics.Drawables.TransitionDrawable icon = ActionBarMenu?.FindItem(Resource.Id.action_search)?.Icon as Android.Graphics.Drawables.TransitionDrawable;
            //tested this and it works well
            if(icon==null)
            {
                if(this.Activity == null)
                {
                    MainActivity.LogInfoFirebase("GetTransitionDrawable activity is null");
                    SearchFragment f = GetSearchFragment();
                    if(f==null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable no search fragment attached to activity");
                    }
                    else if(!f.IsAdded)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable attached but not added");
                    }
                    else if(f.Activity==null)
                    {
                        MainActivity.LogInfoFirebase("GetTransitionDrawable f.Activity activity is null");
                    }
                    else
                    {
                        MainActivity.LogInfoFirebase("we should be using the fragment manager one...");
                    }
                }
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
            if(SearchTabHelper.SearchTargetChosenRoom == string.Empty)
            {
                s.SetSelection(0);
            }
            else
            {
                bool found = false;
                for (int i=0;i<s.Adapter.Count; i++)
                {
                    if((string)(s.GetItemAtPosition(i)) == SearchTabHelper.SearchTargetChosenRoom)
                    {
                        found = true;
                        s.SetSelection(i);
                        custom.Text = string.Empty;
                        break;
                    }
                }
                if(!found)
                {
                    s.SetSelection(s.Adapter.Count - 1);
                    custom.Text = SearchTabHelper.SearchTargetChosenRoom;
                }
            }
        }

        private EditText chooseUserInput = null;
        private EditText customRoomName = null;
        private Spinner roomListSpinner = null;
        private LinearLayout targetRoomLayout = null;
        public void ShowChangeTargetDialog()
        {
            Context toUse = this.Activity != null ? this.Activity : SoulSeekState.MainActivityRef;
            AndroidX.AppCompat.App.AlertDialog.Builder builder = new AndroidX.AppCompat.App.AlertDialog.Builder(toUse); //used to be our cached main activity ref...
            builder.SetTitle(Resource.String.search_target_);
            View viewInflated = LayoutInflater.From(toUse).Inflate(Resource.Layout.changeusertarget, this.rootView as ViewGroup, false);
            chooseUserInput = viewInflated.FindViewById<EditText>(Resource.Id.chosenUserInput);
            customRoomName = viewInflated.FindViewById<EditText>(Resource.Id.customRoomName);
            targetRoomLayout = viewInflated.FindViewById<LinearLayout>(Resource.Id.targetRoomLayout);
            roomListSpinner = viewInflated.FindViewById<Spinner>(Resource.Id.roomListSpinner);

            AndroidX.AppCompat.Widget.AppCompatRadioButton allUsers = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.allUsers);
            AndroidX.AppCompat.Widget.AppCompatRadioButton chosenUser = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.chosenUser);
            AndroidX.AppCompat.Widget.AppCompatRadioButton userList = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetUserList);
            AndroidX.AppCompat.Widget.AppCompatRadioButton room = viewInflated.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetRoom);
            List<string> possibleRooms = new List<string>();
            if(ChatroomController.JoinedRoomNames!=null && ChatroomController.JoinedRoomNames.Count!=0)
            {
                possibleRooms = ChatroomController.JoinedRoomNames.ToList();
            }
            possibleRooms.Add(SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_));
            roomListSpinner.Adapter = new ArrayAdapter<string>(SoulSeekState.ActiveActivityRef,Resource.Layout.support_simple_spinner_dropdown_item,possibleRooms.ToArray());
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
                    if (roomListSpinner.SelectedItem.ToString()==SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_))
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
                if(sender is AndroidX.AppCompat.App.AlertDialog aDiag)
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
            if(roomListSpinner.Adapter.Count - 1 == e.Position)
            {
                customRoomName.Visibility = ViewStates.Visible;
                if (first)
                {
                    first =false;
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
            if(roomListSpinner.SelectedItem.ToString() == SoulSeekState.ActiveActivityRef.GetString(Resource.String.custom_))
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

        public static void ParseFilterString(SearchTab searchTab)
        {
            List<string> filterStringSplit = searchTab.FilterString.Split(' ').ToList();
            searchTab.WordsToAvoid.Clear();
            searchTab.WordsToInclude.Clear();
            searchTab.FilterSpecialFlags.Clear();
            foreach (string word in filterStringSplit)
            {
                if(word.Contains("mbr:")||word.Contains("minbitrate:"))
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
                else if(word.Contains("mfs:")||word.Contains("minfilesize:"))
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
                else if(word.Contains("mfif:") || word.Contains("minfilesinfolder:"))
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    try
                    {
                        searchTab.FilterSpecialFlags.MinFoldersInFile =  Integer.ParseInt(word.Split(':')[1]);
                    }
                    catch(System.Exception)
                    {

                    }
                }
                else if(word=="isvbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsVBR = true;
                }
                else if(word=="iscbr")
                {
                    searchTab.FilterSpecialFlags.ContainsSpecialFlags = true;
                    searchTab.FilterSpecialFlags.IsCBR = true;
                }
                else if (word.StartsWith('-'))
                {
                    searchTab.WordsToAvoid.Add(word);
                }
                else
                {
                    searchTab.WordsToInclude.Add(word);
                }
            }
        }

        private bool MatchesCriteria(SearchResponse s)
        {
            foreach (File f in s.Files)
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

            searchTab.FilteredResponses = searchTab.SearchResponses.FindAll(new Predicate<SearchResponse>(
            (SearchResponse s) =>
            {
                if(!MatchesCriteria(s))
                {
                    return false;
                }
                else
                {   //so it matches the word criteria.  now lets see if it matches the flags if any...
                    if(!searchTab.FilterSpecialFlags.ContainsSpecialFlags)
                    {
                        return true;
                    }
                    else
                    {
                        //we need to make sure this also matches our special flags
                        if(searchTab.FilterSpecialFlags.MinFoldersInFile!=0)
                        {
                            if(searchTab.FilterSpecialFlags.MinFoldersInFile>s.Files.Count)
                            {
                                return false;
                            }
                        }
                        if(searchTab.FilterSpecialFlags.MinFileSizeMB!=0)
                        {
                            bool match = false;
                            foreach(Soulseek.File f in s.Files)
                            {
                                int mb = (int)(f.Size)/(1024*1024);
                                if(mb> searchTab.FilterSpecialFlags.MinFileSizeMB)
                                {
                                    match = true;
                                }
                            }
                            if(!match)
                            {
                                return false;
                            }
                        }
                        if(searchTab.FilterSpecialFlags.MinBitRateKBS!=0)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.Files)
                            {
                                if(f.BitRate==null || !(f.BitRate.HasValue))
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
                        if(searchTab.FilterSpecialFlags.IsCBR)
                        {
                            bool match = false;
                            foreach (Soulseek.File f in s.Files)
                            {
                                if (f.IsVariableBitRate==false)//this is bool? can have no value...
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
                            foreach (Soulseek.File f in s.Files)
                            {
                                if (f.IsVariableBitRate==true)
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
            }));
        }

        private void FilterText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            MainActivity.LogDebug("Text Changed: " + e.Text);
            if (e.Text != null && e.Text.ToString() != string.Empty && SearchTabHelper.SearchResponses != null)
            {
                SearchTabHelper.FilteredResults = true;
                SearchTabHelper.FilterString = e.Text.ToString();
                if(FilterSticky)
                {
                    FilterStickyString = SearchTabHelper.FilterString;
                }
                ParseFilterString(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab]);
                SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.FilteredResponses);
                ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                lv.Adapter = (customAdapter);
            }
            else
            {
                SearchTabHelper.FilteredResults = false;
                SearchAdapter customAdapter = new SearchAdapter(context, SearchTabHelper.SearchResponses);
                ListView lv = this.rootView.FindViewById<ListView>(Resource.Id.listView1);
                lv.Adapter = (customAdapter);
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
            if(FilterSticky)
            {
                editor.PutBoolean(SoulSeekState.M_FilterSticky, FilterSticky);
                editor.PutString(SoulSeekState.M_FilterStickyString, SearchTabHelper.FilterString);
            }
            editor.PutInt(SoulSeekState.M_SearchResultStyle,(int)SearchResultStyle);
            editor.Commit();
            }
        }

        private static void Actv_KeyPressHELPER(object sender, View.KeyEventArgs e)
        {
            SearchFragment.Instance.Actv_KeyPress(sender, e);
        }

        private void Actv_KeyPress(object sender, View.KeyEventArgs e)
        {
            if (e.KeyCode == Keycode.Enter)
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
                (sender as AutoCompleteTextView).DismissDropDown();
                MainActivity.LogDebug("Enter Pressed..");
            }
            else if(e.KeyCode == Keycode.Del && e.Event.Action == KeyEventActions.Down)
            {
                (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode, e.Event);
                return;
            }
            else if(e.Event.Action == KeyEventActions.Down && (e.KeyCode == Android.Views.Keycode.VolumeUp || e.KeyCode == Android.Views.Keycode.VolumeDown))
            {
                //for some reason e.Handled is always true coming in.  also only on down volume press does anything.
                e.Handled = false;
            }
            else //this will only occur on unhandled keys which on the softkeyboard probably has to be in the above two categories....
            {
                if(e.Event.Action == KeyEventActions.Down)
                {
                    //MainActivity.LogDebug(e.KeyCode.ToString()); //happens on HW keyboard... event does NOT get called on SW keyboard. :)
                    //MainActivity.LogDebug((sender as AutoCompleteTextView).IsFocused.ToString());
                    (sender as AutoCompleteTextView).OnKeyDown(e.KeyCode,e.Event);
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
            if (e.Response.Files.Count == 0 && SoulSeekState.HideLockedResults)
            {
                MainActivity.LogDebug("Skipping Locked");
                return;
            }
            //MainActivity.LogDebug("SEARCH RESPONSE RECEIVED");
            refreshListView(e.Response, fromTab, fromWishlist);
            //SoulSeekState.MainActivityRef.RunOnUiThread(action);

        }

        private static void clearListView(bool fromWishlist)
        {
            if(fromWishlist)
            {
                return; //we combine results...
            }


            MainActivity.LogDebug("clearListView SearchResponses.Clear()");
            SearchTabHelper.SortHelper.Clear();
            SearchTabHelper.SearchResponses.Clear();
            SearchTabHelper.LastSearchResponseCount = -1;
            SearchTabHelper.FilteredResponses.Clear();
            if (!fromWishlist)
            {
                SearchFragment.Instance.ClearFilterStringAndCached();
                SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchResponses);
                ListView lv = SearchFragment.Instance.rootView.FindViewById<ListView>(Resource.Id.listView1);
                lv.Adapter = (customAdapter);
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
                Dictionary<string, List<File>> folderFilePairs = new Dictionary<string, List<File>>();
                if (origResponse.Files.Count != 0)
                {
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
                    if (folderFilePairs.Keys.Count > 1)
                    {
                        //split them
                        List<SearchResponse> splitSearchResponses = new List<SearchResponse>();
                        foreach (var pair in folderFilePairs)
                        {
                            splitSearchResponses.Add(new SearchResponse(origResponse.Username, origResponse.Token, origResponse.FreeUploadSlots, origResponse.UploadSpeed, origResponse.QueueLength, pair.Value));
                        }
                        //MainActivity.LogDebug("User: " + origResponse.Username + " got split into " + folderFilePairs.Keys.Count);
                        return new Tuple<bool, List<SearchResponse>>(true, splitSearchResponses);
                    }
                    else
                    {
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
            lock(SearchTabHelper.SortHelper)
            {
                Tuple<bool, List<SearchResponse>> splitResponses = SplitMultiDirResponse(resp);
                try
                {

                    if (splitResponses.Item1)
                    { //we have multiple to add
                        foreach (SearchResponse splitResponse in splitResponses.Item2)
                        {
                            if(fromWishlist && SearchTabHelper.SearchTabCollection[fromTab].SortHelper.ContainsKey(splitResponse))
                            {
                                continue;
                            }
                            SearchTabHelper.SearchTabCollection[fromTab].SortHelper.Add(splitResponse, null);
                        }
                    }
                    else
                    {
                        if (fromWishlist && SearchTabHelper.SearchTabCollection[fromTab].SortHelper.ContainsKey(resp))
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
            //}
            //else //as an optimization, we do not need to sort, we will sort everything when we go back to the tab.
            //{
            //    Tuple<bool, List<SearchResponse>> splitResponses = SplitMultiDirResponse(resp);
            //    if (splitResponses.Item1)
            //    { //we have multiple to add
            //        foreach (SearchResponse splitResponse in splitResponses.Item2)
            //        {
            //            SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Add(splitResponse);
            //        }
            //    }
            //    else
            //    {
            //        SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Add(resp);
            //    }
            //}


            //only do fromWishlist if SearchFragment.Instance is not null...

            if ((!fromWishlist || SearchFragment.Instance != null) && fromTab==SearchTabHelper.CurrentTab)
            {
                Action a = new Action(() => {
                    //SearchResponses.Add(resp);
                    //MainActivity.LogDebug("UI - SEARCH RESPONSE RECEIVED");
                    if(fromTab != SearchTabHelper.CurrentTab)
                    {
                        return;
                    }
                    if(SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount == SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count)
                    {
                        MainActivity.LogDebug("already did it..");
                        //we already updated for this one.
                        //the UI marshelled calls are delayed.  as a result there will be many all coming in with the final search response count of say 751.  
                        return;
                    }

                    MainActivity.LogDebug("refreshListView SearchResponses.Count = " + SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count);

                    if (SearchTabHelper.SearchTabCollection[fromTab].FilteredResults)
                    {
                        SearchFragment.Instance.UpdateFilteredResponses(SearchTabHelper.SearchTabCollection[fromTab]);  //WE JUST NEED TO FILTER THE NEW RESPONSES!!
                        SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].FilteredResponses);
                        ListView lv = SearchFragment.Instance.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        lv.Adapter = (customAdapter);
                    }
                    else
                    {
                        SearchAdapter customAdapter = new SearchAdapter(SearchFragment.Instance.context, SearchTabHelper.SearchTabCollection[fromTab].SearchResponses);
                        ListView lv = SearchFragment.Instance.rootView.FindViewById<ListView>(Resource.Id.listView1);
                        lv.Adapter = (customAdapter);
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResponseCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;
                });
                SoulSeekState.MainActivityRef?.RunOnUiThread(a);
            }

        }

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
                    dlDiagResp = SearchTabHelper.FilteredResponses.ElementAt<SearchResponse>(pos);
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
                    msg = "Filtered.Count " + SearchTabHelper.FilteredResponses.Count.ToString() + " position selected = " + pos.ToString();
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
            h.PostDelayed(new Action(() => {
                var menuItem = ActionBarMenu?.FindItem(Resource.Id.action_search);
                if(menuItem!=null)
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
                if(!fromWishlist)
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
            catch(System.Exception e)
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

            Action<SearchResponseReceivedEventArgs> searchResponseReceived = new Action<SearchResponseReceivedEventArgs>((SearchResponseReceivedEventArgs e)=>{
                SoulseekClient_SearchResponseReceived(null, e, fromTab, fromWishlist);
                });

            SearchOptions searchOptions = new SearchOptions(responseLimit: SoulSeekState.NumberSearchResults, searchTimeout: searchTimeout, maximumPeerQueueLength: int.MaxValue, minimumPeerFreeUploadSlots: SoulSeekState.FreeUploadSlotsOnly ? 1 : 0, responseReceived: searchResponseReceived);
            SearchScope scope = null;
            if(fromWishlist)
            {
                scope = new SearchScope(SearchScopeType.Wishlist); //this is the same as passing no option for search scope 
            }
            else if (SearchTabHelper.SearchTarget == SearchTarget.AllUsers || SearchTabHelper.SearchTarget == SearchTarget.Wishlist) //this is like a manual wishlist search...
            {
                scope = new SearchScope(SearchScopeType.Network); //this is the same as passing no option for search scope
            }
            else if(SearchTabHelper.SearchTarget == SearchTarget.UserList)
            {
                if(SoulSeekState.UserList==null||SoulSeekState.UserList.Count==0)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.user_list_empty, ToastLength.Short).Show();
                    }
                    ));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, SoulSeekState.UserList.Select(item=>item.Username).ToArray());
            }
            else if(SearchTabHelper.SearchTarget == SearchTarget.ChosenUser)
            {
                if(SearchTabHelper.SearchTargetChosenUser == string.Empty)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_user, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.User, new string[] { SearchTabHelper.SearchTargetChosenUser });
            }
            else if(SearchTabHelper.SearchTarget == SearchTarget.Room)
            {
                if (SearchTabHelper.SearchTargetChosenRoom == string.Empty)
                {
                    SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                        Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_room, ToastLength.Short).Show();
                    }));
                    return;
                }
                scope = new SearchScope(SearchScopeType.Room, new string[] { SearchTabHelper.SearchTargetChosenRoom });
            }
            try
            {
                Task<IReadOnlyCollection<SearchResponse>> t = null;

                t = SoulSeekState.SoulseekClient.SearchAsync(SearchQuery.FromText(searchString), options: searchOptions, scope:scope, cancellationToken:cancellationToken);
                //drawable.StartTransition() - since if we get here, the search is launched and the continue with will always happen...

                t.ContinueWith(new Action<Task<IReadOnlyCollection<SearchResponse>>>( (Task<IReadOnlyCollection<SearchResponse>> t)=>
                {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;

                    if(!t.IsCompletedSuccessfully && t.Exception!=null)
                    {
                        MainActivity.LogDebug("search exception: " + t.Exception.Message);
                    }

                    if(t.IsCanceled)
                    {
                        //then the user pressed the button so we dont need to change it back...
                    }
                    else
                    {

                        SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                            try
                            {
                                if(fromTab==SearchTabHelper.CurrentTab && !fromWishlist)
                                {
                                    MainActivity.LogDebug("transitionDrawable: ReverseTransition transition");
                                    transitionDrawable.ReverseTransition(SearchToCloseDuration);
                                    SearchFragment.Instance.PerformBackUpRefresh();


                                }
                            }
                            catch(System.ObjectDisposedException e)
                            { 
                                //since its disposed when you go back to the screen it will be the correct search icon again..
                                //noop
                            }
                        }));
                        
                    }
                    if(t.Result.Count==0 && !fromWishlist)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.no_search_results, ToastLength.Short).Show();
                        }));
                    }
                    SearchTabHelper.SearchTabCollection[fromTab].LastSearchResultsCount = SearchTabHelper.SearchTabCollection[fromTab].SearchResponses.Count;

                    if(fromWishlist)
                    {
                        WishlistController.SearchCompleted(fromTab);
                    }
                    else if(SearchTabHelper.SearchTabCollection[fromTab].SearchTarget == SearchTarget.Wishlist)
                    {
                        SearchTabHelper.SaveStateToSharedPreferences();
                    }
                }));



                if(SearchTabHelper.FilteredResults && FilterSticky && !fromWishlist)
                {
                    //remind the user that the filter is ON.
                    t.ContinueWith(new Action<Task>(
                        (Task t) =>
                        {
                            SoulSeekState.MainActivityRef.RunOnUiThread(new Action(()=>{

                                RelativeLayout rel = SearchFragment.Instance.rootView.FindViewById<RelativeLayout>(Resource.Id.bottomSheet);
                                BottomSheetBehavior bsb = BottomSheetBehavior.From(rel);
                                if(bsb.State == BottomSheetBehavior.StateHidden)
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
                                setTotal.PlaySequentially(set1,set2);
                                setTotal.Start();

                                }
                                
                                }));
                        }
                        ));
                }
            }
            catch (ArgumentNullException ane)
            {
                SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                    string errorMsg = SoulSeekState.MainActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SoulSeekState.MainActivityRef.GetString(Resource.String.no_wish_text);
                    }

                Toast.MakeText(SoulSeekState.MainActivityRef, errorMsg, ToastLength.Short).Show();
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    if(!fromWishlist && fromTab == SearchTabHelper.CurrentTab)
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
                SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                    SearchTabHelper.SearchTabCollection[fromTab].CurrentlySearching = false;
                    string errorMsg = SoulSeekState.MainActivityRef.GetString(Resource.String.no_search_text);
                    if (fromWishlist)
                    {
                        errorMsg = SoulSeekState.MainActivityRef.GetString(Resource.String.no_wish_text);
                    }
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    Toast.MakeText(SoulSeekState.MainActivityRef, errorMsg, ToastLength.Short).Show();
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
                //MainActivity.LogFirebase(new Java.Lang.Throwable(ue.Message + "searchclick_GENERALEXCEPTION"));
                SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
                    Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.search_error_unspecified, ToastLength.Short).Show();
                }));
                MainActivity.LogFirebase("tabpageradapter searchclick: "+ ue.Message);
                return;
            }
            if(!fromWishlist)
            {
                //add a new item to our search history
                if (SoulSeekState.RememberSearchHistory)
                {
                    if(!searchHistory.Contains(searchString))
                    {
                        searchHistory.Add(searchString);
                    }
                }
                var actv = SoulSeekState.MainActivityRef.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere); // lot of nullrefs with actv before this change....
                if(actv==null)
                {
                    actv = (SearchFragment.Instance.Activity as Android.Support.V7.App.AppCompatActivity)?.SupportActionBar?.CustomView?.FindViewById<AutoCompleteTextView>(Resource.Id.searchHere);
                    if(actv==null)
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
            if(!fromWishlist)
            {
                //try to clearFocus on the search if you can (gets rid of blinking cursor)
                ClearFocusSearchEditText();
                MainActivity.LogDebug("Search_Click");
            }
            
            if (!SoulSeekState.currentlyLoggedIn)
            {
                if (!fromWishlist)
                {
                    Toast tst = Toast.MakeText(SearchFragment.Instance.context, Resource.String.must_be_logged_to_search, ToastLength.Long);
                    tst.Show();
                    MainActivity.LogDebug("transitionDrawable: RESET transition");
                    transitionDrawable.ResetTransition();
                }
                else
                {
                    SearchTabHelper.CurrentlySearching = false;
                    return;
                }
            }
            else if (MainActivity.CurrentlyLoggedInButDisconnectedState())
            {
                if(fromWishlist)
                {
                    return;
                }
                Task t;
                if (!MainActivity.ShowMessageAndCreateReconnectTask(SearchFragment.Instance.context, out t))
                {
                    return;
                }
                t.ContinueWith(new Action<Task>((Task t) =>
                {
                    if (t.IsFaulted)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(() =>
                        {

                            Toast.MakeText(SoulSeekState.MainActivityRef, Resource.String.failed_to_connect, ToastLength.Short).Show();

                        });
                        return;
                    }
                    SoulSeekState.MainActivityRef.RunOnUiThread(()=>{SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist); });

                }));
            }
            else
            {
                SearchLogic(cancellationToken, transitionDrawable, searchString, fromTab, fromWishlist);
            }
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
            if(position==-1) //in my case this happens if you delete too fast...
            {
                return;
            }
            int tabToRemove = localDataSet[position];
            bool isWishlist = (SearchTabHelper.SearchTabCollection[tabToRemove].SearchTarget == SearchTarget.Wishlist);
            SearchTabHelper.SearchTabCollection[tabToRemove].CancellationTokenSource?.Cancel();
            if(isWishlist)
            {
                if (tabToRemove == SearchTabHelper.CurrentTab)
                {
                    //remove it for real
                    SearchTabHelper.SearchTabCollection.Remove(tabToRemove, out _);
                    localDataSet.RemoveAt(position);
                    SearchTabDialog.Instance.recycleWishesAdapter.NotifyItemRemoved(position);


                    //go to search tab instead (there is always one)
                    string listOfKeys2 = System.String.Join(",",SearchTabHelper.SearchTabCollection.Keys);
                    MainActivity.LogInfoFirebase("list of Keys: " + listOfKeys2);
                    int tabToGoTo = SearchTabHelper.SearchTabCollection.Keys.Where(key=>key>=0).First();
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
                if(tabToRemove == SearchTabHelper.CurrentTab)
                {
                    SearchTabHelper.SearchTabCollection[tabToRemove] = new SearchTab(); //clear it..
                    SearchFragment.Instance.GoToTab(tabToRemove, true);
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
            if(isWishlist)
            {
                SearchTabHelper.SaveStateToSharedPreferences();
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
            if(searchTab.SearchTarget == SearchTarget.Wishlist)
            {
                string timeString = "-";
                if(searchTab.LastRanTime != DateTime.MinValue)
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
            if (lastTerm!=string.Empty && lastTerm != null)
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


    public class SearchTabDialog : Android.Support.V4.App.DialogFragment
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
            if(wishTabIds.Count==0)
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
            newSearch.SetCompoundDrawablesWithIntrinsicBounds(drawable,null,null,null);

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

            Window window = Dialog.Window;//  getDialog().getWindow();
            Point size = new Point();

            Display display = window.WindowManager.DefaultDisplay;
            display.GetSize(size);

            int width = size.X;

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
    //            if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
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

    public class SearchAdapter : ArrayAdapter<SearchResponse>
    {
        List<int> oppositePositions= new List<int>();
        public SearchAdapter(Context c, List<SearchResponse> items) : base(c, 0, items)
        {
            oppositePositions = new List<int>();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ISearchItemViewBase itemView = (ISearchItemViewBase)convertView;
            if (null == itemView)
            {
                switch(SearchFragment.SearchResultStyle)
                {
                    case SearchResultStyleEnum.ExpandedAll:
                    case SearchResultStyleEnum.CollapsedAll:
                        itemView = SearchItemViewExpandable.inflate(parent);
                        (itemView as View).FindViewById<ImageView>(Resource.Id.expandableClick).Click += CustomAdapter_Click;
                        (itemView as View).FindViewById<LinearLayout>(Resource.Id.relativeLayout1).Click += CustomAdapter_Click1;
                        break;
                    case SearchResultStyleEnum.Medium:
                        itemView = SearchItemViewMedium.inflate(parent);
                        break;
                    case SearchResultStyleEnum.Minimal:
                        itemView = SearchItemViewMinimal.inflate(parent);
                        break;
                }
            }
            bool opposite = oppositePositions.Contains(position);
            itemView.setItem(GetItem(position), opposite); //this will do the right thing no matter what...


            //if(SearchFragment.SearchResultStyle==SearchResultStyleEnum.CollapsedAll)
            //{
            //    (itemView as IExpandable).Collapse();
            //}
            //else if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll)
            //{
            //    (itemView as IExpandable).Expand();
            //}

            //SETTING TOOLTIPTEXT does not allow list view item click!!! 
            //itemView.TooltipText = "Queue Length: " + GetItem(position).QueueLength + System.Environment.NewLine + "Free Upload Slots: " + GetItem(position).FreeUploadSlots;
            return itemView as View;
            //return base.GetView(position, convertView, parent);
        }

        private void CustomAdapter_Click1(object sender, EventArgs e)
        {
            MainActivity.LogInfoFirebase("CustomAdapter_Click1");
            int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
            SearchFragment.Instance.showEditDialog(position);
        }

        private void CustomAdapter_Click(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            int position = ((sender as View).Parent.Parent.Parent as ListView).GetPositionForView((sender as View).Parent.Parent as View);
            var v = ((sender as View).Parent.Parent as View).FindViewById<View>(Resource.Id.detailsExpandable);
            var img = ((sender as View).Parent.Parent as View).FindViewById<ImageView>(Resource.Id.expandableClick);
            if(v.Visibility == ViewStates.Gone)
            {
                img.Animate().RotationBy((float)(180.0)).SetDuration(350).Start();
                v.Visibility = ViewStates.Visible;
                SearchItemViewExpandable.PopulateFilesListView(v as LinearLayout, GetItem(position));
                if(SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll)
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

    public interface ISearchItemViewBase
    {
        void setItem(SearchResponse item, bool opposite);
    }

    public class SearchItemViewMinimal : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        //private TextView viewQueue;
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

        private void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textView1);
            viewFoldername = FindViewById<TextView>(Resource.Id.textView2);
            viewSpeed = FindViewById<TextView>(Resource.Id.textView3);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        public void setItem(SearchResponse item, bool noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameFromFile(GetFileName(item));
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString(); //kb/s

            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }

        private string GetFileName(SearchResponse item)
        {
            try
            {
                if (item.Files.Count == 0)
                {
                    return "\\Locked\\";
                }
                File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }
    }

    public class SearchItemViewMedium : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewQueue;
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

        private void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
        }

        public void setItem(SearchResponse item, bool noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameFromFile(GetFileName(item));
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SeekerApplication.STRINGS_KBS; //kbs
            viewFileType.Text = Helpers.GetDominantFileType(item);
            if(item.FreeUploadSlots>0)
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

        private string GetFileName(SearchResponse item)
        {
            try
            {
                if (item.Files.Count == 0)
                {
                    return "\\Locked\\";
                }
                File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }
    }

    public interface IExpandable
    {
        void Expand();
        void Collapse();
    }

    public class ExpandableSearchItemFilesAdapter : ArrayAdapter<Soulseek.File>
    {
        public ExpandableSearchItemFilesAdapter(Context c, List<Soulseek.File> files) : base(c,0,files.ToArray())
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

    public class SearchItemViewExpandable: RelativeLayout, ISearchItemViewBase, IExpandable
    {
        private TextView viewQueue;
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private ImageView imageViewExpandable;
        private LinearLayout viewToHideShow;
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

        private void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
        }

        public static void PopulateFilesListView(LinearLayout viewToHideShow, SearchResponse item)
        {
            viewToHideShow.RemoveAllViews();
            foreach(Soulseek.File f in item.Files)
            {
                TextView tv = new TextView(SoulSeekState.MainActivityRef);
                SetTextColor(tv,SoulSeekState.MainActivityRef);
                tv.Text = Helpers.GetFileNameFromFile(f.Filename);
                viewToHideShow.AddView(tv);
            }
        }

        public void setItem(SearchResponse item, bool opposite)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = Helpers.GetFolderNameFromFile(GetFileName(item));
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + "kbs"; //kb/s
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            viewFileType.Text = Helpers.GetDominantFileType(item);

            if(SearchFragment.SearchResultStyle==SearchResultStyleEnum.CollapsedAll && opposite ||
                SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll && !opposite)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                PopulateFilesListView(viewToHideShow,item);
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

        private string GetFileName(SearchResponse item)
        {
            try
            {
                if (item.Files.Count == 0)
                {
                    return "\\Locked\\";
                }
                File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
        }


        public static Color GetColorFromInteger(int color)
        {
            return Color.Rgb(Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static void SetTextColor(TextView tv, Context c)
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 23)
            {
                tv.SetTextColor(GetColorFromInteger(ContextCompat.GetColor(c, Resource.Color.cellTextColor)));
            }
            else
            {
                tv.SetTextColor(c.Resources.GetColor(Resource.Color.cellTextColor));
            }
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

    public class TransferItem
    {
        public string Filename;
        public string Username;
        public string Foldername;
        public string FullFilename;
        public int Progress;
        public TimeSpan? RemainingTime;
        public bool Failed;
        public TransferStates State;
        public long Size;
        public bool Queued = false;
        private int queuelength = 0;
        public int QueueLength
        {
            get
            {
                return queuelength;
            }
            set
            {
                queuelength = value;
                if (value==0)
                {
                    Queued = false;
                }
                else
                {
                    Queued = true;
                }
            }
        }
        public string FinalUri = string.Empty; //final uri of downloaded item
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public CancellationTokenSource CancellationTokenSource = null;
    }

    public interface ITransferItemView
    {
        public TransferItem InnerTransferItem { get; set; }

        public void setupChildren();

        public void setItem(TransferItem ti);

        public TransfersFragment.TransferViewHolder ViewHolder { get;set;}

        public ProgressBar progressBar { get;set;}

        public void SetAdditionalStatusText(TransferItem ti);

    }

    public class TransferItemViewDetails : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewFilename;

        private TextView viewStatus; //In Queue, Failed, Done, In Progress
        private TextView viewStatusAdditionalInfo; //if in Queue then show position, if In Progress show time remaining.

        public TransferItem InnerTransferItem { get; set; }
        //private TextView viewQueue;
        public ProgressBar progressBar {get;set; }

        public TransferItemViewDetails(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            setupChildren();
        }
        public TransferItemViewDetails(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_item_detailed, this, true);
            setupChildren();
        }

        public static TransferItemViewDetails inflate(ViewGroup parent)
        {
            TransferItemViewDetails itemView = (TransferItemViewDetails)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_item_details_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textViewUser);
            viewFilename = FindViewById<TextView>(Resource.Id.textViewFileName);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);

            viewStatus = FindViewById<TextView>(Resource.Id.textViewStatus);
            viewStatusAdditionalInfo = FindViewById<TextView>(Resource.Id.textViewStatusAdditionalInfo);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        private void SetViewStatusText(TransferItem item)
        {
            if(item.State.HasFlag(TransferStates.Queued))
            {
                viewStatus.SetText(Resource.String.in_queue);
            }
            else if(item.State.HasFlag(TransferStates.Cancelled))
            {
                viewStatus.SetText(Resource.String.paused);
            }
            else if(item.State.HasFlag(TransferStates.Rejected)|| item.State.HasFlag(TransferStates.TimedOut)|| item.State.HasFlag(TransferStates.Errored))
            {
                viewStatus.SetText(Resource.String.failed);
            }
            else if(item.State.HasFlag(TransferStates.Initializing)|| item.State.HasFlag(TransferStates.Requested))  //item.State.HasFlag(TransferStates.None) captures EVERYTHING!!
            {
                viewStatus.SetText(Resource.String.not_started);
            }
            else if (item.State.HasFlag(TransferStates.InProgress))
            {
                viewStatus.SetText(Resource.String.in_progress);
            }
            else if (item.State.HasFlag(TransferStates.Succeeded))
            {
                viewStatus.SetText(Resource.String.completed);
            }
            else
            {

            }
        }

        private static string GetTimeRemainingString(TimeSpan? timeSpan)
        {
            if(timeSpan==null)
            {
                return SoulSeekState.ActiveActivityRef.GetString(Resource.String.unknown);
            }
            else
            {
                string[] hms = timeSpan.ToString().Split(':');
                string h = hms[0].TrimStart('0');
                if(h==string.Empty)
                {
                    h="0";
                }
                string m = hms[1].TrimStart('0');
                if (m == string.Empty)
                {
                    m = "0";
                }
                string s = hms[2].TrimStart('0');
                if(s.Contains('.'))
                {
                    s = s.Substring(0,s.IndexOf('.'));
                }
                if (s == string.Empty)
                {
                    s = "0";
                }
                //it will always be length 3.  if the seconds is more than a day it will be like "[13.21:53:20]" and if just 2 it will be like "[00:00:02]"
                if (h!="0")
                {
                    //we have hours
                    return h + "h:"+m+"m:"+s+"s";
                }
                else if(m!="0")
                {
                    return m + "m:" + s + "s";
                }
                else
                {
                    return s + "s";
                }
            }
        }

        public void SetAdditionalStatusText(TransferItem item)
        {
            if (item.State.HasFlag(TransferStates.InProgress))
            {
                viewStatusAdditionalInfo.Text = GetTimeRemainingString(item.RemainingTime);
            }
            else if (item.State.HasFlag(TransferStates.Queued))
            {
                viewStatusAdditionalInfo.Text = string.Format(SoulSeekState.MainActivityRef.GetString(Resource.String.position_),item.QueueLength.ToString());
            }
            else
            {
                viewStatusAdditionalInfo.Text = "";
            }
        }

        public void setItem(TransferItem item)
        {
            InnerTransferItem = item;
            viewFilename.Text = item.Filename;
            progressBar.Progress = item.Progress;
            SetViewStatusText(item);
            SetAdditionalStatusText(item);
            viewUsername.Text = item.Username;
            if (item.Failed)
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


        public class TransferItemViewMinimal : RelativeLayout, ITransferItemView, View.IOnCreateContextMenuListener
    {
        public TransfersFragment.TransferViewHolder ViewHolder {get;set; }
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewFilename;
        public TransferItem InnerTransferItem { get;set;}
        //private TextView viewQueue;
        public ProgressBar progressBar {get;set; }
        public TransferItemViewMinimal(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_row, this, true);
            setupChildren();
        }
        public TransferItemViewMinimal(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.transfer_row, this, true);
            setupChildren();
        }

        public static TransferItemViewMinimal inflate(ViewGroup parent)
        {
            TransferItemViewMinimal itemView = (TransferItemViewMinimal)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.transfer_row_dummy, parent, false);
            return itemView;
        }

        public void SetAdditionalStatusText(TransferItem ti)
        {
            //noop
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textView1);
            viewFilename = FindViewById<TextView>(Resource.Id.textView2);
            progressBar = FindViewById<ProgressBar>(Resource.Id.simpleProgressBar);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        public void setItem(TransferItem item)
        {
            InnerTransferItem = item;
            viewFilename.Text = item.Filename;
            progressBar.Progress = item.Progress;
            viewUsername.Text = item.Username;
            if (item.Failed)
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

    //public class TransferAdapter : ArrayAdapter<TransferItem>
    //{
    //    public TransferAdapter(Context c, List<TransferItem> items) : base(c, 0, items)
    //    {

    //    }

    //    public override View GetView(int position, View convertView, ViewGroup parent)
    //    {
    //        TransferItemView itemView = (TransferItemView)convertView;
    //        if (null == itemView)
    //        {
    //            itemView = TransferItemView.inflate(parent);
    //            itemView.LongClickable = true;
    //        }
    //        itemView.setItem(GetItem(position));
    //        return itemView;
    //        //return base.GetView(position, convertView, parent);
    //    }
    //}


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
        public static List<TransferItem> transferItems = null;
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

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_clear_all_complete:
                    MainActivity.LogInfoFirebase("Clear All Complete Pressed");
                    lock (transferItems)
                    {
                        
                        //Occurs on UI thread.
                        transferItems.RemoveAll((TransferItem i) => { return i.Progress > 99; });
                        //for (int i=0; i< tempArry.Count;i++)
                        //{
                        //    if(tempArry[i].Progress>99)
                        //    {
                        //        transferItems.Remove(tempArry[i]);
                        //    }
                        //}
                        refreshListView();
                    }
                    return true;
                case Resource.Id.action_cancel_and_clear_all:
                    MainActivity.LogInfoFirebase("action_cancel_and_clear_all Pressed");
                    lock (transferItems)
                    {
                        SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        for (int i = 0; i < transferItems.Count; i++)
                        {
                            //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(transferItems[i]), out CancellationTokenSource token);
                            token?.Cancel();
                            //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                        }
                        CancellationTokens.Clear();
                        transferItems.Clear();
                        refreshListView();
                    }
                    return true;
                case Resource.Id.action_pause_all:
                    MainActivity.LogInfoFirebase("pause all Pressed");
                    lock (transferItems)
                    {
                        SoulSeekState.CancelAndClearAllWasPressedDebouncer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        for (int i = 0; i < transferItems.Count; i++)
                        {
                            //CancellationTokens[ProduceCancellationTokenKey(transferItems[i])]?.Cancel();
                            CancellationTokens.TryGetValue(ProduceCancellationTokenKey(transferItems[i]), out CancellationTokenSource token);
                            token?.Cancel();
                            //CancellationTokens.Remove(ProduceCancellationTokenKey(transferItems[i]));
                        }
                        CancellationTokens.Clear();
                        refreshListView();
                    }
                    return true;
                case Resource.Id.action_resume_all:
                    MainActivity.LogInfoFirebase("resume all Pressed");
                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        Task t;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
                        {
                            return base.OnContextItemSelected(item);
                        }
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => {
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
                            SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(false); });
                        }));
                    }
                    else
                    {
                        DownloadRetryAllConditionLogic(false);
                    }
                    return true;
                case Resource.Id.retry_all_failed:
                    MainActivity.LogInfoFirebase("retry all failed Pressed");
                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        Task t;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
                        {
                            return base.OnContextItemSelected(item);
                        }
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => {
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
                            SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryAllConditionLogic(true); });
                        }));
                    }
                    else
                    {
                        DownloadRetryAllConditionLogic(true);
                    }
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }


        private RecyclerView.LayoutManager recycleLayoutManager;
        private RecyclerView recyclerViewTransferItems;
        private TransferAdapterRecyclerVersion recyclerTransferAdapter;

        public override void OnDestroy()
        {
            try
            {
                MainActivity.TransferItemQueueUpdated -= TranferQueueStateChanged;
            }
            catch(System.Exception)
            {

            }
            base.OnDestroy();
        }

        public static void RestoreTransferItems(ISharedPreferences sharedPreferences)
        {
            string transferList = sharedPreferences.GetString(SoulSeekState.M_TransferList, string.Empty);
            if (transferList == string.Empty)
            {
                transferItems = new List<TransferItem>();
            }
            else
            {
                transferItems = new List<TransferItem>();
                using (var stream = new System.IO.StringReader(transferList))
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(transferItems.GetType());
                    transferItems = serializer.Deserialize(stream) as List<TransferItem>;
                    BackwardsCompatFunction(transferItems);
                    OnRelaunch(transferItems);
                }
            }
        }

        public static void OnRelaunch(List<TransferItem> transfers)
        {   //transfers that were previously InProgress before we shut down should now be considered paused (cancelled)
            foreach (var ti in transfers)
            {
                if (ti.State.HasFlag(TransferStates.InProgress))
                {
                    ti.State = TransferStates.Cancelled;
                    ti.RemainingTime = null;
                }
            }
        }

        public static void BackwardsCompatFunction(List<TransferItem> transfers)
        {
            //this should eventually be removed.  its just for the old transfers which have state == None
            foreach(var ti in transfers)
            {
                if(ti.State == TransferStates.None)
                {
                    if(ti.Failed)
                    {
                        ti.State = TransferStates.Errored;
                    }
                    else if(ti.Progress==100)
                    {
                        ti.State = TransferStates.Succeeded;
                    }
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
            if(transferItems==null)//bc our sharedPref string can be older than the transferItems
            {
                RestoreTransferItems(sharedPreferences);
            }
            if (transferItems.Count != 0)
            {
                noTransfers.Visibility = ViewStates.Gone;
            }

            //TransferAdapter customAdapter = new TransferAdapter(Context, transferItems);
            //primaryListView.Adapter = (customAdapter);

            recycleLayoutManager = new CustomLinearLayoutManager(Activity);
            recyclerTransferAdapter = new TransferAdapterRecyclerVersion(transferItems);
            recyclerViewTransferItems.SetAdapter(recyclerTransferAdapter);
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



            SoulSeekState.ClearDownloadAddedEventsFromTarget(this);
            SoulSeekState.DownloadAdded += SoulSeekState_DownloadAdded;
            SoulSeekState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
            SoulSeekState.SoulseekClient.TransferProgressUpdated += SoulseekClient_TransferProgressUpdated;
            SoulSeekState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
            SoulSeekState.SoulseekClient.TransferStateChanged += SoulseekClient_TransferStateChanged;
            MainActivity.TransferItemQueueUpdated += TranferQueueStateChanged;

            MainActivity.LogInfoFirebase("AutoClear: " + SoulSeekState.AutoClearComplete);
            MainActivity.LogInfoFirebase("AutoRetry: " + SoulSeekState.AutoRetryDownload);

            return rootView;
        }

        private void TranferQueueStateChanged(object sender, TransferItem e)
        {
            SoulSeekState.MainActivityRef.RunOnUiThread(new Action(() => {
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


        public override void OnDestroyView()
        {
            //SoulSeekState.DownloadAdded -= SoulSeekState_DownloadAdded;
            //SoulSeekState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
            //SoulSeekState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
            base.OnDestroyView();

        }

        //public override void OnDestroy()
        //{
        //    //SoulSeekState.DownloadAdded -= SoulSeekState_DownloadAdded;
        //    //SoulSeekState.SoulseekClient.TransferProgressUpdated -= SoulseekClient_TransferProgressUpdated;
        //    //SoulSeekState.SoulseekClient.TransferStateChanged -= SoulseekClient_TransferStateChanged;
        //    base.OnDestroy();
        //}

        public override void OnPause()
        {
            base.OnPause();
            MainActivity.LogDebug("TransferFragment OnPause");  //this occurs when we move to the Account Tab or if we press the home button (i.e. to later kill the process)
                                                                //so this is a good place to do it.
            SaveTransferItems(sharedPreferences);
        }

        public static object TransferStateSaveLock = new object();

        public static void SaveTransferItems(ISharedPreferences sharedPreferences)
        {
            if(transferItems==null)
            {
                return;
            }
            string listOfTransferItems = string.Empty;
            using (var writer = new System.IO.StringWriter())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(transferItems.GetType());
                serializer.Serialize(writer, transferItems);
                listOfTransferItems = writer.ToString();
            }
            lock (MainActivity.SHARED_PREF_LOCK)
            lock (TransferStateSaveLock)
            {
                var editor = sharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_TransferList, listOfTransferItems);
                editor.Commit();
            }
        }

        //public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        //{
        //    base.OnCreateContextMenu(menu, (v as RecyclerView).GetLayoutManager().FindViewByPosition(0), menuInfo);
        //    //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo) menuInfo;
        //    menu.Add(0, 0, 0, "Retry Download");
        //    menu.Add(1, 1, 1, "Clear from List");
        //    menu.Add(2, 2, 2, "Cancel Download and Clear");
        //}


        private void DownloadRetryAllConditionLogic(bool failed) //if true DownloadRetryAllFailed if false Resume All Paused
        {
            List< Tuple<TransferItem,int> > transferItemConditionList = new List<Tuple<TransferItem, int>>();
            lock (transferItems)
            {
                for(int i=0;i<transferItems.Count;i++)
                {
                    if(failed)
                    {
                        if(transferItems[i].Failed)
                        {
                            transferItemConditionList.Add(new Tuple<TransferItem,int>(transferItems[i], i));
                        }
                    }
                    else //paused
                    {
                        if (transferItems[i].State.HasFlag(TransferStates.Cancelled) || transferItems[i].State.HasFlag(TransferStates.Queued))
                        {
                            transferItemConditionList.Add(new Tuple<TransferItem, int>(transferItems[i], i));
                        }
                    }
                }
            }
            bool exceptionShown = false;
            foreach(Tuple<TransferItem,int> tuple in transferItemConditionList)
            {
                TransferItem item = tuple.Item1;
                //TransferItem item1 = transferItems[info.Position];  
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    Android.Net.Uri incompleteUri = null;
                    Task task = DownloadDialog.DownloadFileAsync(item.Username, item.FullFilename, item.Size, cancellationTokenSource);
                    task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item.Username, item.FullFilename, item.Size, task, cancellationTokenSource,item.QueueLength,0))));
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
                    if(!exceptionShown)
                    {
                        SoulSeekState.MainActivityRef.RunOnUiThread(a);
                        exceptionShown = true;
                    }
                    return; //otherwise null ref with task!
                }
                //save to disk, update ui.
                //task.ContinueWith(SoulSeekState.MainActivityRef.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username,item1.FullFilename,item1.Size,task, cancellationTokenSource))));
                item.Progress = 0; //no longer red... some good user feedback
                item.Failed = false;
                var refreshOnlySelected = new Action(() => { 
                    
                    MainActivity.LogDebug("notifyItemChanged " + tuple.Item2);
                    recyclerTransferAdapter?.NotifyItemChanged(tuple.Item2); 
                    
                    
                    });
                lock (transferItems)
                { //also can update this to do a partial refresh...
                    refreshListView(refreshOnlySelected);
                }
            }
            

        }


        private void DownloadRetryLogic(int position)
        {
            //AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
            //its possible that in between creating this contextmenu and getting here that the transfer items changed especially if maybe a re-login was done...
            //either position changed, or someone just straight up cleared them in the meantime...
            //maybe we can add the filename in the menuinfo to match it with, rather than the index..

            //NEVER USE GetChildAt!!!  IT WILL RETURN NULL IF YOU HAVE MORE THAN ONE PAGE OF DATA
            //FindViewByPosition works PERFECTLY.  IT RETURNS THE VIEW CORRESPONDING TO THE TRANSFER LIST..

            ITransferItemView targetView = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(position) as ITransferItemView;

            TransferItem item1 = null;
            MainActivity.LogDebug("targetView is null? " + (targetView == null).ToString());
            if(targetView == null)
            {
                SeekerApplication.ShowToast(SoulSeekState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist),ToastLength.Short);
                return;
            }
            string chosenFname = targetView.InnerTransferItem.FullFilename; //  targetView.FindViewById<TextView>(Resource.Id.textView2).Text;
            MainActivity.LogDebug("chosenFname? " + chosenFname);
            item1 = GetTransferItemByFileName(chosenFname, out int _);
            MainActivity.LogDebug("item1 is null?" + (item1==null).ToString());//tested
            if (item1==null)
            {
                SeekerApplication.ShowToast(SoulSeekState.MainActivityRef.GetString(Resource.String.chosen_transfer_doesnt_exist), ToastLength.Short);
                return;
            }




            //TransferItem item1 = transferItems[info.Position];  
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            try
            {

                Android.Net.Uri incompleteUri = null;
                Task task = DownloadDialog.DownloadFileAsync(item1.Username, item1.FullFilename, item1.Size, cancellationTokenSource);
                    //SoulSeekState.SoulseekClient.DownloadAsync(
                    //username: item1.Username,
                    //filename: item1.FullFilename,
                    //size: item1.Size,
                    //cancellationToken: cancellationTokenSource.Token);
                task.ContinueWith(MainActivity.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username, item1.FullFilename, item1.Size, task, cancellationTokenSource,item1.QueueLength, 1)))); //maybe do 1 here since we are already retrying it manually
            }
            catch (DuplicateTransferException)
            {
                //happens due to button mashing...
                return;
            }
            catch (System.Exception error)
            {
                Action a = new Action(() => { Toast.MakeText(SoulSeekState.MainActivityRef, SoulSeekState.MainActivityRef.GetString(Resource.String.error_) + error.Message, ToastLength.Long); });
                if(error.Message != null && error.Message.ToString().Contains("must be connected and logged"))
                {

                }
                else
                {
                    MainActivity.LogFirebase(error.Message + " OnContextItemSelected");
                }
                SoulSeekState.MainActivityRef.RunOnUiThread(a);
                return; //otherwise null ref with task!
            }



            //save to disk, update ui.
            //task.ContinueWith(SoulSeekState.MainActivityRef.DownloadContinuationActionUI(new DownloadAddedEventArgs(new DownloadInfo(item1.Username,item1.FullFilename,item1.Size,task, cancellationTokenSource))));
            item1.Progress = 0; //no longer red... some good user feedback
            item1.QueueLength = 0; //let the State Changed update this for us...
            item1.Failed = false;
            var refreshOnlySelected = new Action(() => {

                MainActivity.LogDebug("notifyItemChanged " + position); 

                recyclerTransferAdapter.NotifyItemChanged(position); 
                
                
                });
            lock (transferItems)
            { //also can update this to do a partial refresh...
                refreshListView(refreshOnlySelected);
            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            int position = recyclerTransferAdapter.getPosition();
            switch (item.ItemId)
            {
                case 0:
                    //retry download

                    if (MainActivity.CurrentlyLoggedInButDisconnectedState())
                    {
                        Task t;
                        if (!MainActivity.ShowMessageAndCreateReconnectTask(this.Context, out t))
                        {
                            return base.OnContextItemSelected(item);
                        }
                        t.ContinueWith(new Action<Task>((Task t) =>
                        {
                            if (t.IsFaulted)
                            {
                                SoulSeekState.MainActivityRef.RunOnUiThread(() => { 
                                    if(Context!=null)
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
                            SoulSeekState.MainActivityRef.RunOnUiThread(() => { DownloadRetryLogic(position); });
                        }));
                    }
                    else
                    {
                        DownloadRetryLogic(position);
                    }
                    //Toast.MakeText(Applicatio,"Retrying...",ToastLength.Short).Show();
                    break;
                case 1:
                    //clear complete?
                    //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                    MainActivity.LogInfoFirebase("Clear Complete item pressed");
                    lock (transferItems)
                    {
                        try
                        {
                            transferItems.RemoveAt(position); //UI
                        }
                        catch(ArgumentOutOfRangeException)
                        {
                            MainActivity.LogFirebase("case1: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                            Toast.MakeText(SoulSeekState.MainActivityRef,"Selected transfer does not exist anymore.. try again.",ToastLength.Short).Show();
                            return base.OnContextItemSelected(item);
                        }
                        recyclerTransferAdapter.NotifyItemRemoved(position);  //UI
                        //refreshListView();
                    }
                    break;
                case 2:
                    //info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
                    MainActivity.LogInfoFirebase("Cancel and Clear item pressed");
                    TransferItem tItem = null;
                    try
                    {
                        tItem = transferItems[position];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        MainActivity.LogFirebase("case2: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                        Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                        return base.OnContextItemSelected(item);
                    }
                    CancellationTokens.TryGetValue(ProduceCancellationTokenKey(tItem), out CancellationTokenSource token);
                    token?.Cancel();
                    //CancellationTokens[ProduceCancellationTokenKey(tItem)]?.Cancel(); throws if does not exist.
                    CancellationTokens.Remove(ProduceCancellationTokenKey(tItem));
                    MainActivity.LogDebug("Cancellation Token does not exist");
                    //tItem.CancellationTokenSource.Cancel();
                    lock (transferItems)
                    {
                        transferItems.RemoveAt(position);
                        recyclerTransferAdapter.NotifyItemRemoved(position);
                    }
                    break;
                case 3:
                    tItem = null;
                    try
                    {
                        tItem = transferItems[position];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        MainActivity.LogFirebase("case3: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                        Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                        return base.OnContextItemSelected(item);
                    }
                    int queueLenOld = tItem.QueueLength;

                    Func<TransferItem, object> actionOnComplete = new Func<TransferItem, object>((TransferItem t) =>
                        {
                            try
                            {
                            if(queueLenOld == t.QueueLength) //always true bc its a reference...
                            {
                                Toast.MakeText(SoulSeekState.MainActivityRef, string.Format(SoulSeekState.MainActivityRef.GetString(Resource.String.position_is_still_), t.QueueLength),ToastLength.Short).Show();
                            }
                            else
                            {
                                GetTransferItemByFileName(t.FullFilename, out int indexOfItem);
                                recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
                            }
                            }
                            catch(System.Exception e)
                            {
                                MainActivity.LogFirebase("actionOnComplete" + e.Message + e.StackTrace);
                            }

                            return null;
                        });

                    MainActivity.GetDownloadPlaceInQueue(tItem.Username, tItem.FullFilename, actionOnComplete);
                    break;
                case 4:
                    tItem = null;
                    try
                    {
                        tItem = transferItems[position];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        MainActivity.LogFirebase("case4: info.Position: " + position + " transferItems.Count is: " + transferItems.Count);
                        Toast.MakeText(SoulSeekState.MainActivityRef, "Selected transfer does not exist anymore.. try again.", ToastLength.Short).Show();
                        return base.OnContextItemSelected(item);
                    }
                    try
                    {
                        //tested on API25 and API30
                        //AndroidX.Core.Content.FileProvider
                        Android.Net.Uri uriToUse = null;
                        if(SoulSeekState.UseLegacyStorage())
                        {
                            uriToUse = AndroidX.Core.Content.FileProvider.GetUriForFile(this.Context, this.Context.ApplicationContext.PackageName + ".provider", new Java.IO.File(Android.Net.Uri.Parse(tItem.FinalUri).Path));
                        }
                        else
                        {
                            uriToUse = Android.Net.Uri.Parse(tItem.FinalUri);
                        }
                        Intent playFileIntent = new Intent(Intent.ActionView);
                        //playFileIntent.SetDataAndType(uriToUse,"audio/*");  
                        playFileIntent.SetDataAndType(uriToUse, Helpers.GetMimeTypeFromFilename(tItem.FullFilename));   //works
                        playFileIntent.AddFlags(ActivityFlags.GrantReadUriPermission | /*ActivityFlags.NewTask |*/ ActivityFlags.GrantWriteUriPermission); //works.  newtask makes it go to foobar and immediately jump back
                        //Intent chooser = Intent.CreateChooser(playFileIntent, "Play song with");
                        this.StartActivity(playFileIntent); //also the chooser isnt needed.  if you show without the chooser, it will show you the options and you can check Only Once, Always.
                    }
                    catch(System.Exception e)
                    {
                        MainActivity.LogFirebase(e.Message + e.StackTrace);
                        Toast.MakeText(this.Context, Resource.String.failed_to_play, ToastLength.Short).Show(); //normally bc no player is installed.
                    }
                    break;
            }
            return base.OnContextItemSelected(item);
        }

        public void UpdateQueueState(string fullFilename) //Add this to the event handlers so that when downloads are added they have their queue position.
        {
            try
            {
                GetTransferItemByFileName(fullFilename, out int indexOfItem);
                MainActivity.LogDebug("NotifyItemChanged + UpdateQueueState" + indexOfItem);
                MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: " + fullFilename);
                if(recyclerTransferAdapter.ItemCount == indexOfItem)
                {

                }
                MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
                recyclerTransferAdapter.NotifyItemChanged(indexOfItem);
            }
            catch(System.Exception)
            {

            }
        }


        public static TransferItem GetTransferItemByFileName(string fullFileName, out int indexOfItem)
        {
            if(fullFileName==null)
            {
                indexOfItem = -1;
                return null;
            }
            lock (transferItems)
            {
                foreach (TransferItem item in transferItems)
                {
                    if (item.FullFilename.Equals(fullFileName)) //fullfilename includes dir so that takes care of any ambiguity...
                    {
                        indexOfItem = transferItems.IndexOf(item);
                        return item;
                    }
                }
            }
            indexOfItem = -1;
            return null;
        }

        private void SoulseekClient_TransferStateChanged(object sender, TransferStateChangedEventArgs e)
        {
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop();
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }
            int indexOfItem = -1;
            TransferItem relevantItem = GetTransferItemByFileName(e.Transfer?.Filename, out indexOfItem);
            if (relevantItem != null)
            {
                relevantItem.State = e.Transfer.State;
            }
            if (e.Transfer.State.HasFlag(TransferStates.Errored) || e.Transfer.State.HasFlag(TransferStates.TimedOut))
            {

                //this gets called regardless of UI lifecycle, so I am guessing that this.transferItems is null....
                //maybe better to make static and independent... bc you should still be able to update an item as failed even if UI is disposed
                //if(transferItems==null)
                //{
                //    MainActivity.LogFirebase(("transferItems is null" + " SoulseekClient_TransferStateChanged"));
                //    return;
                //} 
                if (relevantItem == null)
                {
                    return;
                }
                else
                {
                    relevantItem.Failed = true;
                    Action action = new Action(()=>{refreshListViewSpecificItem(indexOfItem); });
                    SoulSeekState.MainActivityRef.RunOnUiThread(action);
                    //Activity.RunOnUiThread(action); //this is probably the cause of the nullref.  since Activity is null whenever Context is null i.e. onDeattach or not yet attached..
                }
            }
            else if(e.Transfer.State.HasFlag(TransferStates.Queued))
            {
                if (relevantItem == null)
                {
                    return;
                }
                relevantItem.Queued = true;
                if(relevantItem.QueueLength!=0) //this means that it probably came from a search response where we know the users queuelength  ***BUT THAT IS NEVER THE ACTUAL QUEUE LENGTH*** its always much shorter...
                {
                    //nothing to do, bc its already set..
                    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename, null);
                }
                else //this means that it came from a browse response where we may not know the users initial queue length... or if its unexpectedly queued.
                {
                    //GET QUEUE LENGTH AND UPDATE...
                    MainActivity.GetDownloadPlaceInQueue(e.Transfer.Username, e.Transfer.Filename,null);
                }
                Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
            else if(e.Transfer.State.HasFlag(TransferStates.Initializing))
            {
                if (relevantItem == null)
                {
                    return;
                }
                //clear queued flag...
                relevantItem.Queued = false;
                relevantItem.QueueLength = 0;
                Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
            else if(e.Transfer.State.HasFlag(TransferStates.Completed))
            {
                if (relevantItem == null)
                {
                    return;
                }
                if(!e.Transfer.State.HasFlag(TransferStates.Cancelled))
                {
                    //clear queued flag...
                    relevantItem.Progress = 100;
                    Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
                    SoulSeekState.MainActivityRef.RunOnUiThread(action);
                }
            }
            else
            {
                Action action = new Action(() => { refreshListViewSpecificItem(indexOfItem); });
                SoulSeekState.MainActivityRef.RunOnUiThread(action);
            }
        }

        private void SoulseekClient_TransferProgressUpdated(object sender, TransferProgressUpdatedEventArgs e)
        {
            //Its possible to get a nullref here IF the system orientation changes..
            //throttle this maybe...

            //MainActivity.LogDebug("TRANSFER PROGRESS UPDATED"); //this typically happens once every 10 ms or even less and thats in debug mode.  in fact sometimes it happens 4 times in 1 ms.
            try
            {
                MainActivity.KeepAliveInactivityKillTimer.Stop(); //lot of nullref here...
                MainActivity.KeepAliveInactivityKillTimer.Start();
            }
            catch (System.Exception err)
            {
                MainActivity.LogFirebase("timer issue2: " + err.Message + err.StackTrace); //remember at worst the locks will get released early which is fine.
            }
            TransferItem relevantItem = null;
            if (transferItems == null)
            {
                MainActivity.LogDebug("transferItems Null " + e.Transfer.Filename);
                return;
            }
            lock (transferItems)
            {
                foreach (TransferItem item in transferItems) //THIS is where those enumeration exceptions are all coming from...
                {
                    if (item.FullFilename.Equals(e.Transfer.Filename))
                    {
                        relevantItem = item;
                        break;
                    }
                }
            }
            if (relevantItem == null)
            {
                //this happens on Clear and Cancel All.
                MainActivity.LogDebug("Relevant Item Null " + e.Transfer.Filename);
                MainActivity.LogDebug("transferItems.Count " + transferItems.Count);
                return;
            }
            else
            {
                bool fullRefresh = false;
                double percentComplete = e.Transfer.PercentComplete;
                relevantItem.Progress = (int)percentComplete;
                relevantItem.RemainingTime = e.Transfer.RemainingTime;
               // int indexRemoved = -1;
                if (SoulSeekState.AutoClearComplete && System.Math.Abs(percentComplete - 100) < .001) //if 100% complete and autoclear
                {
                    fullRefresh = true;
                    Action action = new Action(() => {
                        int before = transferItems.Count;
                        lock (transferItems)
                        {
                            //used to occur on nonUI thread.  Im pretty sure this causes the recyclerview inconsistency crash..
                            //indexRemoved = transferItems.IndexOf(relevantItem);
                            transferItems.Remove(relevantItem);
                        }
                        int after = transferItems.Count;
                        MainActivity.LogDebug("transferItems.Remove(relevantItem): before: " + before + "after: " + after);
                    });
                    if(Activity!=null)
                    {
                        Activity?.RunOnUiThread(action);
                    }
                    else
                    {
                        SoulSeekState.MainActivityRef?.RunOnUiThread(action);
                    }
                }
                else if (System.Math.Abs(percentComplete - 100) < .001)
                {
                    fullRefresh = true;
                }
                if (percentComplete != 0)
                {
                    bool wasFailed = false;
                    if (relevantItem.Failed)
                    {
                        wasFailed = true;
                        relevantItem.Failed = false;
                    }
                    if (fullRefresh)
                    {
                        Action action = refreshListViewSafe;
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
                            bool isNew = !ProgressUpdatedThrottler.ContainsKey(relevantItem.FullFilename);

                            DateTime now = DateTime.UtcNow;
                            DateTime lastUpdated = ProgressUpdatedThrottler.GetOrAdd(relevantItem.FullFilename, now); //this returns now if the key is not in the dictionary!
                            if(now.Subtract(lastUpdated).TotalMilliseconds > THROTTLE_PROGRESS_UPDATED_RATE || isNew)
                            {
                                ProgressUpdatedThrottler[relevantItem.FullFilename] = now;
                            }
                            else if(wasFailed)
                            {
                                //still update..
                            }
                            else
                            {
                                return;
                            }

                            int index = -1;
                            //partial refresh just update progress..
                            lock (transferItems)
                            {
                                for(int i=0;i<transferItems.Count;i++)
                                {
                                    if (transferItems[i].FullFilename == relevantItem.FullFilename)
                                    {
                                        index = i;
                                    }
                                }
                            }
                            //MainActivity.LogDebug("Index is "+index+" TransferProgressUpdated"); //tested!
                            if (index==-1)
                            {
                                MainActivity.LogDebug("Index is -1 TransferProgressUpdated");
                                return;
                            }
                            //int indexToUpdate = transferItems.IndexOf(relevantItem);

                            Activity?.RunOnUiThread(() => {

                                MainActivity.LogDebug("UI THREAD TRANSFER PROGRESS UPDATED"); //this happens every 20ms.  so less often then tranfer progress updated.  usually 6 of those can happen before 2 of these.
                                refreshItemProgress(index, relevantItem.Progress, relevantItem, wasFailed); 
                                
                                });



                        }
                        catch (System.Exception error)
                        {
                            MainActivity.LogFirebase(error.Message + " partial update");
                        }
                    }
                }
            }

        }

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


        private void refreshItemProgress(int indexToRefresh, int progress, TransferItem relevantItem, bool wasFailed)
        {
            //View v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh);


            //METHOD 1 of updating... (causes flicker)
            //recyclerViewTransferItems.GetAdapter().NotifyItemChanged(indexToRefresh); //this index is the index in the transferItem list...

            //METHOD 2 of updating (no flicker)
            ITransferItemView v = recyclerViewTransferItems.GetLayoutManager().FindViewByPosition(indexToRefresh) as ITransferItemView;
            if(v!=null) //it scrolled out of view which is find bc it will get updated when it gets rebound....
            {
                v.progressBar.Progress = progress;
                v.SetAdditionalStatusText(relevantItem);
                if(wasFailed)
                {
                    ClearProgressBarColor(v.progressBar);
                }
            }

                //recyclerViewTransferItems.GetChildAt(indexToRefresh - recyclerViewTransferItems.GetLayoutManager().FindViewByPositionFirstVisiblePosition);
                //            if (v == null)
                //            {
                //                //this is when its not visible i.e. you scroll.  it is null in that case.
                //                return;
                //            }
                //            ProgressBar progressBar = v.FindViewById<ProgressBar>(Resource.Id.simpleProgressBar) as ProgressBar;
                //            progressBar.Progress = progress;


            //            if (relevantItem.Failed)
            //            {
            //                progressBar.Progress = 100;
            //#pragma warning disable 0618
            //                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            //                {
            //                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.Red);
            //                }
            //                else
            //                {
            //                    progressBar.ProgressDrawable.SetColorFilter(Color.Red, PorterDuff.Mode.Multiply);
            //                }
            //#pragma warning restore 0618
            //            }
            //            else
            //            {
            //#pragma warning disable 0618
            //                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            //                {
            //                    progressBar.ProgressTintList = ColorStateList.ValueOf(Color.DodgerBlue);
            //                }
            //                else
            //                {
            //                    progressBar.ProgressDrawable.SetColorFilter(Color.DodgerBlue, PorterDuff.Mode.Multiply);
            //                }
            //#pragma warning restore 0618
            //            }
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
            if (transferItems.Count != 0)
            {
                noTransfers.Visibility = ViewStates.Gone;
            }
            else
            {
                noTransfers.Visibility = ViewStates.Visible;
            }
            MainActivity.LogDebug("NotifyItemChanged" + indexOfItem);
            MainActivity.LogDebug("item count: " + recyclerTransferAdapter.ItemCount + " indexOfItem " + indexOfItem + "itemName: ");
            MainActivity.LogDebug("UI thread: " + Looper.MainLooper.IsCurrentThread);
            if (recyclerTransferAdapter.ItemCount == indexOfItem)
            {

            }
            recyclerTransferAdapter.NotifyItemChanged(indexOfItem);

        }

        private void refreshListView(Action specificRefreshAction = null)
        {
            //creating the TransferAdapter can cause a Collection was Modified error due to transferItems.
            //maybe a better way to do this is .ToList().... rather than locking...
            //TransferAdapter customAdapter = null;
            if (Context == null)
            {
                if(SoulSeekState.MainActivityRef == null)
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
            if(this.noTransfers == null)
            {
                MainActivity.LogFirebase("cannot refreshListView on TransferStateUpdated, noTransfers is null");
                return;
            }
            if (transferItems.Count != 0)
            {
                noTransfers.Visibility = ViewStates.Gone;
            }
            else
            {
                noTransfers.Visibility = ViewStates.Visible;
            }
            if(specificRefreshAction==null)
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

        public class TransferAdapterRecyclerVersion : RecyclerView.Adapter //<TransferAdapterRecyclerVersion.TransferViewHolder>
        {
            private List<TransferItem> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position=-1;

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as TransferViewHolder).getTransferItemView().setItem(localDataSet[position]);
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

            private void TransferAdapterRecyclerVersion_LongClick(object sender, View.LongClickEventArgs e)
            {
                //var pop = new PopupMenu(SoulSeekState.MainActivityRef,(sender as TransferItemView),GravityFlags.Right);//anchor to sender
                //pop.Inflate(Resource.Menu.download_diag_options);
                //pop.Show();
                setPosition((sender as ITransferItemView).ViewHolder.AdapterPosition);
                (sender as View).ShowContextMenu();
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                bool TYPE_TO_USE = true;
                ITransferItemView view = null;
                if (TYPE_TO_USE)
                {
                    view = TransferItemViewDetails.inflate(parent);
                }
                else
                {
                    view = TransferItemViewMinimal.inflate(parent);
                }
                view.setupChildren();
                // .inflate(R.layout.text_row_item, viewGroup, false);
                (view as View).LongClick += TransferAdapterRecyclerVersion_LongClick;
                return new TransferViewHolder(view as View);

            }

            public TransferAdapterRecyclerVersion(List<TransferItem> ti)
            {
                localDataSet = ti;
            }

        }


        //public class ContextMenuRecyclerView : RecyclerView
        //{

        //  private RecyclerViewContextMenuInfo mContextMenuInfo;

        //        @Override
        //  protected ContextMenu.ContextMenuInfo getContextMenuInfo()
        //        {
        //            return mContextMenuInfo;
        //        }

        //        @Override
        //  public boolean showContextMenuForChild(View originalView)
        //        {
        //            final int longPressPosition = getChildPosition(originalView);
        //            if (longPressPosition >= 0)
        //            {
        //                final long longPressId = getAdapter().getItemId(longPressPosition);
        //                mContextMenuInfo = new RecyclerViewContextMenuInfo(longPressPosition, longPressId);
        //                return super.showContextMenuForChild(originalView);
        //            }
        //            return false;
        //        }

        //        public static class RecyclerViewContextMenuInfo implements ContextMenu.ContextMenuInfo
        //        {

        //    public RecyclerViewContextMenuInfo(int position, long id)
        //        {
        //            this.position = position;
        //            this.id = id;
        //        }

        //        final public int position;
        //        final public long id;
        //    }
        //}


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
                
                AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo) menuInfo;
                if(tvh != null && tvh.InnerTransferItem != null&&tvh.InnerTransferItem.State.HasFlag(TransferStates.Cancelled) && tvh.InnerTransferItem.Progress>0)
                {
                    menu.Add(0, 0, 0, Resource.String.resume_dl);
                }
                else
                {
                    menu.Add(0, 0, 0, Resource.String.retry_dl);
                }
                menu.Add(1, 1, 1, Resource.String.clear_from_list);
                if (tvh != null && tvh.InnerTransferItem != null && (tvh.InnerTransferItem.State.HasFlag(TransferStates.Succeeded)))
                {
                    //if completed then we dont need to show the cancel option...
                }
                else
                {
                    menu.Add(2, 2, 2, Resource.String.cancel_and_clear);
                }
                if(tvh!=null&&tvh.InnerTransferItem!=null)
                {
                    if(tvh.InnerTransferItem.QueueLength>0) 
                    {
                        //the queue length of a succeeded download can be 183......
                        //bc queue length AND free upload slots!!
                        if(tvh.InnerTransferItem.State.HasFlag(TransferStates.Succeeded) || 
                            tvh.InnerTransferItem.State.HasFlag(TransferStates.Completed))
                        {
                            //no op
                        }
                        else
                        {
                            menu.Add(3,3,3, Resource.String.refresh_queue_pos);
                        }
                    }
                }
                if (tvh != null && tvh.InnerTransferItem != null && (tvh.InnerTransferItem.State.HasFlag(TransferStates.Succeeded)) && tvh.InnerTransferItem.FinalUri!=string.Empty)
                {
                    menu.Add(4,4,4, Resource.String.play_file);
                }

            }

        }



    private void SoulSeekState_DownloadAdded(object sender, DownloadAddedEventArgs e)
        {
            MainActivity.LogDebug("SoulSeekState_DownloadAdded");
            TransferItem transferItem = new TransferItem();
            transferItem.Filename = Helpers.GetFileNameFromFile(e.dlInfo.fullFilename);
            transferItem.Foldername = Helpers.GetFolderNameFromFile(e.dlInfo.fullFilename);
            transferItem.Username = e.dlInfo.username;
            transferItem.FullFilename = e.dlInfo.fullFilename;
            transferItem.Size = e.dlInfo.Size;
            transferItem.CancellationTokenSource = e.dlInfo.CancellationTokenSource;
            transferItem.QueueLength = e.dlInfo.QueueLength;

            e.dlInfo.TransferItemReference = transferItem;

            if (!CancellationTokens.TryAdd(ProduceCancellationTokenKey(transferItem), e.dlInfo.CancellationTokenSource))
            {
                //likely old already exists so just replace the old one
                CancellationTokens[ProduceCancellationTokenKey(transferItem)] = e.dlInfo.CancellationTokenSource;
            }
            addTransferItemToListView(transferItem); //this is where the enumeration collection modified was thrown.  probably bc UI was also modifying collection.
        }

        private void addTransferItemToListView(TransferItem ti)
        {
            SoulSeekState.MainActivityRef.RunOnUiThread(() => {
            //occurs on nonUI thread...
            //if there is any deadlock due to this, then do Thread.Start().
            lock (transferItems)
            {
                transferItems.Add(ti); //UI

                refreshListView();
            }

            });
        }

        public static string ProduceCancellationTokenKey(TransferItem i)
        {
            return ProduceCancellationTokenKey(i.FullFilename, i.Size, i.Username);
        }

        public static string ProduceCancellationTokenKey(string fullFilename, long size, string username)
        {
            return fullFilename + size.ToString() + username;
        }


        public static Dictionary<string, CancellationTokenSource> CancellationTokens = new Dictionary<string, CancellationTokenSource>();




    }

    //    public class ChangeUserTargetDialog : Android.Support.V4.App.DialogFragment
    //{
    //    private EditText chooseUserInput = null;
    //    private AndroidX.AppCompat.Widget.AppCompatRadioButton allUsers = null;
    //    private AndroidX.AppCompat.Widget.AppCompatRadioButton chosenUser = null;
    //    private AndroidX.AppCompat.Widget.AppCompatRadioButton userList = null;

    //    public ChangeUserTargetDialog() :base()
    //    {
    //    }

    //    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    //    {
    //        return inflater.Inflate(Resource.Layout.changeusertarget, container); //container is parent
    //    }

    //    public override void OnCreate(Bundle savedInstanceState)
    //    {
    //        base.OnCreate(savedInstanceState);
    //    }


    //    /// <summary>
    //    /// Called after on create view
    //    /// </summary>
    //    /// <param name="view"></param>
    //    /// <param name="savedInstanceState"></param>
    //    public override void OnViewCreated(View view, Bundle savedInstanceState)
    //    {
    //        this.SetStyle((int)Android.App.DialogFragmentStyle.NoTitle, 0);
    //        allUsers = this.View.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.allUsers);
    //        chosenUser = this.View.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.chosenUser);
    //        userList = this.View.FindViewById<AndroidX.AppCompat.Widget.AppCompatRadioButton>(Resource.Id.targetUserList);

    //        chooseUserInput = this.View.FindViewById<EditText>(Resource.Id.chosenUserInput);
    //        chooseUserInput.Text = SearchFragment.SearchTargetChosenUser;


    //        switch (SearchFragment.SearchTarget)
    //        {
    //            case SearchTarget.AllUsers:
    //                allUsers.Checked = true;
    //                chooseUserInput.Visibility = ViewStates.Gone;
    //                break;
    //            case SearchTarget.UserList:
    //                userList.Checked = true;
    //                chooseUserInput.Visibility = ViewStates.Gone;
    //                break;
    //            case SearchTarget.ChosenUser:
    //                chosenUser.Checked = true;
    //                chooseUserInput.Visibility = ViewStates.Visible;
    //                break;

    //        }

    //        allUsers.Click += AllUsers_Click;
    //        chosenUser.Click += ChosenUser_Click;
    //        userList.Click += UserList_Click;
    //        chooseUserInput.TextChanged += ChooseUserInput_TextChanged;
    //    }

    //    private void ChooseUserInput_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
    //    {
    //        SearchFragment.SearchTargetChosenUser = e.Text.ToString();
    //    }

    //    private void AllUsers_Click(object sender, EventArgs e)
    //    {
    //        SearchFragment.SearchTarget = SearchTarget.AllUsers;
    //        chooseUserInput.Visibility = ViewStates.Gone;
    //    }

    //    private void ChosenUser_Click(object sender, EventArgs e)
    //    {
    //        SearchFragment.SearchTarget = SearchTarget.ChosenUser;
    //        chooseUserInput.Visibility = ViewStates.Visible;
    //    }

    //    private void UserList_Click(object sender, EventArgs e)
    //    {
    //        SearchFragment.SearchTarget = SearchTarget.UserList;
    //        chooseUserInput.Visibility = ViewStates.Gone;
    //    }

    //}
}
