using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Search;
using Soulseek;

using Common;

namespace Seeker
{
    // Style A: Chip Row - two lines, flat with bottom cell border
    public class SearchItemViewMediumA : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumA(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_a, this, true);
            setupChildren();
        }

        public SearchItemViewMediumA(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_a, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumA inflate(ViewGroup parent)
        {
            SearchItemViewMediumA itemView = (SearchItemViewMediumA)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediuma_dummy, parent, false);
            return itemView;
        }

        private bool hideLocked = false;

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewBitrate = FindViewById<TextView>(Resource.Id.bitrateTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.queueTextView);
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleSpeed(viewSpeed, (item.UploadSpeed / 1024).ToString() + " kb/s");
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            SearchChipHelper.StyleFileCount(viewFileCount, fcount);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot, item.QueueLength);

        }
    }

    // Style F: Badge Line - two lines only, most compact, flat with separator
    public class SearchItemViewMediumF : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumF(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f, this, true);
            setupChildren();
        }

        public SearchItemViewMediumF(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumF inflate(ViewGroup parent)
        {
            SearchItemViewMediumF itemView = (SearchItemViewMediumF)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumf_dummy, parent, false);
            return itemView;
        }

        private bool hideLocked = false;

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewBitrate = FindViewById<TextView>(Resource.Id.bitrateTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.queueTextView);
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleSpeed(viewSpeed, (item.UploadSpeed / 1024).ToString() + " kb/s");
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            SearchChipHelper.StyleFileCount(viewFileCount, fcount);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot, item.QueueLength);

        }
    }

    // Style A2: Chip Row 2 - bitrate on folder line, right-aligned info
    public class SearchItemViewMediumA2 : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumA2(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_a2, this, true);
            setupChildren();
        }

        public SearchItemViewMediumA2(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_a2, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumA2 inflate(ViewGroup parent)
        {
            SearchItemViewMediumA2 itemView = (SearchItemViewMediumA2)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediuma2_dummy, parent, false);
            return itemView;
        }

        private bool hideLocked = false;

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewBitrate = FindViewById<TextView>(Resource.Id.bitrateTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.queueTextView);
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleSpeed(viewSpeed, (item.UploadSpeed / 1024).ToString() + " kb/s");
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            SearchChipHelper.StyleFileCount(viewFileCount, fcount);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot, item.QueueLength);
        }
    }

    // Style F2: Badge Line 2 - bitrate on folder line, right-aligned info
    public class SearchItemViewMediumF2 : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumF2(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f2, this, true);
            setupChildren();
        }

        public SearchItemViewMediumF2(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f2, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumF2 inflate(ViewGroup parent)
        {
            SearchItemViewMediumF2 itemView = (SearchItemViewMediumF2)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumf2_dummy, parent, false);
            return itemView;
        }

        private bool hideLocked = false;

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewBitrate = FindViewById<TextView>(Resource.Id.bitrateTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.queueTextView);
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleSpeed(viewSpeed, (item.UploadSpeed / 1024).ToString() + " kb/s");
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            SearchChipHelper.StyleFileCount(viewFileCount, fcount);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot, item.QueueLength);
        }
    }
}
