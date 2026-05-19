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

using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Messages;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Threading.Tasks;

using Common;
namespace Seeker
{
    [Activity(Label = "ViewUserInfoActivity", Theme = "@style/AppTheme.NoActionBar", Exported = false)]
    public class ViewUserInfoActivity : ThemeableActivity
    {
        public const string USERNAME_TO_VIEW = "USERNAME_TO_VIEW";
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.view_user_info_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            UiHelpers.SetMenuTitles(menu, UserToView);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (UiHelpers.HandleCommonContextMenuActions(item.TitleFormatted.ToString(), UserToView, this, null))
            {
                return true;
            }
            switch (item.ItemId)
            {
                case Resource.Id.browseUsersFiles:
                    BrowseService.RequestFilesApi(UserToView, null); 
                    return true;
                case Resource.Id.searchUserFiles:
                    SearchTabHelper.SearchTarget = SearchTarget.ChosenUser;
                    SearchTabHelper.SearchTargetChosenUser = this.UserToView;
                    Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                    intent.PutExtra(MainActivity.GoToSearchExtra, true);
                    this.StartActivity(intent);
                    return true;
                case Resource.Id.messageUser:
                    Intent intentMsg = new Intent(SeekerState.ActiveActivityRef, typeof(MessagesActivity));
                    intentMsg.AddFlags(ActivityFlags.SingleTop);
                    intentMsg.PutExtra(MessageController.FromUserName, this.UserToView); //so we can go to this user..
                    intentMsg.PutExtra(MessageController.ComingFromMessageTapped, true); //so we can go to this user..
                    this.StartActivity(intentMsg);
                    return true;
                case Resource.Id.addUser:
                    UserListService.AddUserAPI(SeekerState.ActiveActivityRef, this.UserToView, null);
                    return true;
                case Android.Resource.Id.Home:
                    OnBackPressedDispatcher.OnBackPressed();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private string UserToView = string.Empty;
        private Soulseek.UserInfo userInfo = null;
        private Soulseek.UserData userData = null;

        private TextView statUploadSlotsValue, statQueuedValue, statSpeedValue, statFilesValue, statDirsValue, statFreeUploadValue;
        private View shimmerSpeed, shimmerFiles, shimmerDirs;
        private ObjectAnimator shimmerAnimSpeed, shimmerAnimFiles, shimmerAnimDirs;

        private View noPicture = null;
        private TextView noPictureText = null;
        private ImageView picture = null;
        private TextView userBio = null;
        private bool pictureFailedToLoad = false;
        // Independent of userInfo.HasPicture, which is derived from picture != null on the
        // stripped UserInfo we hold. This flag tracks "the peer originally sent a picture",
        // sourced from UserListItem.HasPicture / the bundle / a self-user's UserInfo.
        private bool hasPicture = false;
        private bool pictureLoadInFlight = false;

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("UserToView", UserToView);
            if (userInfo != null)
            {
                // Picture bytes are not in the bundle — they live in UserInfoPictureCacheService
                // keyed by username, and survive process death via the disk tier.
                outState.PutBoolean("UserInfo.HasPicture", hasPicture);
                outState.PutString("UserInfo.Description", userInfo.Description);
                outState.PutInt("UserInfo.QueueLength", userInfo.QueueLength);
                outState.PutInt("UserInfo.UploadSlots", userInfo.UploadSlots);
                outState.PutBoolean("UserInfo.HasFreeUploadSlot", userInfo.HasFreeUploadSlot);
            }
            if (userData != null)
            {
                outState.PutInt("userData.AverageSpeed", userData.AverageSpeed);
                outState.PutString("userData.CountryCode", userData.CountryCode);
                outState.PutInt("userData.DirectoryCount", userData.DirectoryCount);
                outState.PutLong("userData.DownloadCount", userData.UploadCount);
                outState.PutInt("userData.FileCount", userData.FileCount);
                if (userData.SlotsFree.HasValue)
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
                        hasPicture = savedInstanceState.GetBoolean("UserInfo.HasPicture", false);
                        string desc = savedInstanceState.GetString("UserInfo.Description", string.Empty);
                        int ql = savedInstanceState.GetInt("UserInfo.QueueLength");
                        int uploadSlots = savedInstanceState.GetInt("UserInfo.UploadSlots");
                        bool freeSlot = savedInstanceState.GetBoolean("UserInfo.HasFreeUploadSlot");
                        // Picture bytes (if any) come from the cache by username at render time.
                        userInfo = new Soulseek.UserInfo(desc, uploadSlots, ql, freeSlot, picture: null);
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
            base.OnCreate(savedInstanceState);

            SeekerState.ActiveActivityRef = this;
            SetContentView(Resource.Layout.view_user_info_layout);

            AndroidX.AppCompat.Widget.Toolbar myToolbar = (AndroidX.AppCompat.Widget.Toolbar)FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.view_user_info_toolbar);
            myToolbar.InflateMenu(Resource.Menu.view_user_info_menu);
            if (Intent != null)
            {
                UserToView = Intent.GetStringExtra(USERNAME_TO_VIEW);
            }
            if (UserToView == null)
            {
                Logger.Firebase("UserToView==null");
            }
            myToolbar.Title = UserToView;
            this.SetSupportActionBar(myToolbar);
            this.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            this.SupportActionBar.SetHomeButtonEnabled(true);

            if (UserToView == PreferencesState.Username)
            {
                // viewing self
                //for UserData we only care about Online Status, upload speed, file count, and dir count
                userData = new Soulseek.UserData(UserToView, Soulseek.UserPresence.Online, PreferencesState.UploadSpeed, 0, SeekerState.SharedFileCache?.FileCount ?? 0, SeekerState.SharedFileCache?.DirectoryCount ?? 0, "");
                userInfo = UserInfoResponder.HandleRequest(UserToView, null).Result; //the task is already completed.  (task.fromresult).
                hasPicture = userInfo != null && userInfo.HasPicture && userInfo.Picture != null && userInfo.Picture.Length > 0;
            }
            else if (UserToView != null && RequestedUserInfoHelper.GetInfoForUser(UserToView) != null)
            {
                UserListItem uli = RequestedUserInfoHelper.GetInfoForUser(UserToView);
                userInfo = uli.UserInfo; //null ref on uli.
                userData = uli.UserData;
                hasPicture = uli.HasPicture;
            }
            else
            {
                RestoreStateFromBundleIfNecessary(savedInstanceState);
            }

            statUploadSlotsValue = FindViewById<TextView>(Resource.Id.stat_upload_slots_value);
            statQueuedValue = FindViewById<TextView>(Resource.Id.stat_queued_value);
            statSpeedValue = FindViewById<TextView>(Resource.Id.stat_speed_value);
            statFilesValue = FindViewById<TextView>(Resource.Id.stat_files_value);
            statDirsValue = FindViewById<TextView>(Resource.Id.stat_dirs_value);
            statFreeUploadValue = FindViewById<TextView>(Resource.Id.stat_free_upload_value);
            shimmerSpeed = FindViewById<View>(Resource.Id.stat_speed_shimmer);
            shimmerFiles = FindViewById<View>(Resource.Id.stat_files_shimmer);
            shimmerDirs = FindViewById<View>(Resource.Id.stat_dirs_shimmer);

            SetupShimmerPlaceholder(shimmerSpeed);
            SetupShimmerPlaceholder(shimmerFiles);
            SetupShimmerPlaceholder(shimmerDirs);

            noPicture = FindViewById(Resource.Id.user_info_no_picture);
            noPictureText = FindViewById<TextView>(Resource.Id.user_info_no_picture_text);
            picture = FindViewById<ImageView>(Resource.Id.user_info_picture);
            userBio = FindViewById<TextView>(Resource.Id.textViewBio);

            this.RegisterForContextMenu(picture);
            picture.LongClick += Picture_LongClick;
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
            RunOnUiThread(() => { SetUserStatsStatus(); });
        }

