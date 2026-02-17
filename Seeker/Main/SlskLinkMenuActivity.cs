using Android.Content;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Seeker.Helpers;

namespace Seeker
{
    public class SlskLinkMenuActivity : ThemeableActivity
    {
        public const int FromSlskLinkCopyLink = 78;
        public const int FromSlskLinkBrowseAtLocation = 79;
        //public const int FromSlskLinkDownloadFolder = 80;
        public const int FromSlskLinkDownloadFiles = 81;
        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v is TextView && SimpleHelpers.ShowSlskLinkContextMenu)
            {
                if (!CommonHelpers.ParseSlskLinkString(SimpleHelpers.SlskLinkClickedData, out _, out _, out _, out bool isFile))
                {
                    Toast.MakeText(SeekerState.ActiveActivityRef, "Failed to parse link", ToastLength.Long).Show();
                    base.OnCreateContextMenu(menu, v, menuInfo);
                    return;
                }

                if (isFile)
                {
                    //download file
                    menu.Add(FromSlskLinkDownloadFiles, FromSlskLinkDownloadFiles, 1, Resource.String.DownloadFile);
                    //show containing folder
                }
                else
                {
                    //download folder
                    menu.Add(FromSlskLinkDownloadFiles, FromSlskLinkDownloadFiles, 1, this.GetString(Resource.String.download_folder));
                    //show folder
                }
                menu.Add(FromSlskLinkBrowseAtLocation, FromSlskLinkBrowseAtLocation, 2, this.GetString(Resource.String.browse_at_location));
                menu.Add(FromSlskLinkCopyLink, FromSlskLinkCopyLink, 3, Resource.String.CopyLink);
            }
            base.OnCreateContextMenu(menu, v, menuInfo);
        }

        public override void OnContextMenuClosed(IMenu menu)
        {
            SimpleHelpers.ShowSlskLinkContextMenu = false;
            base.OnContextMenuClosed(menu);
        }

        //public static void DownloadFilesActionEntry(Task<Directory> dirTask)
        //{
        //    DownloadFilesLogic(dirTask,null);
        //}

        public static void DownloadFilesLogic(Task<Directory> dirTask, string _uname, string thisFileOnly = null)
        {
            if (dirTask.IsFaulted)
            {
                //failed to follow link..
                if (dirTask.Exception?.InnerException?.Message != null)
                {
                    string msgToToast = string.Empty;
                    if (dirTask.Exception.InnerException.Message.ToLower().Contains("timed out"))
                    {
                        msgToToast = "Failed to Add Download - Request timed out";
                    }
                    else if (dirTask.Exception.InnerException.Message.ToLower().Contains(Soulseek.SoulseekClient.FailedToEstablishDirectOrIndirectStringLower))
                    {
                        msgToToast = $"Failed to Add Download - Cannot establish connection to user {_uname}";
                    }
                    else if (dirTask.Exception.InnerException is Soulseek.UserOfflineException)
                    {
                        msgToToast = $"Failed to Add Download - User {_uname} is offline";
                    }
                    else
                    {
                        msgToToast = "Failed to follow link";
                    }
                    Logger.Debug(dirTask.Exception.InnerException.Message);
                    SeekerState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, msgToToast, ToastLength.Short).Show();
                    });
                }
                Logger.Debug("DirectoryReceivedContAction faulted");
            }
            else
            {
                List<FullFileInfo> fullFileInfos = new List<FullFileInfo>();

                //the filenames for these files are NOT the fullname.
                //the fullname is dirTask.Result.Name "\\" f.Filename
                var directory = dirTask.Result;
                foreach (var f in directory.Files)
                {
                    string fullFilename = directory.Name + "\\" + f.Filename;
                    if (thisFileOnly == null)
                    {
                        fullFileInfos.Add(new FullFileInfo() { Depth = 1, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                    }
                    else
                    {
                        if (fullFilename == thisFileOnly)
                        {
                            //add
                            fullFileInfos.Add(new FullFileInfo() { Depth = 1, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                            break;
                        }
                    }
                }


                if (fullFileInfos.Count == 0)
                {
                    if (thisFileOnly == null)
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the file exists and is not locked.", ToastLength.Short).Show();
                    }
                    else
                    {
                        Toast.MakeText(SeekerState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the directory contains files and they are not locked.", ToastLength.Short).Show();
                    }
                    return;
                }

                BrowseFragment.DownloadListOfFiles(fullFileInfos, false, _uname);


            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case FromSlskLinkBrowseAtLocation: //Browse At Location
                    CommonHelpers.ParseSlskLinkString(SimpleHelpers.SlskLinkClickedData, out string username, out string dirPath, out _, out _);
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SeekerState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((AndroidX.ViewPager.Widget.ViewPager)(SeekerState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    DownloadDialog.RequestFilesApi(username, null, action, dirPath);
                    return true;
                case FromSlskLinkCopyLink:
                    CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, SimpleHelpers.SlskLinkClickedData);
                    return true;
                case FromSlskLinkDownloadFiles:
                    CommonHelpers.ParseSlskLinkString(SimpleHelpers.SlskLinkClickedData, out string _username, out string _dirPath, out string fullFilePath, out bool isFile);
                    Action<Task<Directory>> ContAction = null;
                    if (isFile)
                    {
                        ContAction = (Task<Directory> t) => { DownloadFilesLogic(t, _username, fullFilePath); };
                    }
                    else
                    {
                        ContAction = (Task<Directory> t) => { DownloadFilesLogic(t, _username, null); };
                    }
                    DownloadDialog.GetFolderContentsAPI(_username, _dirPath, false, ContAction);
                    return true;
            }
            return base.OnContextItemSelected(item);
        }
    }
}