using AndriodApp1.Extensions.SearchResponseExtensions;
using AndriodApp1.Helpers;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Provider;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Common;
using Java.IO;
using SlskHelp;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using static Android.Provider.DocumentsContract;
using log = Android.Util.Log;

namespace AndriodApp1
{
    public class SlskLinkMenuActivity : ThemeableActivity
    {
        public const int FromSlskLinkCopyLink = 78;
        public const int FromSlskLinkBrowseAtLocation = 79;
        //public const int FromSlskLinkDownloadFolder = 80;
        public const int FromSlskLinkDownloadFiles = 81;
        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            if (v is TextView && CommonHelpers.ShowSlskLinkContextMenu)
            {
                if (!CommonHelpers.ParseSlskLinkString(CommonHelpers.SlskLinkClickedData, out _, out _, out _, out bool isFile))
                {
                    Toast.MakeText(SoulSeekState.ActiveActivityRef, "Failed to parse link", ToastLength.Long).Show();
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
            CommonHelpers.ShowSlskLinkContextMenu = false;
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
                    MainActivity.LogDebug(dirTask.Exception.InnerException.Message);
                    SoulSeekState.ActiveActivityRef.RunOnUiThread(() =>
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, msgToToast, ToastLength.Short).Show();
                    });
                }
                MainActivity.LogDebug("DirectoryReceivedContAction faulted");
            }
            else
            {
                List<BrowseFragment.FullFileInfo> fullFileInfos = new List<BrowseFragment.FullFileInfo>();

                //the filenames for these files are NOT the fullname.
                //the fullname is dirTask.Result.Name "\\" f.Filename
                var directory = dirTask.Result;
                foreach (var f in directory.Files)
                {
                    string fullFilename = directory.Name + "\\" + f.Filename;
                    if (thisFileOnly == null)
                    {
                        fullFileInfos.Add(new BrowseFragment.FullFileInfo() { Depth = 1, FileName = f.Filename, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                    }
                    else
                    {
                        if (fullFilename == thisFileOnly)
                        {
                            //add
                            fullFileInfos.Add(new BrowseFragment.FullFileInfo() { Depth = 1, FileName = f.Filename, FullFileName = fullFilename, Size = f.Size, wasFilenameLatin1Decoded = f.IsLatin1Decoded, wasFolderLatin1Decoded = directory.DecodedViaLatin1 });
                            break;
                        }
                    }
                }


                if (fullFileInfos.Count == 0)
                {
                    if (thisFileOnly == null)
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the file exists and is not locked.", ToastLength.Short).Show();
                    }
                    else
                    {
                        Toast.MakeText(SoulSeekState.ActiveActivityRef, "Nothing to download. Browse at this location to ensure that the directory contains files and they are not locked.", ToastLength.Short).Show();
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
                    CommonHelpers.ParseSlskLinkString(CommonHelpers.SlskLinkClickedData, out string username, out string dirPath, out _, out _);
                    Action<View> action = new Action<View>((v) =>
                    {
                        Intent intent = new Intent(SoulSeekState.ActiveActivityRef, typeof(MainActivity));
                        intent.PutExtra(UserListActivity.IntentUserGoToBrowse, 3);
                        this.StartActivity(intent);
                        //((Android.Support.V4.View.ViewPager)(SoulSeekState.MainActivityRef.FindViewById(Resource.Id.pager))).SetCurrentItem(3, true);
                    });

                    DownloadDialog.RequestFilesApi(username, null, action, dirPath);
                    return true;
                case FromSlskLinkCopyLink:
                    CommonHelpers.CopyTextToClipboard(SoulSeekState.ActiveActivityRef, CommonHelpers.SlskLinkClickedData);
                    return true;
                case FromSlskLinkDownloadFiles:
                    CommonHelpers.ParseSlskLinkString(CommonHelpers.SlskLinkClickedData, out string _username, out string _dirPath, out string fullFilePath, out bool isFile);
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