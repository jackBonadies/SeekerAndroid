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

using Seeker.Services;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Google.Android.Material.TextField;
using Soulseek;
using System;
using System.Linq;
using System.Threading.Tasks;
using Seeker.Helpers;
using Seeker.Messages;
using Common;
namespace Seeker
{
    public class LoginFragment : Fragment
    {
        public const string LogoutMessage = "UserLogout";

        private static bool s_loginInFlight;

        private ViewFlipper viewFlipper;
        private View rootView;

        // Login form views (child 0)
        private Button loginButton;
        private EditText usernameTextEdit;
        private EditText passwordTextEdit;
        private TextInputLayout usernameInputLayout;

        // Logged-in views (child 2)
        private Button mustSelectDirButton;
        private TextView welcomeTextView;
        private View connectionStatusDot;
        private TextView connectionStatusText;
        private View connectionStatusChip;

        // Menu rows
        private View menuSetUpSharing;
        private View menuManageUserList;
        private View menuMessages;
        private TextView messagesUnreadBadge;
        private View menuSettings;
        private View menuLogout;

        private const int ChildLoginForm = 0;
        private const int ChildLoading = 1;
        private const int ChildLoggedIn = 2;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.account_menu, menu);
            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override void OnResume()
        {
            base.OnResume();

            SeekerState.SoulseekClient.StateChanged += SoulseekClient_StateChanged;
            UpdateConnectionStatus(SeekerState.SoulseekClient.State);

            MessageController.MessageReceived += OnMessageReceivedUpdateBadge;
            MessagesBroadcastReceiver.MarkAsReadFromNotification += OnMarkAsReadUpdateBadge;
            UpdateUnreadBadge();
        }

        public override void OnPause()
        {
            base.OnPause();
            SeekerState.SoulseekClient.StateChanged -= SoulseekClient_StateChanged;
            MessageController.MessageReceived -= OnMessageReceivedUpdateBadge;
            MessagesBroadcastReceiver.MarkAsReadFromNotification -= OnMarkAsReadUpdateBadge;
        }

