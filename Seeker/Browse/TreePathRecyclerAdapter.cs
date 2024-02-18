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

namespace Seeker
{
    public class TreePathRecyclerAdapter : RecyclerView.Adapter
    {
        private List<PathItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public BrowseFragment Owner;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            TreePathItemView view = TreePathItemView.inflate(parent);
            view.setupChildren();
            view.ViewFolderName.Click += View_Click;
            // .inflate(R.layout.text_row_item, viewGroup, false);
            //(view as SearchTabView).searchTabLayout.Click += SearchTabLayout_Click;
            //(view as SearchTabView).removeSearch.Click += RemoveSearch_Click;
            return new TreePathItemViewHolder(view as View);


        }

        private void View_Click(object sender, EventArgs e)
        {
            int pos = ((sender as TextView).Parent.Parent as TreePathItemView).ViewHolder.AdapterPosition;
            Owner.GoUpDirectory(localDataSet.Count - pos - 2);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as TreePathItemViewHolder).pathItemView.setItem(localDataSet[position]);
        }


        //private void SearchTabLayout_Click(object sender, EventArgs e)
        //{
        //    position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
        //    int tabToGoTo = localDataSet[position];
        //    SearchFragment.Instance.GoToTab(tabToGoTo, false);
        //    SearchTabDialog.Instance.Dismiss();
        //}

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

        public TreePathItemView getUnderlyingView()
        {
            return pathItemView;
        }
    }

    public class TreePathItemView : LinearLayout
    {
        //public TransfersFragment.TransferViewHolder ViewHolder { get; set; }
        private ImageView viewSeparator;
        public TextView ViewFolderName;
        public PathItem InnerPathItem { get; set; }
        public TreePathItemViewHolder ViewHolder;

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
        }

        public void setItem(PathItem item)
        {
            InnerPathItem = item;
            ViewFolderName.Text = item.DisplayName;
            if (item.IsLastNode)
            {
                ViewFolderName.Clickable = false;
                viewSeparator.Visibility = ViewStates.Gone;
            }
            else
            {
                ViewFolderName.Clickable = true;
                viewSeparator.Visibility = ViewStates.Visible;
            }
        }
    }


}