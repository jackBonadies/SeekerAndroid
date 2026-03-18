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
        private TextView viewSlotQueue;
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
            viewSlotQueue = FindViewById<TextView>(Resource.Id.slotQueueTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = "\u2193 " + (item.UploadSpeed / 1024).ToString() + "kb/s";
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            if (item.HasFreeUploadSlot)
            {
                viewSlotQueue.Visibility = ViewStates.Gone;
            }
            else
            {
                SearchChipHelper.StyleSlotAndQueue(viewSlotQueue, item.HasFreeUploadSlot, item.QueueLength);
            }
        }
    }

    // Style B: Two Line (flat with separator, matches screenshot 2)
    public class SearchItemViewMediumB : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewSlotQueue;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumB(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_b, this, true);
            setupChildren();
        }

        public SearchItemViewMediumB(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_b, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumB inflate(ViewGroup parent)
        {
            SearchItemViewMediumB itemView = (SearchItemViewMediumB)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumb_dummy, parent, false);
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
            viewSlotQueue = FindViewById<TextView>(Resource.Id.slotQueueTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = "\u2193 " + (item.UploadSpeed / 1024).ToString() + "kb/s";
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            if (item.HasFreeUploadSlot)
            {
                viewSlotQueue.Visibility = ViewStates.Gone;
            }
            else
            {
                SearchChipHelper.StyleSlotAndQueue(viewSlotQueue, item.HasFreeUploadSlot, item.QueueLength);
            }
        }
    }

    // Style D: Status Bar (card with all chips on bottom row)
    public class SearchItemViewMediumD : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewNoSlot;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumD(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_d, this, true);
            setupChildren();
        }

        public SearchItemViewMediumD(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_d, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumD inflate(ViewGroup parent)
        {
            SearchItemViewMediumD itemView = (SearchItemViewMediumD)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumd_dummy, parent, false);
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
            viewNoSlot = FindViewById<TextView>(Resource.Id.noSlotTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = "\u2193 " + (item.UploadSpeed / 1024).ToString() + "kb/s";
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot ? 0 : item.QueueLength);
            SearchChipHelper.StyleNoSlotChip(viewNoSlot, item.HasFreeUploadSlot);
        }
    }

    // Style E: Chip Grid - three lines, ALL metadata as chips, flat with separator
    public class SearchItemViewMediumE : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewNoSlot;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumE(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_e, this, true);
            setupChildren();
        }

        public SearchItemViewMediumE(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_e, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumE inflate(ViewGroup parent)
        {
            SearchItemViewMediumE itemView = (SearchItemViewMediumE)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediume_dummy, parent, false);
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
            viewNoSlot = FindViewById<TextView>(Resource.Id.noSlotTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            string speedText = "\u2193 " + (item.UploadSpeed / 1024).ToString() + "kb/s";
            SearchChipHelper.StyleSpeedChip(viewSpeed, speedText);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot ? 0 : item.QueueLength);
            SearchChipHelper.StyleNoSlotChip(viewNoSlot, item.HasFreeUploadSlot);
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
        private TextView viewSlotQueue;
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
            viewSlotQueue = FindViewById<TextView>(Resource.Id.slotQueueTextView);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = "\u2193 " + (item.UploadSpeed / 1024).ToString() + "kb/s";
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            if (item.HasFreeUploadSlot)
            {
                viewSlotQueue.Visibility = ViewStates.Gone;
            }
            else
            {
                SearchChipHelper.StyleSlotAndQueue(viewSlotQueue, item.HasFreeUploadSlot, item.QueueLength);
            }
        }
    }
}
