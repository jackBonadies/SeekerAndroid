using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Seeker.BrowseFragment;

namespace Seeker
{
    public class BrowseAdapter : RecyclerView.Adapter
    {
        public List<int> SelectedPositions = new List<int>();
        public bool IsInBatchSelectMode = false;
        public BrowseFragment Owner = null;
        private List<DataItem> localDataSet;

        public BrowseAdapter(List<DataItem> items, BrowseFragment owner)
        {
            Owner = owner;
            // Snapshot: the source list is mutated in-place by BrowseState from
            // non-UI threads (filter debounce, browse-response handler). Holding
            // a reference would let RecyclerView's layout pass see the list mid-mutation
            // and crash in OnBindViewHolder with ArgumentOutOfRangeException.
            lock (items)
            {
                localDataSet = new List<DataItem>(items);
            }
        }

        public BrowseAdapter(List<DataItem> items, BrowseFragment owner, int[]? selectedPos)
        {
            Owner = owner;
            lock (items)
            {
                localDataSet = new List<DataItem>(items);
            }
            if (selectedPos != null && selectedPos.Length != 0)
            {
                SelectedPositions = selectedPos.ToList();
            }
        }

        public override int ItemCount => localDataSet.Count;

        /// <summary>A tap may race a data-set swap; validate against the adapter's own count so
        /// the owner is never asked to index a position this adapter no longer has.</summary>
        private bool IsValidTapPosition(int position)
        {
            return position != RecyclerView.NoPosition && position >= 0 && position < localDataSet.Count;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            BrowseResponseItemView itemView = BrowseResponseItemView.Create(parent);
            BrowseResponseItemViewHolder holder = new BrowseResponseItemViewHolder(itemView);
            itemView.Click += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (IsValidTapPosition(pos))
                {
                    Owner.OnItemClick(pos);
                }
            };
            itemView.LongClick += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (IsValidTapPosition(pos))
                {
                    Owner.OnItemLongClick(pos, itemView);
                }
            };
            itemView.FolderIndicator.Click += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (IsValidTapPosition(pos))
                {
                    Owner.OnActionButtonClick(pos, itemView);
                }
            };
            itemView.DownloadButton.Click += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (IsValidTapPosition(pos))
                {
                    Owner.OnDownloadButtonClick(pos);
                }
            };
            return holder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            BrowseResponseItemViewHolder browseHolder = (BrowseResponseItemViewHolder)holder;
            BrowseResponseItemView itemView = browseHolder.browseItemView;

            if (SelectedPositions.Contains(position))
            {
                itemView.SetSelectedBackground(true);
            }
            else
            {
                itemView.SetSelectedBackground(false);
            }

            var dataItem = localDataSet[position];
            if (dataItem.IsDirectory())
            {
                itemView.FolderIcon.Visibility = ViewStates.Visible;
                itemView.ActionContainer.Visibility = ViewStates.Visible;
                itemView.FolderIndicator.Visibility = ViewStates.Visible;
                itemView.FileDetails.Visibility = ViewStates.Visible;
                itemView.FileDetails.Text = GetFolderDetailLine(dataItem);
            }
            else
            {
                itemView.FolderIcon.Visibility = ViewStates.Gone;
                itemView.ActionContainer.Visibility = ViewStates.Gone;
                itemView.FolderIndicator.Visibility = ViewStates.Invisible;
                itemView.FileDetails.Visibility = ViewStates.Visible;
                itemView.FileDetails.Text = SimpleHelpers.GetSizeLengthAttrString(dataItem.File);
            }

            if (IsInBatchSelectMode)
            {
                itemView.ActionContainer.Visibility = ViewStates.Visible;
                itemView.FolderIndicator.Visibility = ViewStates.Invisible;
                itemView.DownloadButton.Visibility = ViewStates.Gone;
                itemView.SelectionCheckbox.Visibility = ViewStates.Visible;
                if (SelectedPositions.Contains(position))
                {
                    itemView.SelectionCheckbox.SetImageResource(Resource.Drawable.check_circle);
                }
                else
                {
                    itemView.SelectionCheckbox.SetImageResource(Resource.Drawable.check_circle_outline);
                }
            }
            else
            {
                itemView.DownloadButton.Visibility = ViewStates.Visible;
                itemView.SelectionCheckbox.Visibility = ViewStates.Invisible;
            }

            itemView.DisplayName.Text = dataItem.GetDisplayName();
        }

        /// <summary>
        /// Cheap secondary line for a folder using fields the browse response already carries
        /// (immediate file count + child folder count). No recursive walk on the bind path, so
        /// large folders stay a RecyclerView and do not do O(n) work per row.
        /// </summary>
        private static string GetFolderDetailLine(DataItem dataItem)
        {
            int files = dataItem.Directory != null ? dataItem.Directory.FileCount : 0;
            int folders = dataItem.Node != null && dataItem.Node.Children != null ? dataItem.Node.Children.Count : 0;
            return string.Format(SeekerApplication.GetString(Resource.String.browse_folder_meta), files, folders);
        }
    }


    public class BrowseResponseItemViewHolder : RecyclerView.ViewHolder
    {
        public BrowseResponseItemView browseItemView;
        public BrowseResponseItemViewHolder(View view) : base(view)
        {
            browseItemView = (BrowseResponseItemView)view;
            browseItemView.ViewHolder = this;
        }
    }


    public class BrowseResponseItemView : LinearLayout
    {
        public TextView DisplayName;
        public TextView FileDetails;
        public ImageView FolderIndicator;
        public ImageView FolderIcon;
        public ImageView SelectionCheckbox;
        public ImageButton DownloadButton;
        public FrameLayout ActionContainer;
        public LinearLayout ContainingViewGroup;
        public BrowseResponseItemViewHolder ViewHolder { get; set; }
        public BrowseResponseItemView(Context context) : base(context)
        {
            Foreground = UiHelpers.GetDrawableFromAttribute(context, Android.Resource.Attribute.SelectableItemBackground);
            LayoutInflater.From(context).Inflate(Resource.Layout.browse_response_item, this, true);
            setupChildren();
        }

        public static BrowseResponseItemView Create(ViewGroup parent)
        {
            var itemView = new BrowseResponseItemView(parent.Context);
            itemView.LayoutParameters = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            return itemView;
        }

        public void setupChildren()
        {
            DisplayName = FindViewById<TextView>(Resource.Id.displayName);
            FolderIndicator = FindViewById<ImageView>(Resource.Id.folderIndicator);
            FolderIcon = FindViewById<ImageView>(Resource.Id.folderIcon);
            SelectionCheckbox = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            DownloadButton = FindViewById<ImageButton>(Resource.Id.downloadButton);
            ActionContainer = FindViewById<FrameLayout>(Resource.Id.actionContainer);
            FileDetails = FindViewById<TextView>(Resource.Id.fileDetails);
            ContainingViewGroup = FindViewById<LinearLayout>(Resource.Id.containingViewGroup);
        }

        public void SetSelectedBackground(bool isSelected)
        {
            if (isSelected)
            {
                this.Background = Resources.GetDrawable(Resource.Color.batchSelectHighlight, null);
            }
            else
            {
                this.Background = null;
            }
        }
    }

}
