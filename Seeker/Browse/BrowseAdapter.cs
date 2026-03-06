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
            localDataSet = items;
        }

        public BrowseAdapter(List<DataItem> items, BrowseFragment owner, int[]? selectedPos)
        {
            Owner = owner;
            localDataSet = items;
            if (selectedPos != null && selectedPos.Length != 0)
            {
                SelectedPositions = selectedPos.ToList();
            }
        }

        public override int ItemCount => localDataSet.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            BrowseResponseItemView itemView = BrowseResponseItemView.inflate(parent);
            itemView.setupChildren();
            if (SeekerState.InDarkModeCache)
            {
                itemView.DisplayName.SetTextColor(Android.Graphics.Color.White);
            }
            else
            {
                itemView.DisplayName.SetTextColor(Android.Graphics.Color.Black);
            }
            BrowseResponseItemViewHolder holder = new BrowseResponseItemViewHolder(itemView);
            itemView.Click += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (pos != RecyclerView.NoPosition)
                {
                    Owner.OnItemClick(pos);
                }
            };
            itemView.LongClick += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (pos != RecyclerView.NoPosition)
                {
                    Owner.OnItemLongClick(pos, itemView);
                }
            };
            itemView.FolderIndicator.Click += (sender, e) =>
            {
                int pos = holder.BindingAdapterPosition;
                if (pos != RecyclerView.NoPosition)
                {
                    Owner.OnActionButtonClick(pos, itemView);
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
                itemView.FolderIndicator.Visibility = ViewStates.Visible;
                itemView.FileDetails.Visibility = ViewStates.Gone;
            }
            else
            {
                itemView.FolderIcon.Visibility = ViewStates.Gone;
                itemView.FolderIndicator.Visibility = ViewStates.Gone;
                itemView.FileDetails.Visibility = ViewStates.Visible;
                itemView.FileDetails.Text = SimpleHelpers.GetSizeLengthAttrString(dataItem.File);
            }

            if (IsInBatchSelectMode)
            {
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
                itemView.SelectionCheckbox.Visibility = ViewStates.Gone;
            }

            itemView.DisplayName.Text = dataItem.GetDisplayName();
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
        public LinearLayout ContainingViewGroup;
        public BrowseResponseItemViewHolder ViewHolder { get; set; }
        public BrowseResponseItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.browse_response_item, this, true);
            setupChildren();
        }
        public BrowseResponseItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.browse_response_item, this, true);
            setupChildren();
        }

        public static BrowseResponseItemView inflate(ViewGroup parent)
        {
            BrowseResponseItemView itemView = (BrowseResponseItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.browse_response_item_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            DisplayName = FindViewById<TextView>(Resource.Id.displayName);
            FolderIndicator = FindViewById<ImageView>(Resource.Id.folderIndicator);
            FolderIcon = FindViewById<ImageView>(Resource.Id.folderIcon);
            SelectionCheckbox = FindViewById<ImageView>(Resource.Id.selectionCheckbox);
            FileDetails = FindViewById<TextView>(Resource.Id.fileDetails);
            ContainingViewGroup = FindViewById<LinearLayout>(Resource.Id.containingViewGroup);
        }

        public void SetSelectedBackground(bool isSelected)
        {
#pragma warning disable 0618
            if (isSelected)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(21))
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, SeekerState.ActiveActivityRef.Theme);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                }
            }
            else
            {
                this.Background = null;
            }
#pragma warning restore 0618
        }
    }

}
