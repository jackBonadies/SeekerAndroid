using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Common;
using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Seeker.Search;
using Soulseek;

namespace Seeker
{
    public interface ISearchItemViewBase
    {
        SearchFragment.SearchViewHolder ViewHolder { get; set; }
        void setItem(SearchResponse item, int position);
    }

    public interface IExpandable
    {
        void Expand();
        void Collapse();
    }

    internal static class SearchItemViewExpandableHelper
    {
        public static int GetSeparatorColor(Context ctx)
        {
            TypedArray ta = ctx.ObtainStyledAttributes(new int[] { Resource.Attribute.expandableModernSeparatorColor });
            int c = ta.GetColor(0, Color.Transparent);
            ta.Recycle();
            return c;
        }

        public static void PopulateWithSeparators(LinearLayout container, SearchResponse item, int separatorColor)
        {
            container.RemoveAllViews();
            bool first = true;
            foreach (Soulseek.File f in item.GetFiles(PreferencesState.HideLockedResultsInSearch))
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
                tv.Text = SimpleHelpers.GetFileNameFromFile(f.Filename).ToString();
                tv.SetPadding(0, 4, 0, 4);
                container.AddView(tv);
            }
        }

        public static void PopulatePlain(LinearLayout container, SearchResponse item)
        {
            container.RemoveAllViews();
            foreach (Soulseek.File f in item.GetFiles(PreferencesState.HideLockedResultsInSearch))
            {
                TextView tv = new TextView(SeekerState.MainActivityRef);
                UiHelpers.SetTextColor(tv, SeekerState.MainActivityRef);
                tv.Text = SimpleHelpers.GetFileNameFromFile(f.Filename).ToString();
                container.AddView(tv);
            }
        }
    }

    // Base for the four unified search-result views. Each subclass binds its own layout
    // and contributes setItem (Simple = plain text, Modern = chips) and PopulateFiles
    // (Modern draws separators, Simple does not).
    public abstract class SearchItemViewUnifiedBase : RelativeLayout, ISearchItemViewBase, IExpandable
    {
        protected TextView viewUsername;
        protected TextView viewFoldername;
        protected TextView viewSpeed;
        protected TextView viewFileType;
        protected LinearLayout viewToHideShow;
        protected FrameLayout expandClickArea;
        protected ImageView imageViewExpandable;
        protected View viewNewIndicator;
        protected LinearLayout primaryRow;
        protected bool hideLocked;

        public bool IsExpandable;
        public SearchFragment.SearchAdapterRecyclerVersion AdapterRef;
        public SearchFragment.SearchViewHolder ViewHolder { get; set; }

        protected SearchItemViewUnifiedBase(Context c) : base(c) { }

        public virtual void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            expandClickArea = FindViewById<FrameLayout>(Resource.Id.expandClickArea);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            primaryRow = FindViewById<LinearLayout>(Resource.Id.searchItemPrimaryRow)
                ?? FindViewById<LinearLayout>(Resource.Id.relativeLayout1);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public bool HasNewIndicator => viewNewIndicator != null;

        public void ApplyNewIndicator(bool isNew)
        {
            if (isNew)
            {
                if (viewNewIndicator == null)
                {
                    var bar = new View(Context);
                    float density = Resources.DisplayMetrics.Density;
                    bar.LayoutParameters = new LinearLayout.LayoutParams(
                        (int)(4 * density), LinearLayout.LayoutParams.MatchParent);
                    bar.SetBackgroundColor(UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.colorPrimary));
                    primaryRow.AddView(bar, 0);
                    viewNewIndicator = bar;
                }
                viewNewIndicator.Visibility = ViewStates.Visible;
            }
            else if (viewNewIndicator != null)
            {
                viewNewIndicator.Visibility = ViewStates.Gone;
            }
        }

        public void ApplyExpandableMode()
        {
            if (IsExpandable)
            {
                expandClickArea.Visibility = ViewStates.Visible;
            }
        }

        public abstract void setItem(SearchResponse item, int position);

        public abstract void PopulateFiles(SearchResponse item);

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
        }

        protected void ApplyExpandedState(SearchResponse item, int position)
        {
            bool opposite = AdapterRef.oppositePositions.Contains(position);
            bool shouldExpand = (!SearchFragment.ExpandAllResults && opposite)
                || (SearchFragment.ExpandAllResults && !opposite);
            if (shouldExpand)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                PopulateFiles(item);
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_less_white_32_dp);
            }
            else
            {
                viewToHideShow.Visibility = ViewStates.Gone;
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_more_black_32dp);
            }
        }
    }

    public class SearchItemViewSimpleBottom : SearchItemViewUnifiedBase
    {
        private TextView viewAvailability;

        public SearchItemViewSimpleBottom(Context c) : base(c)
        {
            LayoutInflater.From(c).Inflate(Resource.Layout.search_result_simple_bottom, this, true);
            setupChildren();
        }

        public override void setupChildren()
        {
            base.setupChildren();
            viewAvailability = FindViewById<TextView>(Resource.Id.availability);
        }

        public override void setItem(SearchResponse item, int position)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SimpleHelpers.STRINGS_KBS;
            viewFileType.Text = item.GetDominantFileTypeAndBitRate(hideLocked, out _);
            viewAvailability.Text = item.HasFreeUploadSlot ? string.Empty : item.QueueLength.ToString();
            if (IsExpandable)
            {
                ApplyExpandedState(item, position);
            }
        }

        public override void PopulateFiles(SearchResponse item)
        {
            SearchItemViewExpandableHelper.PopulatePlain(viewToHideShow, item);
        }
    }

    public class SearchItemViewSimpleTop : SearchItemViewUnifiedBase
    {
        private TextView viewAvailability;

        public SearchItemViewSimpleTop(Context c) : base(c)
        {
            LayoutInflater.From(c).Inflate(Resource.Layout.search_result_simple_top, this, true);
            setupChildren();
        }

        public override void setupChildren()
        {
            base.setupChildren();
            viewAvailability = FindViewById<TextView>(Resource.Id.availability);
        }

        public override void setItem(SearchResponse item, int position)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SimpleHelpers.STRINGS_KBS;
            viewFileType.Text = item.GetDominantFileTypeAndBitRate(hideLocked, out _);
            viewAvailability.Text = item.HasFreeUploadSlot ? string.Empty : item.QueueLength.ToString();
            if (IsExpandable)
            {
                ApplyExpandedState(item, position);
            }
        }

        public override void PopulateFiles(SearchResponse item)
        {
            SearchItemViewExpandableHelper.PopulatePlain(viewToHideShow, item);
        }
    }

    public abstract class SearchItemViewModernBase : SearchItemViewUnifiedBase
    {
        protected SearchChip fileTypeChip;
        protected SearchChip queueChip;
        protected TextView viewFileCount;
        protected SearchChipPalette palette;
        protected int separatorColor;

        protected SearchItemViewModernBase(Context c) : base(c) { }

        public override void setupChildren()
        {
            base.setupChildren();
            palette = SearchChipPalette.Get(Context);
            fileTypeChip = new SearchChip(viewFileType);
            queueChip = new SearchChip(FindViewById<TextView>(Resource.Id.queueTextView));
            viewFileCount = FindViewById<TextView>(Resource.Id.fileCountTextView);
            separatorColor = SearchItemViewExpandableHelper.GetSeparatorColor(Context);
        }

        public override void setItem(SearchResponse item, int position)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + " kb/s";
            int fcount = hideLocked ? item.FileCount : item.FileCount + item.LockedFileCount;
            viewFileCount.Text = fcount.ToString();
            SearchChipHelper.StyleFormatChip(fileTypeChip, item.GetDominantFileTypeAndBitRate(hideLocked, out _), palette);
            SearchChipHelper.StyleQueueChip(queueChip, item.HasFreeUploadSlot, item.QueueLength, palette);
            if (IsExpandable)
            {
                ApplyExpandedState(item, position);
            }
        }

        public override void PopulateFiles(SearchResponse item)
        {
            SearchItemViewExpandableHelper.PopulateWithSeparators(viewToHideShow, item, separatorColor);
        }
    }

    public class SearchItemViewModernBottom : SearchItemViewModernBase
    {
        public SearchItemViewModernBottom(Context c) : base(c)
        {
            LayoutInflater.From(c).Inflate(Resource.Layout.search_result_modern_bottom, this, true);
            setupChildren();
        }
    }

    // Compact style. Single-row variant of Modern: only foldername, queue chip
    // (conditional), and file-type chip. Never expandable.
    public class SearchItemViewCompact : SearchItemViewUnifiedBase
    {
        private SearchChip fileTypeChip;
        private SearchChip queueChip;
        private SearchChipPalette palette;

        public SearchItemViewCompact(Context c) : base(c)
        {
            LayoutInflater.From(c).Inflate(Resource.Layout.search_result_compact, this, true);
            setupChildren();
        }

        public override void setupChildren()
        {
            base.setupChildren();
            palette = SearchChipPalette.Get(Context);
            fileTypeChip = new SearchChip(viewFileType);
            queueChip = new SearchChip(FindViewById<TextView>(Resource.Id.queueTextView));
        }

        public override void setItem(SearchResponse item, int position)
        {
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            SearchChipHelper.StyleFormatChip(fileTypeChip, item.GetDominantFileTypeAndBitRate(hideLocked, out _), palette);
            SearchChipHelper.StyleQueueChip(queueChip, item.HasFreeUploadSlot, item.QueueLength, palette);
        }

        public override void PopulateFiles(SearchResponse item)
        {
            // Compact is never expandable.
        }
    }

    public class SearchItemViewModernTop : SearchItemViewModernBase
    {
        public SearchItemViewModernTop(Context c) : base(c)
        {
            LayoutInflater.From(c).Inflate(Resource.Layout.search_result_modern_top, this, true);
            setupChildren();
        }
    }
}
