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
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace AndriodApp1
{
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

        private static void EnableDisableLoginButton(EditText uname, EditText passwd, Button login, bool hasError)
        {
            if (string.IsNullOrEmpty(uname.Text) || string.IsNullOrEmpty(passwd.Text) || hasError)
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
        private TextInputLayout usernameInputLayout = null;


        public void SetUpLogInLayout()
        {
            loginButton = rootView.FindViewById<Button>(Resource.Id.buttonLogin);
            loginButton.Click += LogInClick;
            usernameTextEdit = rootView.FindViewById<EditText>(Resource.Id.etUsername);
            passwordTextEdit = rootView.FindViewById<EditText>(Resource.Id.etPassword);
            usernameInputLayout = rootView.FindViewById<TextInputLayout>(Resource.Id.usernameTextInputLayout);
            usernameTextEdit.TextChanged += UsernamePasswordTextEdit_TextChanged;
            usernameTextEdit.FocusChange += SearchFragment.MainActivity_FocusChange;
            passwordTextEdit.TextChanged += UsernamePasswordTextEdit_TextChanged;
            passwordTextEdit.FocusChange += SearchFragment.MainActivity_FocusChange;
            bool hasError = ValidateUsername();
            EnableDisableLoginButton(usernameTextEdit, passwordTextEdit, loginButton, hasError);
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
                    login?.ContinueWith(MainActivity.GetPostNotifPermissionTask());
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

        private readonly int[] All_Ascii = Enumerable.Range('\x1', 127).ToArray();

        private void UsernamePasswordTextEdit_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            bool hasError = ValidateUsername();
            EnableDisableLoginButton(this.usernameTextEdit, this.passwordTextEdit, loginButton, hasError);
        }

        private bool ValidateUsername()
        {
            // special chars and length check.
            bool hasError = false;
            if (!string.IsNullOrEmpty(usernameTextEdit.Text))
            {
                var uname = usernameTextEdit.Text.ToString();
                if (uname.Length > 30)
                {
                    usernameInputLayout.Error = this.GetString(Resource.String.user_too_long);
                    hasError = true;
                }
                else
                {
                    foreach (char c in uname)
                    {
                        if (!All_Ascii.Contains(c))
                        {
                            usernameInputLayout.Error = this.GetString(Resource.String.user_invalid_char);
                            hasError = true;
                            break;
                        }
                    }
                }
            }

            if (!hasError)
            {
                usernameInputLayout.Error = null;
                usernameInputLayout.ErrorEnabled = false;
            }

            return hasError;
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
                    if (t.Exception.InnerExceptions[0] is Soulseek.LoginRejectedException lre)
                    {
                        string loginRejectedMessage = lre.Message;
                        cannotLogin = true;
                        //"The server rejected login attempt: INVALIDUSERNAME"
                        if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDUSERNAME"))
                        {
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.invalid_username);
                        }
                        else if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDPASS"))
                        {
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.invalid_password);
                        }
                        else
                        {
                            msg = SoulSeekState.ActiveActivityRef.GetString(Resource.String.bad_user_pass);
                        }
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
            bool alreadyConnected = false;
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
                login?.ContinueWith(MainActivity.GetPostNotifPermissionTask());
                //throw new InvalidOperationException("The client is already connected"); //bug catcher

                //login.ContinueWith(t=>t.IsCompletedSuccessfully)
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
                    alreadyConnected = true;
                    SoulSeekState.currentlyLoggedIn = true;
                    MainActivity.AddLoggedInLayout(this.rootView, true);
                    MainActivity.UpdateUIForLoggedIn(this.rootView);
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
                if (!alreadyConnected)
                {
                    MainActivity.UpdateUIForLoggingInLoading(this.rootView);
                }
                SoulSeekState.Username = this.usernameTextEdit.Text;
                SoulSeekState.Password = this.passwordTextEdit.Text;
                if (!alreadyConnected)
                {
                    StaticHacks.LoggingIn = true;
                }
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
}