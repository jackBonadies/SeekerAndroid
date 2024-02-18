using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Seeker.BrowseFragment;

namespace Seeker
{
    public class BrowseAdapter : ArrayAdapter<DataItem>
    {
        public List<int> SelectedPositions = new List<int>();
        public BrowseFragment Owner = null;
        public BrowseAdapter(Context c, List<DataItem> items, BrowseFragment owner) : base(c, 0, items)
        {
            Owner = owner;
        }

        public BrowseAdapter(Context c, List<DataItem> items, BrowseFragment owner, int[]? selectedPos) : base(c, 0, items)
        {
            Owner = owner;
            if (selectedPos != null && selectedPos.Count() != 0)
            {
                SelectedPositions = selectedPos.ToList();
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            BrowseResponseItemView itemView = (BrowseResponseItemView)convertView;
            if (null == itemView) //we do this once
            {
                itemView = BrowseResponseItemView.inflate(parent);
                itemView.setupChildren();
                if (SeekerState.InDarkModeCache)
                {
                    itemView.DisplayName.SetTextColor(Android.Graphics.Color.White);
                }
                else
                {
                    itemView.DisplayName.SetTextColor(Android.Graphics.Color.Black);
                }
            }

            if (SelectedPositions.Contains(position))
            {
                itemView.SetSelectedBackground(true);

            }
            else
            {
                itemView.SetSelectedBackground(false);
            }
            var dataItem = GetItem(position);
            if (dataItem.IsDirectory())
            {
                itemView.FolderIndicator.Visibility = ViewStates.Visible;
                itemView.FileDetails.Visibility = ViewStates.Gone;
                //itemView.ContainingViewGroup.SetPadding(0, SeekerState.ActiveActivityRef.Resources.GetDimensionPixelSize(Resource.Dimension.browse_no_details_top_bottom), 0, SeekerState.ActiveActivityRef.Resources.GetDimensionPixelSize(Resource.Dimension.browse_no_details_top_bottom));
            }
            else
            {
                itemView.FolderIndicator.Visibility = ViewStates.Gone;
                itemView.FileDetails.Visibility = ViewStates.Visible;
                itemView.FileDetails.Text = CommonHelpers.GetSizeLengthAttrString(dataItem.File);
                //itemView.ContainingViewGroup.SetPadding(0,SeekerState.ActiveActivityRef.Resources.GetDimensionPixelSize(Resource.Dimension.browse_details_top),0, SeekerState.ActiveActivityRef.Resources.GetDimensionPixelSize(Resource.Dimension.browse_details_bottom));
            }
            itemView.DisplayName.Text = dataItem.GetDisplayName();
            return itemView;
            //return base.GetView(position, convertView, parent);
        }
    }


    public class BrowseResponseItemView : LinearLayout
    {
        public TextView DisplayName;
        public TextView FileDetails;
        public ImageView FolderIndicator;
        public LinearLayout ContainingViewGroup;
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
            FileDetails = FindViewById<TextView>(Resource.Id.fileDetails);
            ContainingViewGroup = FindViewById<LinearLayout>(Resource.Id.containingViewGroup);
        }

        public void SetSelectedBackground(bool isSelected)
        {
#pragma warning disable 0618
            if (isSelected)
            {
                if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, SeekerState.ActiveActivityRef.Theme);
                    this.DisplayName.Background = Resources.GetDrawable(Resource.Color.cellbackSelected, SeekerState.ActiveActivityRef.Theme);
                }
                else
                {
                    this.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                    this.DisplayName.Background = Resources.GetDrawable(Resource.Color.cellbackSelected);
                }
            }
            else
            {
                this.Background = null;
                this.DisplayName.Background = null;
            }
#pragma warning restore 0618
        }
    }

}