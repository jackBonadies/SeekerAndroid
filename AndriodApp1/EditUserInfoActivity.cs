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
    [Activity(Label = "EditUserInfoActivity", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask, Exported = false)]
    public class EditUserInfoActivity : ThemeableActivity
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
                    if(PendingText == SoulSeekState.UserInfoBio)
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
            SetContentView(Resource.Layout.edit_user_info_layout);

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.edit_user_info_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = this.GetString(Resource.String.edit_info);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);

            EditText editText = this.FindViewById<EditText>(Resource.Id.editTextBio);
            PendingText = SoulSeekState.UserInfoBio ?? string.Empty;
            editText.Text = SoulSeekState.UserInfoBio ?? string.Empty;
            editText.TextChanged += EditText_TextChanged;

            pictureText = this.FindViewById<TextView>(Resource.Id.user_info_picture_textview);
            if(SoulSeekState.UserInfoPictureName!=null && SoulSeekState.UserInfoPictureName != string.Empty)
            {
                pictureText.Text = SoulSeekState.UserInfoPictureName;
            }

            Button selectImage = this.FindViewById<Button>(Resource.Id.buttonSelectImage);
            selectImage.Click += SelectImage_Click;

            Button clearImage = this.FindViewById<Button>(Resource.Id.buttonClearImage);
            clearImage.Click += ClearImage_Click;
            
            
        }

        private void DeleteImage(string image)
        {
            Java.IO.File user_info_dir = new Java.IO.File(this.FilesDir, USER_INFO_PIC_DIR);
            Java.IO.File imageFile = new Java.IO.File(user_info_dir, image);
            imageFile.Delete();
        }

        private void ClearImage_Click(object sender, EventArgs e)
        {
            if (SoulSeekState.UserInfoPictureName != null && SoulSeekState.UserInfoPictureName != string.Empty)
            {
                DeleteImage(SoulSeekState.UserInfoPictureName);
                SoulSeekState.UserInfoPictureName = string.Empty;
                pictureText.Text = this.GetString(Resource.String.no_image_chosen);
                lock (MainActivity.SHARED_PREF_LOCK)
                {
                    var editor = SoulSeekState.SharedPreferences.Edit();
                    editor.PutString(SoulSeekState.M_UserInfoPicture, SoulSeekState.UserInfoPictureName);
                    editor.Commit();
                }
            }
            else
            {
                Toast.MakeText(this, this.GetString(Resource.String.no_pic_set), ToastLength.Short).Show();
            }
        }

        private void SelectImage_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            try
            {
                this.StartActivityForResult(Intent.CreateChooser(intent, this.GetString(Resource.String.select_pic)), PICTURE_SELECTED);
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("No Activity found to handle Intent"))
                {
                    Toast.MakeText(this, this.GetString(Resource.String.error_no_file_manager_image), ToastLength.Long).Show();
                }
                else
                {
                    throw ex;
                }
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data) //UI thread.  as all lifecycle methods.
        {
            if (requestCode == PICTURE_SELECTED)
            {
                if (resultCode == Result.Ok)
                {
                    Android.Support.V4.Provider.DocumentFile chosenFile = null;
                    if (SoulSeekState.PreOpenDocumentTree())
                    {
                        chosenFile = Android.Support.V4.Provider.DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                    }
                    else
                    {
                        chosenFile = Android.Support.V4.Provider.DocumentFile.FromSingleUri(this, data.Data);
                    }
                    
                    //for samsung galaxy api 19 chosenFile.Exists() returns false, whether DF.FromFile or DF.FromSingleUri
                    //even tho it returns false it still works completely fine..

                    if (chosenFile == null || (!SoulSeekState.PreOpenDocumentTree() && !chosenFile.Exists())) //i.e. its not an error if <21 and does not exist.
                    {
                        MainActivity.LogFirebase("selected image does not exist !!!!");
                        Toast.MakeText(this, this.GetString(Resource.String.error_image_doesnt_exist), ToastLength.Long).Show();
                        return;
                    }
                    string name = chosenFile.Name;
                    long length = chosenFile.Length(); //bytes

                    //this is the file we send across the network. if its bigger than 5MB it may annoy other users.
                    //obviously as far as actually displaying a bitmap (ours or another users) on our screen, we need to check bounds etc.
                    if (length > 1024 * 1024 * 5)
                    {
                        Toast.MakeText(this, this.GetString(Resource.String.error_image_too_large), ToastLength.Long).Show();
                        return;
                    }
                    System.IO.Stream inputStream = this.ContentResolver.OpenInputStream(data.Data);

                    //copy the file to our internal storage

                    Java.IO.File user_info_dir = new Java.IO.File(this.FilesDir, USER_INFO_PIC_DIR);
                    if (!user_info_dir.Exists())
                    {
                        user_info_dir.Mkdir();
                    }

                    string oldImage = SoulSeekState.UserInfoPictureName;
                    if (SoulSeekState.UserInfoPictureName!=string.Empty && SoulSeekState.UserInfoPictureName!=null)
                    {
                        DeleteImage(oldImage);  //delete old image in our internal storage.
                    }

                    SoulSeekState.UserInfoPictureName = name;
                    lock (MainActivity.SHARED_PREF_LOCK)
                    {
                        var editor = SoulSeekState.SharedPreferences.Edit();
                        editor.PutString(SoulSeekState.M_UserInfoPicture, SoulSeekState.UserInfoPictureName);
                        editor.Commit();
                    }
                    Java.IO.File fileForOurInternalStorage = new Java.IO.File(user_info_dir, name);
                    System.IO.Stream outputStream = this.ContentResolver.OpenOutputStream(Android.Support.V4.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri, "w");

                    //this doesnt work btw due to the interal uri not being a SAF uri.
                    //Android.Provider.DocumentsContract.CopyDocument(this.ContentResolver, data.Data, Android.Support.V4.Provider.DocumentFile.FromFile(f).Uri);

                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = inputStream.Read(buffer)) != 0) //C# does 0 for you've reached the end!
                    {
                        outputStream.Write(buffer, 0, read);
                    }
                    inputStream.Close();
                    outputStream.Flush();
                    outputStream.Close();

                    //get name of this one and delete the last one...

                    //verify that there is 1 file and its ours.
                    var files = user_info_dir.ListFiles();
                    if(files.Count()!=1)
                    {
                        MainActivity.LogFirebase("files.Count!=1");
                    }
                    else if(files[0].Name != SoulSeekState.UserInfoPictureName)
                    {
                        MainActivity.LogFirebase("files[0].Name != SoulSeekState.UserInfoPictureName");
                    }
                    pictureText.Text = SoulSeekState.UserInfoPictureName;
                    pictureText.Invalidate();
                    Toast.MakeText(this, this.GetString(Resource.String.success_set_pic),ToastLength.Long).Show();
                }
                else if (resultCode == Result.Canceled)
                {

                }
            }
            base.OnActivityResult(requestCode, resultCode, data);
        }



        public static string PendingText = string.Empty;
        private void EditText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            bool wasSame = (PendingText == SoulSeekState.UserInfoBio);
            PendingText = e.Text.ToString();
            bool isSame = (PendingText == SoulSeekState.UserInfoBio);
            if(isSame!=wasSame)
            {
                this.InvalidateOptionsMenu();
            }
        }
    }
}