        private void SoulseekClient_StateChanged(object sender, SoulseekClientStateChangedEventArgs e)
        {
            this.Activity?.RunOnUiThread(() =>
            {
                UpdateConnectionStatus(e.State);
            });
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            HasOptionsMenu = true;
            Logger.Debug("LoginFragmentOnCreateView");
            SeekerState.LoginFragmentRef = this;

            rootView = inflater.Inflate(Resource.Layout.login_viewflipper, container, false);
            viewFlipper = rootView.FindViewById<ViewFlipper>(Resource.Id.loginViewFlipper);

            SetUpLoginFormViews();
            SetUpLoggedInViews();

            if (SessionService.Instance.IsNotLoggedIn())
            {
                PreferencesState.CurrentlyLoggedIn = false;
                viewFlipper.DisplayedChild = ChildLoginForm;
            }
            else if (!SeekerState.SoulseekClient.State.HasFlag(SoulseekClientStates.LoggedIn) && !s_loginInFlight)
            {
                viewFlipper.DisplayedChild = ChildLoading;
                SeekerState.ManualResetEvent.Reset();
                Task login = null;
                try
                {
                    login = SeekerApplication.ConnectAndPerformPostConnectTasks(PreferencesState.Username, PreferencesState.Password);
                }
                catch (InvalidOperationException)
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.we_are_already_logging_in), ToastLength.Short);
                    Logger.Firebase("We are already logging in");
                }
                login?.ContinueWith(new Action<Task>((task) => { UpdateLoginUI(task); }));
                login?.ContinueWith(MainActivity.GetPostNotifPermissionTask());
                SeekerApplication.SetUpLoginContinueWith(login);
            }
            else if (PreferencesState.CurrentlyLoggedIn)
            {
                ShowLoggedIn();
            }
            else
            {
                viewFlipper.DisplayedChild = ChildLoading;
            }

            return rootView;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            SeekerState.ManualResetEvent.Set();
        }

        private void SetUpLoginFormViews()
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

        private void SetUpLoggedInViews()
        {
            mustSelectDirButton = rootView.FindViewById<Button>(Resource.Id.mustSelectDirectory);
            welcomeTextView = rootView.FindViewById<TextView>(Resource.Id.userNameView);
            connectionStatusDot = rootView.FindViewById<View>(Resource.Id.connectionStatusDot);
            connectionStatusText = rootView.FindViewById<TextView>(Resource.Id.connectionStatusText);
            connectionStatusChip = rootView.FindViewById<View>(Resource.Id.connectionStatusChip);

            menuSetUpSharing = rootView.FindViewById<View>(Resource.Id.menuSetUpSharing);
            menuManageUserList = rootView.FindViewById<View>(Resource.Id.menuManageUserList);
            menuMessages = rootView.FindViewById<View>(Resource.Id.menuMessages);
            messagesUnreadBadge = rootView.FindViewById<TextView>(Resource.Id.messagesUnreadBadge);
            menuSettings = rootView.FindViewById<View>(Resource.Id.menuSettings);
            menuLogout = rootView.FindViewById<View>(Resource.Id.menuLogout);

            menuManageUserList.Click += (s, e) =>
            {
                Intent intent = new Intent(SeekerState.MainActivityRef, typeof(UserListActivity));
                SeekerState.MainActivityRef.StartActivityForResult(intent, 141);
            };
            menuMessages.Click += (s, e) =>
            {
                Intent intent = new Intent(SeekerState.MainActivityRef, typeof(MessagesActivity));
                SeekerState.MainActivityRef.StartActivityForResult(intent, 142);
            };
            menuSettings.Click += Settings_Click;
            menuLogout.Click += LogoutClick;
        }

        // --- View-flipping methods ---

        public void ShowLoginForm(bool prefill)
        {
            var action = new Action(() =>
            {
                if (prefill && !string.IsNullOrEmpty(PreferencesState.Username))
                {
                    usernameTextEdit.Text = PreferencesState.Username;
                    passwordTextEdit.Text = PreferencesState.Password;
                }
                else
                {
                    usernameTextEdit.Text = string.Empty;
                    passwordTextEdit.Text = string.Empty;
                }
                viewFlipper.DisplayedChild = ChildLoginForm;
            });
            if (MainActivity.OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }
        public void ShowLoading()
        {
            var action = new Action(() =>
            {
                viewFlipper.DisplayedChild = ChildLoading;
            });
            if (MainActivity.OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        public void ShowLoggedIn()
        {
            var action = new Action(() =>
            {
                welcomeTextView.Text = PreferencesState.Username;
                UpdateConnectionStatus(SeekerState.SoulseekClient.State);

                UpdateUnreadBadge();

                if (UploadDirectoryManager.UploadDirectories == null || UploadDirectoryManager.UploadDirectories.Count == 0)
                {
                    menuSetUpSharing.Visibility = ViewStates.Visible;
                }
                else
                {
                    menuSetUpSharing.Visibility = ViewStates.Gone;
                }

                viewFlipper.DisplayedChild = ChildLoggedIn;
            });
            if (MainActivity.OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        public void UpdateConnectionStatus(SoulseekClientStates state)
        {
            int textResId;
            int dotColorResId;
            int textColorResId;
            int chipBgColorResId;

            if (state.HasFlag(SoulseekClientStates.LoggedIn))
            {
                textResId = Resource.String.status_connected;
                dotColorResId = Resource.Color.statusConnectedDot;
                textColorResId = Resource.Color.statusConnectedText;
                chipBgColorResId = Resource.Color.statusConnectedChipBg;
            }
            else if (state.HasFlag(SoulseekClientStates.Connecting) || state.HasFlag(SoulseekClientStates.LoggingIn))
            {
                textResId = Resource.String.status_connecting;
                dotColorResId = Resource.Color.statusConnectingDot;
                textColorResId = Resource.Color.statusConnectingText;
                chipBgColorResId = Resource.Color.statusConnectingChipBg;
            }
            else
            {
                if (state.HasFlag(SoulseekClientStates.Disconnecting))
                {
                    textResId = Resource.String.status_disconnecting;
                }
                else
                {
                    textResId = Resource.String.status_disconnected;
                }
                dotColorResId = Resource.Color.statusDisconnectedDot;
                textColorResId = Resource.Color.statusDisconnectedText;
                chipBgColorResId = Resource.Color.statusDisconnectedChipBg;
            }

            var resources = this.Context.Resources;
            int dotColor = resources.GetColor(dotColorResId, this.Context.Theme);
            int textColor = resources.GetColor(textColorResId, this.Context.Theme);
            int chipBgColor = resources.GetColor(chipBgColorResId, this.Context.Theme);

            var dotDrawable = (GradientDrawable)connectionStatusDot.Background;
            dotDrawable.SetColor(dotColor);
            connectionStatusText.Text = SeekerApplication.GetString(textResId);
            connectionStatusText.SetTextColor(new Android.Graphics.Color(textColor));
            var chipBgDrawable = (GradientDrawable)connectionStatusChip.Background;
            chipBgDrawable.SetColor(chipBgColor);
        }

        private void UpdateUnreadBadge()
        {
            int unreadCount = MessageController.GetTotalUnreadCount();
            if (unreadCount > 0)
            {
                messagesUnreadBadge.Text = string.Format(
                    SeekerApplication.GetString(Resource.String.unread_count_fmt), unreadCount);
                messagesUnreadBadge.Visibility = ViewStates.Visible;
            }
            else
            {
                messagesUnreadBadge.Visibility = ViewStates.Gone;
            }
        }

        private void OnMessageReceivedUpdateBadge(object sender, Message msg)
        {
            this.Activity?.RunOnUiThread(() => UpdateUnreadBadge());
        }

        private void OnMarkAsReadUpdateBadge(object sender, string username)
        {
            this.Activity?.RunOnUiThread(() => UpdateUnreadBadge());
        }

        public void ShowMustSelectDirectoryButton(EventHandler clickHandler)
        {
            var action = new Action(() =>
            {
                if (mustSelectDirButton != null)
                {
                    mustSelectDirButton.Visibility = ViewStates.Visible;
                    mustSelectDirButton.Click += clickHandler;
                }
            });
            if (MainActivity.OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        public void HideMustSelectDirectoryButton()
        {
            var action = new Action(() =>
            {
                if (mustSelectDirButton != null)
                {
                    mustSelectDirButton.Visibility = ViewStates.Gone;
                }
            });
            if (MainActivity.OnUIthread())
            {
                action();
            }
            else
            {
                SeekerState.MainActivityRef.RunOnUiThread(action);
            }
        }

        // --- Login/Logout logic ---

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
                SeekerState.MainActivityRef.Window.SetSoftInputMode(SoftInput.AdjustNothing);
            }
            catch (System.Exception err)
            {
                Logger.Firebase("MainActivity_FocusChange" + err.Message);
            }
        }

        private readonly int[] All_Ascii = Enumerable.Range('\x1', 127).ToArray();

        private void UsernamePasswordTextEdit_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            bool hasError = ValidateUsername();
            EnableDisableLoginButton(usernameTextEdit, passwordTextEdit, loginButton, hasError);
        }

        private bool ValidateUsername()
        {
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

        public void Settings_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(SeekerState.MainActivityRef, typeof(SettingsActivity));
            SeekerState.MainActivityRef.StartActivityForResult(intent, 140);
        }

        private void UpdateLoginUI(Task t)
        {
            if (SeekerApplication.DnsLookupFailed && (t != null && t.Status == TaskStatus.Faulted))
            {
                // DNS failed and task also faulted — fall through to error handling below
            }
            else if (SeekerApplication.DnsLookupFailed)
            {
                var action = new Action(() =>
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.dns_failed), ToastLength.Long);
                    Logger.Firebase("DNS Lookup of Server Failed. Falling back on hardcoded IP succeeded.");
                });
                SeekerState.MainActivityRef.RunOnUiThread(action);
                SeekerApplication.DnsLookupFailed = false;
            }

            Logger.Debug("Update Login UI");

            if (t != null && t.Status == TaskStatus.Faulted)
            {
                var (msg, clearCreds) = ClassifyLoginError(t);
                OnLoginFailed(msg, clearCreds);
            }
            else
            {
                OnLoginSucceeded();
            }
        }

        private (string message, bool clearCredentials) ClassifyLoginError(Task t)
        {
            string msg;
            string msgToLog = string.Empty;
            bool clearCreds = true;

            if (t.Exception != null && t.Exception.InnerExceptions != null && t.Exception.InnerExceptions.Count != 0)
            {
                Console.WriteLine(t.Exception.ToString());
                if (t.Exception.InnerExceptions[0] is LoginRejectedException lre)
                {
                    string loginRejectedMessage = lre.Message;
                    if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDUSERNAME"))
                    {
                        msg = SeekerState.ActiveActivityRef.GetString(Resource.String.invalid_username);
                    }
                    else if (loginRejectedMessage != null && loginRejectedMessage.Contains("INVALIDPASS"))
                    {
                        msg = SeekerState.ActiveActivityRef.GetString(Resource.String.invalid_password);
                    }
                    else
                    {
                        msg = SeekerState.ActiveActivityRef.GetString(Resource.String.bad_user_pass);
                    }
                }
                else if (t.Exception.InnerExceptions[0] is SoulseekClientException)
                {
                    clearCreds = false;
                    if (t.Exception.InnerExceptions[0].Message.Contains("Network is unreachable") ||
                        t.Exception.InnerExceptions[0].Message.Contains("Connection refused"))
                    {
                        msg = SeekerState.ActiveActivityRef.GetString(Resource.String.network_unreachable);
                    }
                    else
                    {
                        msg = SeekerState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                        msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                    }
                }
                else if (t.Exception.InnerExceptions[0].Message != null &&
                    (t.Exception.InnerExceptions[0].Message.Contains("wait timed out") || t.Exception.InnerExceptions[0].Message.ToLower().Contains("operation timed out")))
                {
                    clearCreds = false;
                    msg = SeekerState.ActiveActivityRef.GetString(Resource.String.cannot_login) + " - Time Out Waiting for Server Response.";
                }
                else
                {
                    msgToLog = t.Exception.InnerExceptions[0].Message + t.Exception.InnerExceptions[0].StackTrace;
                    clearCreds = false;
                    msg = SeekerState.ActiveActivityRef.GetString(Resource.String.cannot_login);
                }
            }
            else
            {
                if (t.Exception != null)
                {
                    msgToLog = t.Exception.Message + t.Exception.StackTrace;
                }
                msg = SeekerState.ActiveActivityRef.GetString(Resource.String.cannot_login);
            }

            if (msgToLog != string.Empty)
            {
                Logger.Debug(msgToLog);
                Logger.Firebase(msgToLog);
            }

            return (msg, clearCreds);
        }

        private void OnLoginFailed(string msg, bool clearCreds)
        {
            Logger.Debug("Login failed: " + msg);
            s_loginInFlight = false;
            var action = new Action(() =>
            {
                SeekerApplication.Toaster.ShowToast(msg, ToastLength.Long);
                if (clearCreds)
                {
                    PreferencesState.ClearCredentials();
                }
                else
                {
                    PreferencesState.CurrentlyLoggedIn = false;
                }
                ShowLoginForm(prefill: !clearCreds);
            });
            SeekerState.MainActivityRef.RunOnUiThread(action);
        }

        private void OnLoginSucceeded()
        {
            Logger.Debug("Login succeeded");
            PreferencesState.CurrentlyLoggedIn = true;
            s_loginInFlight = false;
            ShowLoggedIn();
        }

        private void LogoutClick(object sender, EventArgs e)
        {
            try
            {
                SeekerState.SoulseekClient.Disconnect(message: LogoutMessage);
            }
            catch
            {
            }
            PreferencesState.ClearCredentials();
            ShowLoginForm(prefill: false);
        }

        public void LogInClick(object sender, EventArgs e)
        {
            bool alreadyConnected = false;
            Task login = null;
            try
            {
                if (string.IsNullOrEmpty(usernameTextEdit.Text) || string.IsNullOrEmpty(passwordTextEdit.Text))
                {
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.no_empty_user_pass), ToastLength.Long);
                    return;
                }
                login = SeekerApplication.ConnectAndPerformPostConnectTasks(usernameTextEdit.Text, passwordTextEdit.Text);
                login?.ContinueWith(MainActivity.GetPostNotifPermissionTask());
                try
                {
                    Android.Views.InputMethods.InputMethodManager imm = (Android.Views.InputMethods.InputMethodManager)(this.Activity).GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(usernameTextEdit.WindowToken, 0);
                }
                catch (System.Exception)
                {
                }
            }
            catch (AddressException)
            {
                SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.dns_failed_2), ToastLength.Long);
                PreferencesState.CurrentlyLoggedIn = false;
                return;
            }
            catch (InvalidOperationException err)
            {
                if (err.Message.Equals("The client is already connected"))
                {
                    alreadyConnected = true;
                    PreferencesState.CurrentlyLoggedIn = true;
                    ShowLoggedIn();
                }
                else
                {
                    SeekerApplication.Toaster.ShowToast(err.Message, ToastLength.Long);
                    PreferencesState.CurrentlyLoggedIn = false;
                    return;
                }
            }
            try
            {
                if (!alreadyConnected)
                {
                    ShowLoading();
                }
                PreferencesState.SetCredentials(usernameTextEdit.Text, passwordTextEdit.Text);
                if (!alreadyConnected)
                {
                    s_loginInFlight = true;
                }
                SeekerState.ManualResetEvent.Reset();
                if (login == null)
                {
                    return;
                }
                login.ContinueWith(new Action<Task>((task) => { UpdateLoginUI(task); }));
            }
            catch (System.Exception ex)
            {
                string message;
                if (ex?.InnerException is LoginRejectedException)
                {
                    message = SeekerState.ActiveActivityRef.GetString(Resource.String.bad_user_pass);
                }
                else
                {
                    message = ex.Message;
                }
                SeekerApplication.Toaster.ShowToast(message, ToastLength.Long);
                PreferencesState.CurrentlyLoggedIn = false;
            }
        }
    }
}