        protected override void OnPause()
        {
            RequestedUserInfoHelper.UserDataReceivedUI -= UserDataReceivedUIHandler;
            StopShimmerAnimations();
            base.OnPause();
        }

        private void SetUserStatsStatus()
        {
            statUploadSlotsValue.Text = userInfo.UploadSlots.ToString();
            statQueuedValue.Text = userInfo.QueueLength.ToString();

            if (userInfo.HasFreeUploadSlot)
            {
                statFreeUploadValue.Text = SeekerApplication.GetString(Resource.String.yes);
                statFreeUploadValue.SetTextColor(Color.ParseColor("#4CAF50"));
            }
            else
            {
                statFreeUploadValue.Text = SeekerApplication.GetString(Resource.String.No);
                statFreeUploadValue.SetTextColor(Color.ParseColor("#F44336"));
            }

            if (userData == null)
            {
                shimmerSpeed.Visibility = ViewStates.Visible;
                shimmerFiles.Visibility = ViewStates.Visible;
                shimmerDirs.Visibility = ViewStates.Visible;
                statSpeedValue.Visibility = ViewStates.Invisible;
                statFilesValue.Visibility = ViewStates.Invisible;
                statDirsValue.Visibility = ViewStates.Invisible;
                StartShimmerAnimations();
            }
            else
            {
                StopShimmerAnimations();
                shimmerSpeed.Visibility = ViewStates.Gone;
                shimmerFiles.Visibility = ViewStates.Gone;
                shimmerDirs.Visibility = ViewStates.Gone;

                statSpeedValue.Text = (userData.AverageSpeed / 1000).ToString("N0") + " KB/s";
                statFilesValue.Text = userData.FileCount.ToString("N0");
                statDirsValue.Text = userData.DirectoryCount.ToString("N0");

                statSpeedValue.Alpha = 0f;
                statFilesValue.Alpha = 0f;
                statDirsValue.Alpha = 0f;
                statSpeedValue.Visibility = ViewStates.Visible;
                statFilesValue.Visibility = ViewStates.Visible;
                statDirsValue.Visibility = ViewStates.Visible;
                statSpeedValue.Animate().Alpha(1f).SetDuration(300).Start();
                statFilesValue.Animate().Alpha(1f).SetDuration(300).Start();
                statDirsValue.Animate().Alpha(1f).SetDuration(300).Start();
            }
        }

