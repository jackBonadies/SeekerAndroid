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
using Android.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndriodApp1
{
    [Activity(Label = "ViewUserInfoActivity", Theme = "@style/AppTheme.NoActionBar")]
    public class ViewUserInfoActivity : Android.Support.V7.App.AppCompatActivity
    {
        public const string USERNAME_TO_VIEW = "USERNAME_TO_VIEW";
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.view_user_info_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            Helpers.SetMenuTitles(menu, UserToView);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (Helpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), UserToView, this, null))
            {
                return true;
            }
            switch (item.ItemId)
            {
                case Resource.Id.browseUsersFiles:

                    //!!!!! TESTING ONLY !!!!!!

                    //System.Threading.Tasks.Task t = new System.Threading.Tasks.Task(() => {System.Threading.Thread.Sleep(5000); });
                    //t.Start();
                    //t.ContinueWith((System.Threading.Tasks.Task t) => {
                    //    SoulSeekState.ActiveActivityRef.RunOnUiThread(() => {
                    //    Google.Android.Material.Snackbar.Snackbar sb = Google.Android.Material.Snackbar.Snackbar.Make(SoulSeekState.ActiveActivityRef.FindViewById<ViewGroup>(Android.Resource.Id.Content), "Test anywhere snackbar", Google.Android.Material.Snackbar.Snackbar.LengthLong).SetAction("Go", (View v)=>{

                    //        RequestedUserInfoHelper.LaunchUserInfoView(SoulSeekState.Username);



                    //    });
                    //sb.Show(); });
                    //});

                    //^^^^^ TESTING ONLY ^^^^^^^



                    //do browse thing...
                    DownloadDialog.RequestFilesApi(UserToView, null, null, null); //im pretty sure this is a bug... no action! unless a default one is used later on..
                    return true;
                case Resource.Id.searchUserFiles:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = this.UserToView;
                    //SearchFragment.SetSearchHintTarget(SearchTarget.ChosenUser); this will never work. custom view is null
                    Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(UserListActivity.IntentUserGoToSearch, 1);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.messageUser:
                    Intent intentMsg = new Intent(SoulSeekState.ActiveActivityRef, typeof(MessagesActivity));
                    intentMsg.AddFlags(ActivityFlags.SingleTop);
                    intentMsg.PutExtra(MessageController.FromUserName, this.UserToView); //so we can go to this user..
                    intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                    this.StartActivity(intentMsg);
                    return true;
                case Resource.Id.addUser:
                    UserListActivity.AddUserAPI(SoulSeekState.ActiveActivityRef, this.UserToView, null); 
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private string UserToView = string.Empty;
        private Soulseek.UserInfo userInfo = null;
        private Soulseek.UserData userData = null;

        private TextView userStats = null;
        private TextView noPicture = null;
        private ImageView picture = null;
        private TextView userBio = null;

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("UserToView", UserToView);
            if(userInfo!=null)
            {
                outState.PutBoolean("UserInfo.HasPicture", userInfo.HasPicture);
                if(userInfo.HasPicture)
                {
                    outState.PutByteArray("UserInfo.Picture",userInfo.Picture);
                }
                outState.PutString("UserInfo.Description", userInfo.Description);
                outState.PutInt("UserInfo.QueueLength", userInfo.QueueLength);
                outState.PutInt("UserInfo.UploadSlots", userInfo.UploadSlots);
                outState.PutBoolean("UserInfo.HasFreeUploadSlot", userInfo.HasFreeUploadSlot);
            }
            if(userData!=null)
            {
                outState.PutInt("userData.AverageSpeed",userData.AverageSpeed);
                outState.PutString("userData.CountryCode", userData.CountryCode);
                outState.PutInt("userData.DirectoryCount", userData.DirectoryCount);
                outState.PutLong("userData.DownloadCount", userData.DownloadCount);
                outState.PutInt("userData.FileCount", userData.FileCount);
                if(userData.SlotsFree.HasValue)
                {
                    outState.PutInt("userData.SlotsFree", userData.SlotsFree.Value);
                }
                outState.PutInt("userData.Status", (int)(userData.Status));
                outState.PutString("userData.Username", userData.Username);
            }
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            RestoreStateFromBundleIfNecessary(savedInstanceState);
            base.OnRestoreInstanceState(savedInstanceState);
        }

        private void RestoreStateFromBundleIfNecessary(Bundle savedInstanceState)
        {
            if (string.IsNullOrEmpty(UserToView) || RequestedUserInfoHelper.GetInfoForUser(UserToView) == null)
            {
                UserToView = savedInstanceState.GetString("UserToView", string.Empty);
                if (RequestedUserInfoHelper.GetInfoForUser(UserToView) == null)
                {

                    if (savedInstanceState.ContainsKey("UserInfo.HasPicture"))
                    {
                        bool hasPic = savedInstanceState.GetBoolean("UserInfo.HasPicture", false);
                        byte[] pic = null;
                        if (hasPic)
                        {
                            pic = savedInstanceState.GetByteArray("UserInfo.Picture");
                        }
                        string desc = savedInstanceState.GetString("UserInfo.Description", string.Empty);
                        int ql = savedInstanceState.GetInt("UserInfo.QueueLength");
                        int uploadSlots = savedInstanceState.GetInt("UserInfo.UploadSlots");
                        bool freeSlot = savedInstanceState.GetBoolean("UserInfo.HasFreeUploadSlot");
                        userInfo = new Soulseek.UserInfo(desc, pic, uploadSlots, ql, freeSlot);
                    }

                    if (savedInstanceState.ContainsKey("userData.AverageSpeed"))
                    {
                        int aspeed = savedInstanceState.GetInt("userData.AverageSpeed");
                        string cc = savedInstanceState.GetString("userData.CountryCode");
                        int dc = savedInstanceState.GetInt("userData.DirectoryCount");
                        long downloadCount = savedInstanceState.GetLong("userData.DownloadCount");
                        int fcount = savedInstanceState.GetInt("userData.FileCount");
                        int? slotsFree = null;
                        if (savedInstanceState.ContainsKey("userData.SlotsFree"))
                        {
                            slotsFree = savedInstanceState.GetInt("userData.SlotsFree", userData.SlotsFree.Value);
                        }
                        Soulseek.UserPresence pres = (Soulseek.UserPresence)(savedInstanceState.GetInt("userData.Status"));
                        string user = savedInstanceState.GetString("userData.Username");
                        userData = new Soulseek.UserData(user, pres, aspeed, downloadCount, fcount, dc, cc, slotsFree);
                    }

                }
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SeekerApplication.SetActivityTheme(this);
            SoulSeekState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.view_user_info_layout);

            Android.Support.V7.Widget.Toolbar myToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.view_user_info_toolbar);
            myToolbar.InflateMenu(Resource.Menu.view_user_info_menu);
            if (Intent != null)
            {
                UserToView = Intent.GetStringExtra(USERNAME_TO_VIEW);
            }
            if (UserToView == null)
            {
                MainActivity.LogFirebase("UserToView==null");
            }
            myToolbar.Title = this.GetString(Resource.String.user_) + " " + UserToView;
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);

            if (UserToView == SoulSeekState.Username)
            {
                //for UserData we only care about Online Status, upload speed, file count, and dir count
                userData = new Soulseek.UserData(UserToView, Soulseek.UserPresence.Online, SoulSeekState.UploadSpeed, 0, SoulSeekState.SharedFileCache?.FileCount ?? 0, SoulSeekState.SharedFileCache?.DirectoryCount ?? 0, "");
                userInfo = SeekerApplication.UserInfoResponseHandler(UserToView, null).Result; //the task is already completed.  (task.fromresult).
            }
            else if(UserToView != null && RequestedUserInfoHelper.GetInfoForUser(UserToView) != null)
            {
                UserListItem uli = RequestedUserInfoHelper.GetInfoForUser(UserToView);
                userInfo = uli.UserInfo; //null ref on uli.
                userData = uli.UserData;
            }
            else
            {
                RestoreStateFromBundleIfNecessary(savedInstanceState);
            }


            userStats = FindViewById<TextView>(Resource.Id.user_info_stats);
            noPicture = FindViewById<TextView>(Resource.Id.user_info_no_picture);
            picture = FindViewById<ImageView>(Resource.Id.user_info_picture);
            userBio = FindViewById<TextView>(Resource.Id.textViewBio);

            this.RegisterForContextMenu(picture);
            picture.LongClick += Picture_LongClick;



            base.OnCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            RequestedUserInfoHelper.UserDataReceivedUI += UserDataReceivedUIHandler;
            SetPictureStatus();
            SetBioStatus();
            SetUserStatsStatus();
            base.OnResume();
        }

        private void UserDataReceivedUIHandler(object sender, Soulseek.UserData _userData)
        {
            userData = _userData;
            SetUserStatsStatus();
        }

        protected override void OnPause()
        {
            RequestedUserInfoHelper.UserDataReceivedUI -= UserDataReceivedUIHandler;
            base.OnPause();
        }

        private void SetUserStatsStatus()
        {
            bool userInfoNull = userInfo == null;
            bool userDataNull = userData == null;
            string speedString = "...";
            if (!userDataNull)
            {
                int speedKbs = userData.AverageSpeed / 1000; //someone got a nullref here.... this can happen if the server is slower to respond than the user...
                speedString = speedKbs.ToString("N0") + " kbs";
            }
            userStats.Text = string.Format(Resources.GetString(Resource.String.user_status_string),userInfo.UploadSlots, userInfo.HasFreeUploadSlot, userInfo.QueueLength, speedString, userDataNull ? "..." : userData.FileCount.ToString("N0"), userDataNull ? "..." : userData.DirectoryCount.ToString("N0"));
        }

        private void SetBioStatus()
        {
            if(userInfo.Description!=null && userInfo.Description!=string.Empty)
            {
                userBio.Text = userInfo.Description;
            }
        }

        private void Picture_LongClick(object sender, View.LongClickEventArgs e) //this is a hack for the legacy "full screen" context menu. (vs (x,y) local context menu)
        {
            e.Handled = true; //else will do the context menu anchored to (x,y)
            (sender as View).ShowContextMenu();
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            //if (v.Id == Resource.Id.user_info_picture)
            //{
                //menu.Add(0, 0, 0, Resource.String.copy_image);
                menu.Add(1, 1, 1, Resource.String.save_image);
            //}
            base.OnCreateContextMenu(menu, v, menuInfo);
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                //case 0: //"Copy Image"
                    //var clipboardManager = this.GetSystemService(Context.ClipboardService) as ClipboardManager;
                    //ClipData clip = ClipData.New("simple text", txt);
                    //clipboardManager.PrimaryClip = clip;
                case 1: //"Save Image"
                    SaveImage(userInfo.Picture);
                    Toast.MakeText(this, Resource.String.success_save, ToastLength.Short).Show();
                    return true;
            }
            return base.OnContextItemSelected(item);
        }

        private Bitmap loadedBitmap = null;
        private string originalImageMimetype = null;
        private void SetPictureStatus()
        {
            if (userInfo.HasPicture)
            {
                noPicture.Visibility = ViewStates.Gone;
                picture.Visibility = ViewStates.Visible;
                Tuple<int, int> dimensions = null;
                string mimeType = null;
                loadedBitmap = LoadBitmapFromImage(out dimensions, out mimeType);
                int h = loadedBitmap.Height;
                originalImageMimetype = mimeType;
                LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(dimensions.Item1, dimensions.Item2);
                layoutParams.Gravity = GravityFlags.CenterHorizontal;
                picture.LayoutParameters = (layoutParams);
                picture.SetImageBitmap(loadedBitmap);
                picture.RequestLayout();
            }
            else
            {
                noPicture.Visibility = ViewStates.Visible;
                picture.Visibility = ViewStates.Gone;
            }
        }

        private Bitmap LoadBitmapFromImage(out Tuple<int, int> widthHeightForImageView, out string mimeType)
        {
            //bounds check
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeByteArray(userInfo.Picture, 0, userInfo.Picture.Length, options);
            int imageHeight = options.OutHeight;
            int imageWidth = options.OutWidth;
            mimeType = options.OutMimeType;

            //pixel 2 screen resolution 1920x1080 pixels = 8MB for fullscreen bitmap
            //pixel 5 screen resolution 1080x2340 pixels = 10MB for fullscreen bitmap
            //samsung galaxy tablet 2560x1600 pixels = 16MB for fullscreen bitmap

            //we are going to try to show the image as large as we can 
            //with the width and height of the device as bounds.


            Android.Util.DisplayMetrics displayMetrics = new Android.Util.DisplayMetrics();
            this.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            int screenHeight = displayMetrics.HeightPixels;
            int screenWidth = displayMetrics.WidthPixels;
            if (screenHeight > screenWidth)
            {
                screenHeight = (int)(screenHeight * .7); //the .7 is arbitrary... we just dont want the picture to be filling up the screen to such an extent..
                screenWidth = (int)(screenWidth * .95);
            }
            else
            {
                screenWidth = (int)(screenWidth * .95); //landscape
                screenHeight = (int)(screenHeight * .95);
            }


            double imageRatio = (double)imageHeight / (double)imageWidth;
            double screenRatio = (double)screenHeight / (double)screenWidth;
            bool limitedByHeight = false;
            if (imageRatio > screenRatio)
            {
                limitedByHeight = true;
            }

            int subsampling = 1;
            if (limitedByHeight)
            {
                subsampling = Math.Max((int)(imageHeight / screenHeight), 1);
            }
            else
            {
                subsampling = Math.Max((int)(imageWidth / screenWidth), 1);
            }

            // Calculate inSampleSize
            options.InSampleSize = subsampling; //in the docs it says this rounds down to nearest power of two.. but it actually does work with any integer value!

            // Decode bitmap with inSampleSize set
            options.InJustDecodeBounds = false;

            int imageViewHeight = 0;
            int imageViewWidth = 0;
            if (limitedByHeight)
            {
                imageViewHeight = screenHeight;
                imageViewWidth = (int)(imageViewHeight / imageRatio);
            }
            else
            {
                imageViewWidth = screenWidth;
                imageViewHeight = (int)(imageViewWidth * imageRatio);
            }
            widthHeightForImageView = new Tuple<int, int>(imageViewWidth, imageViewHeight);
            return BitmapFactory.DecodeByteArray(userInfo.Picture, 0, userInfo.Picture.Length, options);

        }


        private void SaveImage(byte[] pic)
        {
            string ext = '.' + originalImageMimetype.Split('/')[1];
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                ContentValues valuesForContentResolver = GetContentValues();
                valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.DisplayName, UserToView + DateTime.Now.ToString("_yyyyMMdd_hhmmss") + ext);
                valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.RelativePath, "Pictures");
                valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.IsPending, true); //Flag indicating if a media item is pending, and still being inserted by its owner.
                                                                                                               //While this flag is set, only the owner of the item can open the underlying file; requests from other apps will be rejected. 
                                                                                                               // RELATIVE_PATH and IS_PENDING are introduced in API 29.

                Android.Net.Uri uri = this.ContentResolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, valuesForContentResolver);
                SaveToStream(pic, this.ContentResolver.OpenOutputStream(uri));
                valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.IsPending, false);
                this.ContentResolver.Update(uri, valuesForContentResolver, null, null);
            }
            else
            {
                ContentValues valuesForContentResolver = GetContentValues();
                Java.IO.File directory = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory.ToString() + Java.IO.File.Separator + "Pictures");

                if (!directory.Exists())
                {
                    directory.Mkdirs();
                }
                string fileName = UserToView + DateTime.Now.ToString("_yyyyMMdd_hhmmss") + ext;
                Java.IO.File file = new Java.IO.File(directory, fileName);
                SaveToStream(pic, this.ContentResolver.OpenOutputStream(Android.Support.V4.Provider.DocumentFile.FromFile(file).Uri, "w"));


                valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.Data, file.AbsolutePath);
                // .DATA is deprecated in API 29
                this.ContentResolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, valuesForContentResolver);
            }
        }

        private ContentValues GetContentValues() 
        {
            ContentValues valuesForContentResolver = new ContentValues();
            DateTime now = DateTime.Now;
            long ms = new DateTimeOffset(now).ToUnixTimeMilliseconds();
            long s = ms / 1000;
            valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.DateAdded, s);
            valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.DateTaken, ms); //this one is in milliseconds. whereas date added and date modified are in seconds. 
            valuesForContentResolver.Put(Android.Provider.MediaStore.Images.ImageColumns.MimeType, originalImageMimetype);
            return valuesForContentResolver;
        }

        private void SaveToStream(byte[] pic, System.IO.Stream outStream)
        {
            if (outStream != null)
            {
                //try
                //{
                outStream.Write(pic,0,pic.Length);
                outStream.Close();
                //}
                //catch (e: Exception) 
                //{
                //    e.printStackTrace()
                //}
            }
        }

        private void SaveImageToStream(Bitmap bmp, System.IO.Stream outStream)
        {
            if (outStream != null)
            {
                //try
                //{
                bmp.Compress(Bitmap.CompressFormat.Png, 100, outStream);
                outStream.Close();
                //}
                //catch (e: Exception) 
                //{
                //    e.printStackTrace()
                //}
            }
        }
    }
}