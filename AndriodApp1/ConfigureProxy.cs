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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1
{
    [Activity(Label = "ConfigureProxy", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = false)]
    public class ConfigureProxyActivity : ThemeableActivity
    {
        public const string USER_INFO_PIC_DIR = "user_info_picture";
        private const int PICTURE_SELECTED = 2001;
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.edit_user_info_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }


        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            if (PendingText == SoulSeekState.UserInfoBio)
            {
                menu.FindItem(Resource.Id.save_user_action).SetVisible(false);
            }
            else
            {
                menu.FindItem(Resource.Id.save_user_action).SetVisible(true);
            }
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.save_user_action:
                    if (PendingText == SoulSeekState.UserInfoBio)
                    {
                        Toast.MakeText(this, this.Resources.GetString(Resource.String.no_changes_to_save), ToastLength.Short).Show();
                    }
                    else
                    {
                        SaveBio();
                        Toast.MakeText(this, this.Resources.GetString(Resource.String.saved), ToastLength.Short).Show();
                        this.InvalidateOptionsMenu();
                    }
                    return true;
                case Resource.Id.view_self_info_action:
                    RequestedUserInfoHelper.LaunchUserInfoView(SoulSeekState.Username);
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void SaveBio()
        {
            SoulSeekState.UserInfoBio = PendingText;
            lock (MainActivity.SHARED_PREF_LOCK)
            {
                var editor = SoulSeekState.SharedPreferences.Edit();
                editor.PutString(SoulSeekState.M_UserInfoBio, PendingText);
                editor.Commit();
            }
        }

        private void OnOkayClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
            force = true;
            this.OnBackPressed();
        }

        private void OnCancelClick(object sender, DialogClickEventArgs e)
        {
            (sender as AndroidX.AppCompat.App.AlertDialog).Dismiss();
        }

        private bool force = false;
        //private AlertDialog diag = null;
        public override void OnBackPressed()
        {
            if (!force && PendingText != SoulSeekState.UserInfoBio)
            {
                force = false;
                var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                var diag = builder.SetMessage(Resource.String.unsaved_changes_body).SetTitle(Resource.String.unsaved_changes_title).SetPositiveButton(Resource.String.okay, OnOkayClick).SetNegativeButton(Resource.String.cancel, OnCancelClick).Create();
                diag.Show();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private TextView pictureText = null;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SoulSeekState.ActiveActivityRef = this;

            //var c = new ContextThemeWrapper(this, Resource.Style.MaterialThemeForChip);
            //ViewGroup itemView = (ViewGroup)LayoutInflater.From(c).Inflate(Resource.Layout.configure_proxy, null, false);
            SetContentView(Resource.Layout.configure_proxy);

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.configure_proxy_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = this.GetString(Resource.String.edit_info);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);

            //EditText editText = this.FindViewById<EditText>(Resource.Id.editTextBio);
            //PendingText = SoulSeekState.UserInfoBio ?? string.Empty;
            //editText.Text = SoulSeekState.UserInfoBio ?? string.Empty;
            //editText.TextChanged += EditText_TextChanged;

            //pictureText = this.FindViewById<TextView>(Resource.Id.user_info_picture_textview);
            //if (SoulSeekState.UserInfoPictureName != null && SoulSeekState.UserInfoPictureName != string.Empty)
            //{
            //    pictureText.Text = SoulSeekState.UserInfoPictureName;
            //}

            //Button selectImage = this.FindViewById<Button>(Resource.Id.buttonSelectImage);
            //selectImage.Click += SelectImage_Click;

            //Button clearImage = this.FindViewById<Button>(Resource.Id.buttonClearImage);
            //clearImage.Click += ClearImage_Click;


        }


        public static string PendingText = string.Empty;
        private void EditText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            bool wasSame = (PendingText == SoulSeekState.UserInfoBio);
            PendingText = e.Text.ToString();
            bool isSame = (PendingText == SoulSeekState.UserInfoBio);
            if (isSame != wasSame)
            {
                this.InvalidateOptionsMenu();
            }
        }
    }
}