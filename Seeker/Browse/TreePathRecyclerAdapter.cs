using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seeker
{
    public class TreePathRecyclerAdapter : RecyclerView.Adapter
    {
        private List<PathItem> localDataSet;
        public override int ItemCount => localDataSet.Count;
        public BrowseFragment Owner;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            TreePathItemView view = TreePathItemView.inflate(parent);
            view.ViewFolderName.Click += View_Click;
            return new TreePathItemViewHolder(view as View);


        }

        private void View_Click(object sender, EventArgs e)
        {
            int pos = (sender as TextView).FindAncestor<TreePathItemView>().ViewHolder.BindingAdapterPosition;
            if (pos == RecyclerView.NoPosition)
            {
                Seeker.Helpers.Logger.Firebase("position is -1");
                return;
            }
            int additionalLevels = localDataSet.Count - pos - 2;
            Seeker.Helpers.Logger.InfoFirebase("browse click pos " + pos + "  additional levels " + additionalLevels);
            Owner.GoUpDirectory(additionalLevels);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as TreePathItemViewHolder).pathItemView.setItem(localDataSet[position]);
        }

        public TreePathRecyclerAdapter(List<PathItem> ti, BrowseFragment owner)
        {
            Owner = owner;
            localDataSet = ti;
        }

    }

    public class TreePathItemViewHolder : RecyclerView.ViewHolder
    {
        public TreePathItemView pathItemView;


        public TreePathItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            pathItemView = (TreePathItemView)view;
            pathItemView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }
    }

    public class TreePathItemView : LinearLayout
    {
        //public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private ImageView viewSeparator;
        public TextView ViewFolderName;
        public PathItem InnerPathItem { get; set; }
        public TreePathItemViewHolder ViewHolder;

        private Color currentFolderColor;
        private Color ancestorFolderColor;

        public TreePathItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tree_path_item_view, this, true);
            setupChildren();
        }
        public TreePathItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.tree_path_item_view, this, true);
            setupChildren();
        }

        public static TreePathItemView inflate(ViewGroup parent)
        {
            TreePathItemView itemView = (TreePathItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.tree_path_item_view_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewSeparator = FindViewById<ImageView>(Resource.Id.folderSeparator);
            ViewFolderName = FindViewById<TextView>(Resource.Id.folderName);

            currentFolderColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.mainTextColor);
            ancestorFolderColor = UiHelpers.GetColorFromAttribute(Context, Resource.Attribute.cellTextColorSubdued);
        }

        public void setItem(PathItem item)
        {
            InnerPathItem = item;
            ViewFolderName.Text = item.DisplayName;
            if (item.IsLastNode)
            {
                ViewFolderName.SetTypeface(null, TypefaceStyle.Bold);
                ViewFolderName.SetTextColor(currentFolderColor);
                ViewFolderName.Clickable = false;
                viewSeparator.Visibility = ViewStates.Gone;
            }
            else
            {
                ViewFolderName.SetTypeface(null, TypefaceStyle.Normal);
                ViewFolderName.SetTextColor(ancestorFolderColor);
                ViewFolderName.Clickable = true;
                viewSeparator.Visibility = ViewStates.Visible;
            }
        }
    }


}