        private void SetupShimmerPlaceholder(View view)
        {
            var drawable = new GradientDrawable();
            Color color = UiHelpers.GetColorFromAttribute(this, Resource.Attribute.slightestContrastColor);
            drawable.SetColor(color);
            drawable.SetCornerRadius(TypedValue.ApplyDimension(ComplexUnitType.Dip, 6, Resources.DisplayMetrics));
            view.Background = drawable;
        }

        private ObjectAnimator CreateShimmerAnimator(View target)
        {
            var animator = ObjectAnimator.OfFloat(target, "alpha", 1f, 0.3f);
            animator.SetDuration(800);
            animator.RepeatCount = ValueAnimator.Infinite;
            animator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            animator.SetInterpolator(new Android.Views.Animations.AccelerateDecelerateInterpolator());
            return animator;
        }

        private void StartShimmerAnimations()
        {
            StopShimmerAnimations();
            shimmerAnimSpeed = CreateShimmerAnimator(shimmerSpeed);
            shimmerAnimFiles = CreateShimmerAnimator(shimmerFiles);
            shimmerAnimDirs = CreateShimmerAnimator(shimmerDirs);
            shimmerAnimSpeed.Start();
            shimmerAnimFiles.Start();
            shimmerAnimDirs.Start();
        }

        private void StopShimmerAnimations()
        {
            if (shimmerAnimSpeed != null)
            {
                shimmerAnimSpeed.Cancel();
                shimmerAnimSpeed = null;
            }
            if (shimmerAnimFiles != null)
            {
                shimmerAnimFiles.Cancel();
                shimmerAnimFiles = null;
            }
            if (shimmerAnimDirs != null)
            {
                shimmerAnimDirs.Cancel();
                shimmerAnimDirs = null;
            }
        }

        private void SetBioStatus()
        {
            if (userInfo.Description != null && userInfo.Description != string.Empty)
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
            switch (item.ItemId)
            {
                //case 0: //"Copy Image"
                //var clipboardManager = this.GetSystemService(Context.ClipboardService) as ClipboardManager;
                //ClipData clip = ClipData.New("simple text", txt);
                //clipboardManager.PrimaryClip = clip;
                case 1: //"Save Image"
                    SaveImage(userInfo.Picture);
                    SeekerApplication.Toaster.ShowToast(SeekerApplication.GetString(Resource.String.success_save), ToastLength.Short);
                    return true;
            }
            return base.OnContextItemSelected(item);
        }

        private Bitmap loadedBitmap = null;
        private string originalImageMimetype = null;

        private struct PictureLoadResult
        {
            public byte[] Bytes;        // null = cache miss
            public Bitmap Bitmap;       // null with Bytes != null = decode failed
            public int ViewWidth;
            public int ViewHeight;
            public string MimeType;
        }

