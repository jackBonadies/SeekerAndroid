using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Common;
using Common.Search;
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
        protected TextView viewMetadata;
        protected ImageButton downloadActionButton;
        protected LinearLayout viewToHideShow;
        protected FrameLayout expandClickArea;
        protected ImageView imageViewExpandable;
        protected View viewNewIndicator;
        protected ImageView selectionIndicator;
        protected LinearLayout primaryRow;
        protected bool hideLocked;
        protected SearchMetadataLabels metadataLabels;

        public ImageButton DownloadActionButton => downloadActionButton;

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
            viewMetadata = FindViewById<TextView>(Resource.Id.resultMetaTextView);
            downloadActionButton = FindViewById<ImageButton>(Resource.Id.downloadActionButton);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            expandClickArea = FindViewById<FrameLayout>(Resource.Id.expandClickArea);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            selectionIndicator = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            primaryRow = FindViewById<LinearLayout>(Resource.Id.searchItemPrimaryRow)
                ?? FindViewById<LinearLayout>(Resource.Id.relativeLayout1);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
            metadataLabels = SearchMetadataLabels.Create(
                Context.GetString(Resource.String.search_result_bitrate_unit),
                Context.GetString(Resource.String.search_result_speed_unit),
                Context.GetString(Resource.String.search_result_free_slot),
                Context.GetString(Resource.String.search_result_queue),
                Context.GetString(Resource.String.search_result_files_suffix),
                Context.GetString(Resource.String.search_result_file_suffix));
        }

        /// <summary>
        /// Binds the shared, pure metadata projection once per row. It fills the file name, the
        /// compact detail line and the accessibility descriptions; subclasses then only bind the
        /// fields specific to their layout (chips, speed, availability).
        /// </summary>
        protected SearchResultMetadata BindMetadata(SearchResponse item)
        {
            var meta = SearchResultMetadata.Create(item, hideLocked, metadataLabels);
            if (viewFoldername != null)
            {
                // Results are folder/group rows, so the folder is the primary visible identity
                // (the representative file name is the secondary line). A single file result
                // shows its file name here and its folder in the secondary line.
                viewFoldername.Text = meta.PrimaryText;
            }
            if (viewMetadata != null)
            {
                // Variants without a dedicated file count field keep the count in this line, so
                // a multi-file row never looks like a single file.
                string detail = HasFileCountField ? meta.DetailLine : meta.DetailLineWithFileCount;
                viewMetadata.Text = detail;
                viewMetadata.Visibility = string.IsNullOrEmpty(detail) ? ViewStates.Gone : ViewStates.Visible;
            }
            if (primaryRow != null)
            {
                primaryRow.ContentDescription = meta.IsGroup
                    ? string.Format(
                        Context.GetString(Resource.String.search_result_group_row_description),
                        meta.FolderName,
                        meta.FileCountText,
                        meta.FileName,
                        meta.Peer)
                    : string.Format(
                        Context.GetString(Resource.String.search_result_row_description),
                        meta.FileName,
                        meta.FolderName,
                        meta.Peer);
            }
            if (downloadActionButton != null)
            {
                // The action opens the existing file selection download dialog, so for a group
                // it must describe the folder, not the representative file.
                downloadActionButton.ContentDescription = meta.IsGroup
                    ? string.Format(
                        Context.GetString(Resource.String.download_group_result_description),
                        meta.FolderName)
                    : string.Format(
                        Context.GetString(Resource.String.download_result_description_for),
                        meta.FileName);
            }
            return meta;
        }

        /// <summary>
        /// True when the layout shows the file count in its own field (modern variants), so the
        /// metadata line does not repeat it.
        /// </summary>
        protected virtual bool HasFileCountField => false;

        /// <summary>
        /// Shows the multi-select checkbox only while the contextual action mode is active and
        /// reflects this row's stable selection identity. Called once per bind; no extra work.
        /// </summary>
        public void ApplySelectionState(bool selectionMode, bool selected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.Visibility = selectionMode ? ViewStates.Visible : ViewStates.Gone;
                selectionIndicator.SetImageResource(selected
                    ? Resource.Drawable.check_circle
                    : Resource.Drawable.check_circle_outline);
            }
            if (downloadActionButton != null)
            {
                // The download action opens the per-result dialog; during multi-select the row
                // tap/checkbox already owns the interaction, so hide it instead of offering a
                // second, unexpected dialog path behind the action mode.
                downloadActionButton.Visibility = selectionMode ? ViewStates.Gone : ViewStates.Visible;
            }
            if (primaryRow != null)
            {
                primaryRow.Selected = selected;
                if (selectionMode && selected && !string.IsNullOrEmpty(primaryRow.ContentDescription))
                {
                    primaryRow.ContentDescription = primaryRow.ContentDescription + ", "
                        + Context.GetString(Resource.String.search_result_selected_state);
                }
            }
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
            var meta = BindMetadata(item);
            viewUsername.Text = meta.Peer;
            viewSpeed.Text = meta.Speed;
            viewFileType.Text = meta.DominantFileType;
            viewAvailability.Text = meta.HasFreeUploadSlot ? string.Empty : item.QueueLength.ToString();
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
            var meta = BindMetadata(item);
            viewUsername.Text = meta.Peer;
            viewSpeed.Text = meta.Speed;
            viewFileType.Text = meta.DominantFileType;
            viewAvailability.Text = meta.HasFreeUploadSlot ? string.Empty : item.QueueLength.ToString();
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

        protected override bool HasFileCountField => viewFileCount != null;

        public override void setItem(SearchResponse item, int position)
        {
            var meta = BindMetadata(item);
            viewUsername.Text = meta.Peer;
            viewSpeed.Text = meta.Speed;
            viewFileCount.Text = meta.FileCountText;
            SearchChipHelper.StyleFormatChip(fileTypeChip, meta.DominantFileType, palette);
            SearchChipHelper.StyleQueueChip(queueChip, meta.HasFreeUploadSlot, item.QueueLength, palette);
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
            var meta = BindMetadata(item);
            // Compact has no separate peer/speed/slots/file count fields, so they live in the
            // single metadata line instead of being dropped from this variant.
            if (viewMetadata != null && !string.IsNullOrEmpty(meta.CompactLine))
            {
                viewMetadata.Text = meta.CompactLine;
                viewMetadata.Visibility = ViewStates.Visible;
            }
            SearchChipHelper.StyleFormatChip(fileTypeChip, meta.DominantFileType, palette);
            SearchChipHelper.StyleQueueChip(queueChip, meta.HasFreeUploadSlot, item.QueueLength, palette);
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
