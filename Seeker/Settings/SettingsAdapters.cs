using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Seeker.Managers;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public partial class SettingsActivity
    {
        private enum ChangeDialogType
        {
            ChangePort = 0,
            ChangeDL = 1,
            ChangeUL = 2,
            ConcurrentDL = 3,
        }

        public class ReyclerUploadsAdapter : RecyclerView.Adapter
        {
            public List<UploadDirectoryInfo> localDataSet;
            public override int ItemCount => localDataSet.Count;
            private int position = -1;
            public SettingsActivity settingsActivity;
            public ReyclerUploadsAdapter(SettingsActivity activity, List<UploadDirectoryInfo> ti)
            {
                this.settingsActivity = activity;
                localDataSet = ti;
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                (holder as RecyclerViewFolderHolder).folderView.setItem(localDataSet[position]);
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
            {
                RecyclerViewFolderView view = RecyclerViewFolderView.inflate(parent);
                view.setupChildren();
                view.SettingsActivity = this.settingsActivity;
                (view as View).Click += view.FolderClick;
                (view as View).LongClick += view.FolderLongClick;
                return new RecyclerViewFolderHolder(view as View);
            }

            public void setPosition(int position)
            {
                this.position = position;
            }
        }

        public class RecyclerViewFolderHolder : RecyclerView.ViewHolder, View.IOnCreateContextMenuListener
        {
            public RecyclerViewFolderView folderView;


            public RecyclerViewFolderHolder(View view) : base(view)
            {
                folderView = (RecyclerViewFolderView)view;
                folderView.ViewHolder = this;
                folderView.SetOnCreateContextMenuListener(this);
            }

            public void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
            {
                RecyclerViewFolderView folderRowView = v as RecyclerViewFolderView;
                ContextMenuItem = folderRowView.BoundItem;
                if (ContextMenuItem.HasError())
                {
                    menu.Add(0, 1, 0, Resource.String.ViewErrorOptions);
                }
                else
                {
                    menu.Add(0, 1, 0, Resource.String.ViewFolderOptions);
                }
                menu.Add(0, 2, 1, Resource.String.Remove);
            }
        }

        public class RecyclerViewFolderView : RelativeLayout
        {
            public UploadDirectoryInfo BoundItem;

            public RecyclerViewFolderHolder ViewHolder;
            public SettingsActivity SettingsActivity = null;
            public TextView viewFolderName;
            public ImageView viewFolderStatus;

            public RecyclerViewFolderView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.upload_folder_row, this, true);
                setupChildren();
            }
            public RecyclerViewFolderView(Context context, IAttributeSet attrs) : base(context, attrs)
            {
                LayoutInflater.From(context).Inflate(Resource.Layout.upload_folder_row, this, true);
                setupChildren();
            }

            public void FolderLongClick(object sender, View.LongClickEventArgs e)
            {

                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).setPosition((sender as RecyclerViewFolderView).ViewHolder.AdapterPosition);
                (sender as View).ShowContextMenu();
            }

            public void FolderClick(object sender, EventArgs e)
            {

                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).setPosition((sender as RecyclerViewFolderView).ViewHolder.AdapterPosition);
                (ViewHolder.BindingAdapter as ReyclerUploadsAdapter).settingsActivity.ShowDialogForUploadDir((sender as RecyclerViewFolderView).ViewHolder.folderView.BoundItem);
            }

            public static RecyclerViewFolderView inflate(ViewGroup parent)
            {
                RecyclerViewFolderView itemView = (RecyclerViewFolderView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.upload_folder_row_dummy, parent, false);
                return itemView;
            }

            public void setupChildren()
            {
                viewFolderName = FindViewById<TextView>(Resource.Id.uploadFolderName);
                viewFolderStatus = FindViewById<ImageView>(Resource.Id.uploadFolderStatus);

            }

            public void setItem(UploadDirectoryInfo item)
            {
                this.Clickable = SeekerState.SharingOn;
                this.LongClickable = SeekerState.SharingOn;

                BoundItem = item;
                if (string.IsNullOrEmpty(item.DisplayNameOverride))
                {
                    viewFolderName.Text = item.GetLastPathSegment();
                }
                else
                {
                    viewFolderName.Text = item.GetLastPathSegment() + $" ({item.DisplayNameOverride})";
                }

                if (item.HasError())
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.alert_circle_outline);
                }
                else if (item.IsHidden)
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.hidden_lock_question);
                }
                else if (item.IsLocked)
                {
                    viewFolderStatus.Visibility = ViewStates.Visible;
                    viewFolderStatus.SetImageResource(Resource.Drawable.lock_icon);
                }
                else
                {
                    viewFolderStatus.Visibility = ViewStates.Gone;
                }
            }
        }
    }
}