        private void SetPictureStatus()
        {
            if (!hasPicture || string.IsNullOrEmpty(UserToView))
            {
                pictureFailedToLoad = false;
                ShowNoPictureView();
                return;
            }

            if (loadedBitmap != null)
            {
                // Already decoded earlier in this activity instance — just re-assert visibility
                // (e.g. coming back through OnResume after pause without rotation).
                noPicture.Visibility = ViewStates.Gone;
                picture.Visibility = ViewStates.Visible;
                return;
            }

            if (pictureLoadInFlight)
            {
                return;
            }

            // WindowManager.DefaultDisplay must be touched on the UI thread; capture the
            // metrics here and pass them into the worker so the decode can run off-thread.
            var displayMetrics = new Android.Util.DisplayMetrics();
            this.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            int screenWidthPx = displayMetrics.WidthPixels;
            int screenHeightPx = displayMetrics.HeightPixels;

            // Hide both views during the load. A few-hundred-KB read + decode is typically
            // fast enough that a placeholder would just flicker.
            noPicture.Visibility = ViewStates.Gone;
            picture.Visibility = ViewStates.Gone;

            pictureLoadInFlight = true;
            string requestedUser = UserToView;
            var uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            byte[] inMemory = userInfo?.Picture;
            if (inMemory != null && inMemory.Length > 0)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var result = DecodeBitmap(inMemory, screenWidthPx, screenHeightPx);
                        result.Bytes = inMemory;
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Logger.Firebase("Failed to decode user picture asynchronously: " + ex.Message);
                        return default(PictureLoadResult);
                    }
                })
                .ContinueWith(t => ApplyPictureLoadResult(t.Result, requestedUser), uiScheduler);
                return;
            }

            Services.UserInfoPictureCacheService.Instance.GetAsync(requestedUser)
                .ContinueWith(getTask =>
                {
                    try
                    {
                        byte[] bytes = getTask.Result;
                        if (bytes == null)
                        {
                            return default(PictureLoadResult);
                        }
                        var result = DecodeBitmap(bytes, screenWidthPx, screenHeightPx);
                        result.Bytes = bytes;
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Logger.Firebase("Failed to decode user picture asynchronously: " + ex.Message);
                        return default(PictureLoadResult);
                    }
                }, TaskScheduler.Default)
                .ContinueWith(t => ApplyPictureLoadResult(t.Result, requestedUser), uiScheduler);
        }

        private void ApplyPictureLoadResult(PictureLoadResult result, string requestedUser)
        {
            pictureLoadInFlight = false;
            if (IsFinishing || IsDestroyed)
            {
                return;
            }
            if (!string.Equals(requestedUser, UserToView, StringComparison.Ordinal))
            {
                // UserToView changed under us — drop this stale result.
                return;
            }
            if (result.Bytes == null)
            {
                pictureFailedToLoad = true;
                ShowNoPictureView();
                return;
            }
            if (result.Bitmap == null)
            {
                // The picture is just a byte array another user sent — could be a .svg,
                // .txt, etc. Nicotine lets peers attach any file. Surface decode failure.
                SeekerApplication.Toaster.ShowToast("Failed to decode the user's picture.", ToastLength.Long);
                string uname = userData != null ? userData.Username : "no user";
                Logger.Firebase("FAILURE TO DECODE USERS PICTURE " + uname);
                pictureFailedToLoad = true;
                ShowNoPictureView();
                return;
            }

            if (userInfo != null)
            {
                userInfo = userInfo.WithPicture(result.Bytes);
            }
            loadedBitmap = result.Bitmap;
            originalImageMimetype = result.MimeType;

            noPicture.Visibility = ViewStates.Gone;
            picture.Visibility = ViewStates.Visible;
            var layoutParams = new LinearLayout.LayoutParams(result.ViewWidth, result.ViewHeight);
            layoutParams.Gravity = GravityFlags.CenterHorizontal;
            picture.LayoutParameters = layoutParams;
            picture.SetImageBitmap(loadedBitmap);
            picture.RequestLayout();
        }

        private void ShowNoPictureView()
        {
            noPicture.Visibility = ViewStates.Visible;
            picture.Visibility = ViewStates.Gone;
            if (noPictureText != null)
            {
                noPictureText.SetText(pictureFailedToLoad
                    ? Resource.String.user_picture_failed_to_load
                    : Resource.String.user_has_no_pic);
            }
        }

        // Pure decode + sizing math. No Android.Views.WindowManager access — caller must pass
        // screen dimensions captured on the UI thread so this can run on a worker.
        // Pixel 2 1920x1080 = 8MB fullscreen bitmap; Pixel 5 1080x2340 = 10MB; tablet 2560x1600 = 16MB.
        // Result.Bitmap may be null if the bytes are not a decodable image.
        private static PictureLoadResult DecodeBitmap(byte[] pictureBytes, int screenWidthPx, int screenHeightPx)
        {
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.InJustDecodeBounds = true;
            BitmapFactory.DecodeByteArray(pictureBytes, 0, pictureBytes.Length, options);
            int imageHeight = options.OutHeight;
            int imageWidth = options.OutWidth;
            string mimeType = options.OutMimeType;

            int screenHeight = screenHeightPx;
            int screenWidth = screenWidthPx;
            if (screenHeight > screenWidth)
            {
                screenHeight = (int)(screenHeight * .7); //arbitrary; keeps the picture from filling the screen.
                screenWidth = (int)(screenWidth * .95);
            }
            else
            {
                screenWidth = (int)(screenWidth * .95);
                screenHeight = (int)(screenHeight * .95);
            }

            double imageRatio = (double)imageHeight / (double)imageWidth;
            double screenRatio = (double)screenHeight / (double)screenWidth;
            bool limitedByHeight = imageRatio > screenRatio;

            int subsampling = 1;
            if (limitedByHeight)
            {
                subsampling = Math.Max((int)(imageHeight / screenHeight), 1);
            }
            else
            {
                subsampling = Math.Max((int)(imageWidth / screenWidth), 1);
            }
            // Docs say this rounds to nearest power of two, but any integer actually works.
            options.InSampleSize = subsampling;
            options.InJustDecodeBounds = false;

            int imageViewHeight, imageViewWidth;
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
            return new PictureLoadResult
            {
                Bitmap = BitmapFactory.DecodeByteArray(pictureBytes, 0, pictureBytes.Length, options),
                ViewWidth = imageViewWidth,
                ViewHeight = imageViewHeight,
                MimeType = mimeType,
            };
        }


        private void SaveImage(byte[] pic)
        {
            string ext = '.' + originalImageMimetype.Split('/')[1];
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                ContentValues valuesForContentResolver = GetContentValues();
                valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, UserToView + SimpleHelpers.GetDateTimeNowSafe().ToString("_yyyyMMdd_hhmmss") + ext);
                valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Pictures");
                valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, true); //Flag indicating if a media item is pending, and still being inserted by its owner.
                                                                                                          //While this flag is set, only the owner of the item can open the underlying file; requests from other apps will be rejected.
                                                                                                          // RELATIVE_PATH and IS_PENDING are introduced in API 29.

                Android.Net.Uri uri = this.ContentResolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, valuesForContentResolver);
                SaveToStream(pic, this.ContentResolver.OpenOutputStream(uri));
                valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, false);
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
                string fileName = UserToView + SimpleHelpers.GetDateTimeNowSafe().ToString("_yyyyMMdd_hhmmss") + ext;
                Java.IO.File file = new Java.IO.File(directory, fileName);
                SaveToStream(pic, this.ContentResolver.OpenOutputStream(AndroidX.DocumentFile.Provider.DocumentFile.FromFile(file).Uri, "w"));


                valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.Data, file.AbsolutePath);
                // .DATA is deprecated in API 29
                this.ContentResolver.Insert(Android.Provider.MediaStore.Images.Media.ExternalContentUri, valuesForContentResolver);
            }
        }

        private ContentValues GetContentValues()
        {
            ContentValues valuesForContentResolver = new ContentValues();
            DateTime now = SimpleHelpers.GetDateTimeNowSafe();
            long ms = new DateTimeOffset(now).ToUnixTimeMilliseconds();
            long s = ms / 1000;
            valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.DateAdded, s);
            valuesForContentResolver.Put(Android.Provider.MediaStore.Images.IImageColumns.DateTaken, ms); //this one is in milliseconds. whereas date added and date modified are in seconds.
            valuesForContentResolver.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, originalImageMimetype);
            return valuesForContentResolver;
        }

        private void SaveToStream(byte[] pic, System.IO.Stream outStream)
        {
            if (outStream != null)
            {
                outStream.Write(pic, 0, pic.Length);
                outStream.Close();
            }
        }
    }
}