using Seeker.Extensions.SearchResponseExtensions;
using Seeker.Helpers;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Chip;
using System;
using System.Collections.Generic;
using System.Linq;

using Common;
namespace Seeker
{
    public class ChipsItemRecyclerAdapter : RecyclerView.Adapter
    {
        private List<ChipDataItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            ChipItemView view = ChipItemView.inflate(parent);
            view.setupChildren();
            view.Chip.CheckedChange += Chip_CheckedChange;

            return new ChipItemViewHolder(view as View);


        }

        /// <summary>
        /// multiple for a type should be OR'd together. none means all.
        /// </summary>
        /// <returns></returns>
        public List<ChipDataItem> GetCheckedItemsForType(ChipType type)
        {
            return localDataSet.Where(item => item.ChipType == type && item.IsChecked && item.IsEnabled).ToList();
        }

        private void Chip_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            //results need to update.
            int pos = ((sender as View).Parent.Parent as ChipItemView).ViewHolder.BindingAdapterPosition;
            bool prevValue = localDataSet[pos].IsChecked;
            localDataSet[pos].IsChecked = e.IsChecked;
            if (prevValue != e.IsChecked)
            {
                if (localDataSet[pos].ChipType == ChipType.FileType)
                {
                    if (localDataSet[pos].DisplayText.Contains(" - all"))
                    {
                        string baseType = localDataSet[pos].DisplayText.Replace(" - all", "");
                        for (int i = 0; i < localDataSet.Count; i++)
                        {
                            if (localDataSet[i].DisplayText.Contains(baseType) && localDataSet[i].DisplayText != localDataSet[pos].DisplayText)
                            {
                                localDataSet[i].IsEnabled = !e.IsChecked;
                                this.NotifyItemChanged(i); //needed to turn off animations for this. else doesn't look too good.
                            }
                        }
                    }
                }
                //if changed, then alert to filter.
            }
            //if (e.IsChecked)
            //{
            //    CheckedItems.Add(pos);
            //}
            //else
            //{
            //    CheckedItems.Remove(pos);
            //}
            var searchTab = SearchTabHelper.SearchTabCollection[SearchTabHelper.CurrentTab];
            searchTab.ChipsFilter = SearchFilter.ParseChips(searchTab.ChipDataItems);
            SearchFragment.Instance.RefreshOnChipChanged();
        }


        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ChipItemViewHolder).chipItemView.setItem(localDataSet[position]);
        }


        //private void SearchTabLayout_Click(object sender, EventArgs e)
        //{
        //    position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.BindingAdapterPosition;
        //    int tabToGoTo = localDataSet[position];
        //    SearchFragment.Instance.GoToTab(tabToGoTo, false);
        //    SearchTabDialog.Instance.Dismiss();
        //}

        public ChipsItemRecyclerAdapter(List<ChipDataItem> ti)
        {
            if (ti == null)
            {
                localDataSet = new List<ChipDataItem>();
            }
            else
            {
                localDataSet = ti;
            }
        }

    }


    public class ChipItemViewHolder : RecyclerView.ViewHolder
    {
        public ChipItemView chipItemView;


        public ChipItemViewHolder(View view) : base(view)
        {
            //super(view);
            // Define click listener for the ViewHolder's View

            chipItemView = (ChipItemView)view;
            chipItemView.ViewHolder = this;
            //(ChatroomOverviewView as View).SetOnCreateContextMenuListener(this);
        }
    }


    public class ChipItemView : LinearLayout
    {
        public Chip Chip;
        public View ChipSeparator;
        public View ChipLayout;
        public ChipItemViewHolder ViewHolder;

        public ChipItemView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chip_item_view, this, true);
            setupChildren();
        }
        public ChipItemView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.chip_item_view, this, true);
            setupChildren();
        }

        public static ChipItemView inflate(ViewGroup parent)
        {
            var c = new ContextThemeWrapper(parent.Context, Resource.Style.MaterialThemeForChip);
            ChipItemView itemView = (ChipItemView)LayoutInflater.From(c).Inflate(Resource.Layout.chip_item_view_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            Chip = FindViewById<Chip>(Resource.Id.chip1);
            ChipSeparator = FindViewById<View>(Resource.Id.chipSeparator);
            ChipLayout = FindViewById<View>(Resource.Id.chipLayout);
        }

        public void setItem(ChipDataItem item)
        {
            Chip.Text = item.DisplayText;
            Chip.Checked = item.IsChecked;

            Chip.Enabled = item.IsEnabled;
            Chip.Clickable = item.IsEnabled;

            if (item.LastInGroup)
            {
                //we already have the right padding due to the separator so set it to 0
                ChipLayout.SetPadding(ChipLayout.PaddingLeft, ChipLayout.PaddingTop, 0, ChipLayout.PaddingBottom);
                ChipSeparator.Visibility = ViewStates.Visible;
            }
            else
            {
                ChipLayout.SetPadding(ChipLayout.PaddingLeft, ChipLayout.PaddingTop, 4, ChipLayout.PaddingBottom);
                ChipSeparator.Visibility = ViewStates.Gone;
            }


        }
    }
}