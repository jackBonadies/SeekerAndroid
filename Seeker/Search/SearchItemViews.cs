using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using Seeker.Extensions.SearchResponseExtensions;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common;
namespace Seeker
{
    public interface ISearchItemViewBase
    {
        void setupChildren();
        SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }
        void setItem(SearchResponse item, int opposite);
    }

    public class SearchItemViewMinimal : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        //private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        public SearchItemViewMinimal(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }
        public SearchItemViewMinimal(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.test_row, this, true);
            setupChildren();
        }

        public static SearchItemViewMinimal inflate(ViewGroup parent)
        {
            SearchItemViewMinimal itemView = (SearchItemViewMinimal)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewminimal_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.textView1);
            viewFoldername = FindViewById<TextView>(Resource.Id.textView2);
            viewSpeed = FindViewById<TextView>(Resource.Id.textView3);
            //viewQueue = FindViewById<TextView>(Resource.Id.textView4);
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString(); //kb/s

            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }
    }

    public class SearchItemViewMedium : RelativeLayout, ISearchItemViewBase
    {
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private TextView viewQueue;
        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }
        public SearchItemViewMedium(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_medium, this, true);
            setupChildren();
        }

        public static SearchItemViewMedium inflate(ViewGroup parent)
        {
            SearchItemViewMedium itemView = (SearchItemViewMedium)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.searchitemviewmedium_dummy, parent, false);
            return itemView;
        }
        private bool hideLocked = false;
        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }

        public void setItem(SearchResponse item, int noop)
        {
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item); //todo maybe also cache this...
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + SimpleHelpers.STRINGS_KBS; //kbs
            viewFileType.Text = item.GetDominantFileTypeAndBitRate(hideLocked, out _);
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            //line separated..
            //viewUsername.Text = item.Username + "  |  " + Helpers.GetDominantFileTypeAndBitRate(item) + "  |  " + (item.UploadSpeed / 1024).ToString() + "kbs";

        }


    }

    public interface IExpandable
    {
        void Expand();
        void Collapse();
    }

    public class ExpandableSearchItemFilesAdapter : ArrayAdapter<Soulseek.File>
    {
        public ExpandableSearchItemFilesAdapter(Context c, List<Soulseek.File> files) : base(c, 0, files.ToArray())
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            TextView itemView = (TextView)convertView;
            if (null == itemView)
            {
                itemView = new TextView(this.Context);//ItemView.inflate(parent);
            }
            itemView.Text = GetItem(position).Filename;
            return itemView;
        }
    }

    public class SearchItemViewExpandable : RelativeLayout, ISearchItemViewBase, IExpandable
    {
        private TextView viewQueue;
        private TextView viewUsername;
        private TextView viewFoldername;
        private TextView viewSpeed;
        private TextView viewFileType;
        private ImageView imageViewExpandable;
        private LinearLayout viewToHideShow;

        public SearchFragment.SearchAdapterRecyclerVersion AdapterRef;


        public SearchFragment.SearchViewHolder ViewHolder
        {
            get; set;
        }

        //private TextView viewQueue;
        public SearchItemViewExpandable(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }
        public SearchItemViewExpandable(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.search_result_expandable, this, true);
            setupChildren();
        }

        public static SearchItemViewExpandable inflate(ViewGroup parent)
        {
            SearchItemViewExpandable itemView = (SearchItemViewExpandable)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.search_result_exampandable_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            viewUsername = FindViewById<TextView>(Resource.Id.userNameTextView);
            viewFoldername = FindViewById<TextView>(Resource.Id.folderNameTextView);
            viewSpeed = FindViewById<TextView>(Resource.Id.speedTextView);
            viewFileType = FindViewById<TextView>(Resource.Id.fileTypeTextView);
            viewToHideShow = FindViewById<LinearLayout>(Resource.Id.detailsExpandable);
            imageViewExpandable = FindViewById<ImageView>(Resource.Id.expandableClick);
            viewQueue = FindViewById<TextView>(Resource.Id.availability);
            hideLocked = PreferencesState.HideLockedResultsInSearch;
        }
        private bool hideLocked = false;
        public static void PopulateFilesListView(LinearLayout viewToHideShow, SearchResponse item)
        {
            viewToHideShow.RemoveAllViews();
            foreach (Soulseek.File f in item.GetFiles(PreferencesState.HideLockedResultsInSearch))
            {
                TextView tv = new TextView(SeekerState.MainActivityRef);
                SetTextColor(tv, SeekerState.MainActivityRef);
                tv.Text = SimpleHelpers.GetFileNameFromFile(f.Filename);
                viewToHideShow.AddView(tv);
            }
        }

        public void setItem(SearchResponse item, int position)
        {
            bool opposite = this.AdapterRef.oppositePositions.Contains(position);
            viewUsername.Text = item.Username;
            viewFoldername.Text = SimpleHelpers.GetFolderNameForSearchResult(item);
            viewSpeed.Text = (item.UploadSpeed / 1024).ToString() + "kbs"; //kb/s
            if (item.FreeUploadSlots > 0)
            {
                viewQueue.Text = "";
            }
            else
            {
                viewQueue.Text = item.QueueLength.ToString();
            }
            viewFileType.Text = item.GetDominantFileTypeAndBitRate(hideLocked, out _);

            if (SearchFragment.SearchResultStyle == SearchResultStyleEnum.CollapsedAll && opposite ||
                SearchFragment.SearchResultStyle == SearchResultStyleEnum.ExpandedAll && !opposite)
            {
                viewToHideShow.Visibility = ViewStates.Visible;
                PopulateFilesListView(viewToHideShow, item);
                //imageViewExpandable.ClearAnimation();
                imageViewExpandable.Rotation = 0;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_less_white_32_dp);
                //viewToHideShow.Adapter = new ExpandableSearchItemFilesAdapter(this.Context,item.Files.ToList());
            }
            else
            {
                viewToHideShow.Visibility = ViewStates.Gone;
                imageViewExpandable.Rotation = 0;
                //imageViewExpandable.ClearAnimation(); //THIS DOES NOT CLEAR THE ROTATE.
                //AFTER doing a rotation animation, the rotation is still there in the 
                //imageview state.  just check float rot = imageViewExpandable.Rotation;
                imageViewExpandable.SetImageResource(Resource.Drawable.ic_expand_more_black_32dp);
            }
            //TEST
            //viewSpeed.Text = item.FreeUploadSlots.ToString();


            //viewQueue.Text = (item.QueueLength).ToString();
        }

        public void Expand()
        {
            viewToHideShow.Visibility = ViewStates.Visible;
        }

        public void Collapse()
        {
            viewToHideShow.Visibility = ViewStates.Gone;
        }

        public static Color GetColorFromAttribute(Context c, int attr, Resources.Theme overrideTheme = null)
        {
            var typedValue = new TypedValue();
            if (overrideTheme != null)
            {
                overrideTheme.ResolveAttribute(attr, typedValue, true);
            }
            else
            {
                c.Theme.ResolveAttribute(attr, typedValue, true);
            }

            if (typedValue.ResourceId == 0)
            {
                return GetColorFromInteger(typedValue.Data);
            }
            else
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(23))
                {
                    return GetColorFromInteger(ContextCompat.GetColor(c, typedValue.ResourceId));
                }
                else
                {
                    return c.Resources.GetColor(typedValue.ResourceId);
                }
            }
        }

        public static Color GetColorFromInteger(int color)
        {
            return Color.Rgb(Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
        }

        public static void SetTextColor(TextView tv, Context c)
        {
            tv.SetTextColor(GetColorFromAttribute(c, Resource.Attribute.cellTextColor));
        }
    }

}