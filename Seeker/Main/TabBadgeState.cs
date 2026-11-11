using Seeker.Browse;
using Seeker.Helpers;
using Seeker.Services;
using System;

namespace Seeker
{
    // "new since you last looked" dots on the Transfers and Browse tabs
    // in memory only (for now, might revisit)
    public static class TabBadgeState
    {
        public const int TransfersPage = 2;
        public const int BrowsePage = 3;

        // pager page of the live MainActivity (kept while it is stopped), -1 if there is none
        public static volatile int VisiblePage = -1;

        private static volatile bool newDownloads;
        private static volatile bool newUploads;
        private static volatile bool newBrowse;

        // raised (from any thread) only when a flag actually changes
        public static event EventHandler Changed;

        public static bool HasNewTransfers => newDownloads || newUploads;
        public static bool HasNewBrowse => newBrowse;

        public static void Initialize()
        {
            DownloadService.Instance.DownloadAddedUINotify += (s, e) => MarkDownloadAdded();
            UploadService.TransferAddedUINotify += (s, e) => MarkUploadAdded();
            BrowseService.BrowseResponseReceived += (s, e) => MarkBrowseReceived();
        }

        private static void MarkDownloadAdded()
        {
            // if already there, then ignore
            if (newDownloads || VisiblePage == TransfersPage && !TransfersViewState.Instance.InUploadsMode)
            {
                return;
            }
            newDownloads = true;
            RaiseChanged();
        }

        private static void MarkUploadAdded()
        {
            // if already there, then ignore
            if (newUploads || (VisiblePage == TransfersPage && TransfersViewState.Instance.InUploadsMode))
            {
                return;
            }
            newUploads = true;
            RaiseChanged();
        }

        private static void MarkBrowseReceived()
        {
            // if already there, then ignore
            if (newBrowse || VisiblePage == BrowsePage)
            {
                return;
            }
            newBrowse = true;
            RaiseChanged();
        }

        // UI thread. page selected, mode switched, or activity started
        public static void OnPageVisible(int page)
        {
            VisiblePage = page;
            if (page == TransfersPage)
            {
                newDownloads = false;
                if (TransfersViewState.Instance.InUploadsMode)
                {
                    newUploads = false;
                }
            }
            else if (page == BrowsePage)
            {
                newBrowse = false;
            }
        }

        private static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Logger.FirebaseError("TabBadgeState.Changed handler failed", e);
            }
        }
    }
}
