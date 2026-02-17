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
using AndroidX.Activity;
using Java.Lang;
using Seeker.Helpers;
using System;
using System.Linq;

using Common;
namespace Seeker
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
            if (PendingText == PreferencesState.UserInfoBio)
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
                    if (PendingText == PreferencesState.UserInfoBio)
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
                    RequestedUserInfoHelper.LaunchUserInfoView(PreferencesState.Username);
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        public void SaveBio()
        {
            PreferencesState.UserInfoBio = PendingText;
            PreferencesManager.SaveUserInfoBio(PendingText);
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
        private void backPressedAction(OnBackPressedCallback callback)
        {
            if (!force && PendingText != PreferencesState.UserInfoBio)
            {
                force = false;
                var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogTheme);
                var diag = builder.SetMessage(Resource.String.unsaved_changes_body).SetTitle(Resource.String.unsaved_changes_title).SetPositiveButton(Resource.String.okay, OnOkayClick).SetNegativeButton(Resource.String.cancel, OnCancelClick).Create();
                diag.Show();
            }
            else
            {
                callback.Enabled = false;
                OnBackPressedDispatcher.OnBackPressed();
                callback.Enabled = true;
            }
        }

        private TextView pictureText = null;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.edit_user_info_layout);

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.edit_user_info_toolbar);
            myToolbar.InflateMenu(Resource.Menu.messages_overview_list_menu);
            myToolbar.Title = this.GetString(Resource.String.edit_info);
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);

            EditText editText = this.FindViewById<EditText>(Resource.Id.editTextBio);
            PendingText = PreferencesState.UserInfoBio ?? string.Empty;
            editText.Text = PreferencesState.UserInfoBio ?? string.Empty;
            editText.TextChanged += EditText_TextChanged;

            pictureText = this.FindViewById<TextView>(Resource.Id.user_info_picture_textview);
            if (PreferencesState.UserInfoPictureName != null && PreferencesState.UserInfoPictureName != string.Empty)
            {
                pictureText.Text = PreferencesState.UserInfoPictureName;
            }

            Button selectImage = this.FindViewById<Button>(Resource.Id.buttonSelectImage);
            selectImage.Click += SelectImage_Click;

            Button clearImage = this.FindViewById<Button>(Resource.Id.buttonClearImage);
            clearImage.Click += ClearImage_Click;

            var editUserInfoOnBackPressed = new GenericOnBackPressedCallback(true, this.backPressedAction);
            OnBackPressedDispatcher.AddCallback(editUserInfoOnBackPressed);
        }

        private void DeleteImage(string image)
        {
            Java.IO.File user_info_dir = new Java.IO.File(this.FilesDir, USER_INFO_PIC_DIR);
            Java.IO.File imageFile = new Java.IO.File(user_info_dir, image);
            imageFile.Delete();
        }

        private void ClearImage_Click(object sender, EventArgs e)
        {
            if (PreferencesState.UserInfoPictureName != null && PreferencesState.UserInfoPictureName != string.Empty)
            {
                DeleteImage(PreferencesState.UserInfoPictureName);
                PreferencesState.UserInfoPictureName = string.Empty;
                pictureText.Text = this.GetString(Resource.String.no_image_chosen);
                PreferencesManager.SaveUserInfoPictureName();
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
                if (ex.Message.Contains(SimpleHelpers.NoDocumentOpenTreeToHandle))
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
                    AndroidX.DocumentFile.Provider.DocumentFile chosenFile = null;
                    if (SeekerState.PreOpenDocumentTree())
                    {
                        chosenFile = AndroidX.DocumentFile.Provider.DocumentFile.FromFile(new Java.IO.File(data.Data.Path));
                    }
                    else
                    {
                        chosenFile = AndroidX.DocumentFile.Provider.DocumentFile.FromSingleUri(this, data.Data);
                    }

                    //for samsung galaxy api 19 chosenFile.Exists() returns false, whether DF.FromFile or DF.FromSingleUri
                    //even tho it returns false it still works completely fine..

                    if (chosenFile == null || (!SeekerState.PreOpenDocumentTree() && !chosenFile.Exists())) //i.e. its not an error if <21 and does not exist.
                    {
                        Logger.Firebase("selected image does not exist !!!!");
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

                    string oldImage = PreferencesState.UserInfoPictureName;
                    if (PreferencesState.UserInfoPictureName != string.Empty && PreferencesState.UserInfoPictureName != null)
                    {
                        DeleteImage(oldImage);  //delete old image in our internal storage.
                    }

                    PreferencesState.UserInfoPictureName = name;
                    PreferencesManager.SaveUserInfoPictureName();
                    Java.IO.File fileForOurInternalStorage = new Java.IO.File(user_info_dir, name);
                    System.IO.Stream outputStream = this.ContentResolver.OpenOutputStream(AndroidX.DocumentFile.Provider.DocumentFile.FromFile(fileForOurInternalStorage).Uri, "w");

                    //this doesnt work btw due to the interal uri not being a SAF uri.
                    //Android.Provider.DocumentsContract.CopyDocument(this.ContentResolver, data.Data, AndroidX.DocumentFile.Provider.DocumentFile.FromFile(f).Uri);

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
                    if (files.Count() != 1)
                    {
                        Logger.Firebase("files.Count!=1");
                    }
                    else if (files[0].Name != PreferencesState.UserInfoPictureName)
                    {
                        Logger.Firebase("files[0].Name != PreferencesState.UserInfoPictureName");
                    }
                    pictureText.Text = PreferencesState.UserInfoPictureName;
                    pictureText.Invalidate();
                    Toast.MakeText(this, this.GetString(Resource.String.success_set_pic), ToastLength.Long).Show();
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
            bool wasSame = (PendingText == PreferencesState.UserInfoBio);
            PendingText = e.Text.ToString();
            bool isSame = (PendingText == PreferencesState.UserInfoBio);
            if (isSame != wasSame)
            {
                this.InvalidateOptionsMenu();
            }
        }
    }
}