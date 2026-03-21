using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Search;
using Soulseek;

using Common;

namespace Seeker
{
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

    public class SearchItemViewMediumBadgeBitrateBottom : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumBadgeBitrateBottom(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f, this, true);
            setupChildren();
        }

        public SearchItemViewMediumBadgeBitrateBottom(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumBadgeBitrateBottom inflate(ViewGroup parent)
        {
            SearchItemViewMediumBadgeBitrateBottom itemView = (SearchItemViewMediumBadgeBitrateBottom)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumf_dummy, parent, false);
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

    public class SearchItemViewMediumBadgeBitrateTop : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewMediumBadgeBitrateTop(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f2, this, true);
            setupChildren();
        }

        public SearchItemViewMediumBadgeBitrateTop(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium_f2, this, true);
            setupChildren();
        }

        public static SearchItemViewMediumBadgeBitrateTop inflate(ViewGroup parent)
        {
            SearchItemViewMediumBadgeBitrateTop itemView = (SearchItemViewMediumBadgeBitrateTop)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmediumf2_dummy, parent, false);
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

    public class SearchItemViewExpandableModern : RelativeLayout, ISearchItemViewBase, IExpandable
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewBitrate;
        private TextView viewQueue;
        private TextView viewFileCount;
        private LinearLayout viewToHideShow;
        private View headerFilesSeparator;
        private ImageView imageViewExpandable;

        public SearchFragment.SearchAdapterRecyclerVersion AdapterRef;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        public SearchItemViewExpandableModern(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable_modern, this, true);
            setupChildren();
        }

        public SearchItemViewExpandableModern(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable_modern, this, true);
            setupChildren();
        }

        public static SearchItemViewExpandableModern inflate(ViewGroup parent)
        {
            SearchItemViewExpandableModern itemView = (SearchItemViewExpandableModern)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_result_expandable_modern_dummy, parent, false);
            return itemView;
        }

        private bool hideLocked = false;
        private int separatorColor = 0;

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewBitrate = FindViewById<TextView>(Resource.Id.bitrateTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.queueTextView);
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            headerFilesSeparator = FindViewById<View>(Resource.Id.headerFilesSeparator);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            hideLocked = PreferencesState.HideLockedResultsInSearch;

            TypedArray ta = Context.ObtainStyledAttributes(new int[] { Resource.Attribute.expandableModernSeparatorColor });
            separatorColor = ta.GetColor(0, Color.Transparent);
            ta.Recycle();
        }

        public void setItem(SearchResponse item, int position)
        {
            bool opposite = this.AdapterRef.oppositePositions.Contains(position);

            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleSpeed(viewSpeed, (item.UploadSpeed / 1024).ToString() + " kb/s");
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            SearchChipHelper.StyleFileCount(viewFileCount, fcount);
            SearchChipHelper.StyleFormatAndBitrateChips(viewFileType, viewBitrate, item.GetDominantFileTypeAndBitRate(hideLocked, out _));
            SearchChipHelper.StyleQueueChip(viewQueue, item.HasFreeUploadSlot, item.QueueLength);

            if (!SearchFragment.ExpandAllResults && opposite ||
                SearchFragment.ExpandAllResults && !opposite)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                //headerFilesSeparator.Visibility = ViewStates.Visible;
                PopulateFilesListView(viewToHideShow, item);
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_less_white_32_dp);
            }
            else
            {
                viewToHideShow.Visibility = ViewStates.Gone;
                headerFilesSeparator.Visibility = ViewStates.Gone;
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_more_black_32dp);
            }
        }

        public void PopulateFilesListView(LinearLayout container, SearchResponse item)
        {
            container.RemoveAllViews();
            var files = item.GetFiles(PreferencesState.HideLockedResultsInSearch);
            bool first = true;
            foreach (Soulseek.File f in files)
            {
                if (!first)
                {
                    View sep = new View(SeekerState.MainActivityRef);
                    sep.LayoutParameters = new LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MatchParent, 1);
                    sep.SetBackgroundColor(new Color(separatorColor));
                    container.AddView(sep);
                }
                first = false;

                TextView tv = new TextView(SeekerState.MainActivityRef);
                UiHelpers.SetTextColor(tv, SeekerState.MainActivityRef);
                tv.Text = SimpleHelpers.GetFileNameFromFile(f.Filename);
                tv.SetPadding(0, 4, 0, 4);
                container.AddView(tv);
            }
        }

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
            //headerFilesSeparator.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
            headerFilesSeparator.Visibility = ViewStates.Gone;
        }
    }
}
