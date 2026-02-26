using Android.Content;
using Android.Views;
using Android.Widget;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Seeker.Helpers;
using System.Linq;

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
                    SeekerApplication.Toaster.ShowToast("Failed to parse link", ToastLength.Long);
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

                    Browse.BrowseService.RequestFilesApi(username, null, action, dirPath);
                    return true;
                case FromSlskLinkCopyLink:
                    CommonHelpers.CopyTextToClipboard(SeekerState.ActiveActivityRef, SimpleHelpers.SlskLinkClickedData);
                    return true;
                case FromSlskLinkDownloadFiles:
                    CommonHelpers.ParseSlskLinkString(SimpleHelpers.SlskLinkClickedData, out string _username, out string _dirPath, out string fullFilePath, out bool isFile);
                    Action<Task<IReadOnlyCollection<Directory>>> ContAction = null;
                    if (isFile)
                    {
                        ContAction = (Task<IReadOnlyCollection<Directory>> t) => { Browse.BrowseService.DownloadFilesLogic(t, _username, fullFilePath); };
                    }
                    else
                    {
                        ContAction = (Task<IReadOnlyCollection<Directory>> t) => { Browse.BrowseService.DownloadFilesLogic(t, _username, null); };
                    }
                    Browse.BrowseService.GetFolderContentsAPI(_username, _dirPath, false, ContAction);
                    return true;
            }
            return base.OnContextItemSelected(item);
        }
    }
}