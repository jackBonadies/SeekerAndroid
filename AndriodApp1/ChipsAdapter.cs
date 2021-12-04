using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Text.Format;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Android.Material.Chip;
using Android.Util;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;
using System;
namespace AndriodApp1
{

    public static class ChipsHelper
    {
        public static List<ChipDataItem> GetChipDataItemsFromSearchResults(List<Soulseek.SearchResponse> responses)
        {
            Dictionary<string, int> fileTypeCounts = new Dictionary<string, int>();
            Dictionary<int, int> fileCountCounts = new Dictionary<int, int>();

            //inital pass
            int count = responses.Count;
            for (int i = 0; i < count; i++)
            {
                var searchResponse = responses[i]; //the search is done at this point, so search responses will not be changed.

                //create file type, file num, and keyword buckets.
                //get counts to show in order
                //there are parent child relationships between 'fileType' and 'fileType (vbr/kbps/samples/depth)'
                string ftype = searchResponse.GetDominantFileType();
                if (string.IsNullOrEmpty(ftype))
                {
                    continue;
                }
                if (fileTypeCounts.ContainsKey(ftype))
                {
                    fileTypeCounts[ftype]++;
                }
                else
                {
                    fileTypeCounts[ftype] = 1;
                }
                //int baseIndex = ftype.IndexOf(" (");
                //if (baseIndex != -1)
                //{
                //    fileTypeBases.Add(ftype.Substring(0,baseIndex));
                //}

                int fcount = searchResponse.FileCount;
                if (fileCountCounts.ContainsKey(fcount))
                {
                    fileCountCounts[fcount]++;
                }
                else
                {
                    fileCountCounts[fcount] = 1;
                }

                //TODO: keywords
                #if DEBUG
                Console.WriteLine(Helpers.GetFolderNameFromFile(searchResponse.Files.First().Filename));
                #endif
            }


            //second pass
            //create file count buckets
            List<string> chipDescriptions = new List<string>();
            if (fileCountCounts.Count > 4)
            {
                //do groups.
                //the each group consists of >= 1/4 of the results
                int groupSize = count / 4;
                var sortedList = fileCountCounts.ToList();
                //key is the folder count, value is the number of times that folder count appeared.
                sortedList.Sort((x, y) => y.Key.CompareTo(x.Key));
                int start = int.MinValue;
                int partialTotal = 0;
                int numGroups = 0;

                for (int ii = 0; ii < sortedList.Count; ii++)
                {
                    if (numGroups == 3)
                    {
                        //put the rest in the last bucket
                        if (ii == sortedList.Count - 1)
                        {
                            //we are on the last one
                            chipDescriptions.Add($"{sortedList[ii].Key} files");
                        }
                        else
                        {
                            chipDescriptions.Add($"{sortedList[ii].Key} to {sortedList[sortedList.Count - 1].Key} files");
                        }
                        break;
                    }
                    if ((sortedList[ii].Value + partialTotal) >= groupSize)
                    {
                        //thats all for this group
                        if (start == int.MinValue)
                        {
                            //that means we start and end here
                            numGroups++;
                            if (sortedList[ii].Key == 1)
                            {
                                chipDescriptions.Add($"{sortedList[ii].Key} file");
                            }
                            else
                            {
                                chipDescriptions.Add($"{sortedList[ii].Key} files");
                            }
                        }
                        else
                        {
                            //that means we start and end here
                            numGroups++;
                            chipDescriptions.Add($"{start} to {sortedList[ii].Key} files");
                        }
                        partialTotal = 0;
                        start = int.MinValue;
                    }
                    else
                    {
                        if (start == int.MinValue)
                        {
                            start = sortedList[ii].Key;
                        }
                        partialTotal += sortedList[ii].Value;
                    }
                }
            }


            List<string> fileTypeBases = new List<string>();
            //get bases
            foreach (string fileType in fileTypeCounts.Keys)
            {
                int fIndexBase = fileType.IndexOf(" (");
                if (fIndexBase != -1)
                {
                    string fbase = fileType.Substring(0, fIndexBase);
                    if (!fileTypeBases.Contains(fbase))
                    {
                        fileTypeBases.Add(fbase);
                    }
                }
            }

            //fileTypeBases i.e. mp3, flac
            //if bases have more than 1 add "base - all"
            int bases = 0;
            foreach (string fileTypeBase in fileTypeBases)
            {
                int count1 = 0;
                int results = 0;
                foreach (var fileType in fileTypeCounts)
                {
                    if (fileType.Key.Contains(fileTypeBase))
                    {
                        count1++;
                        results += fileType.Value;
                    }
                }
                if (count1 > 1)
                {
                    //add a " - all".
                    //remove the (base) if it is there.
                    fileTypeCounts.Remove(fileTypeBase);
                    fileTypeCounts.Add(fileTypeBase + " - all", results);
                    bases++;
                }
            }

            //now sort.  the sort is a bit special as its mostly by number of results, but with variants coming after all (if there are any)
            var sortedListPass1 = fileTypeCounts.ToList();
            sortedListPass1.Sort((x, y) => y.Value.CompareTo(x.Value));
            var sortedListPass1str = sortedListPass1.Select((pair) => pair.Key).ToList();
            int startIndex = 0;
            while (bases > 0)
            {
                for (int iii = startIndex; iii < sortedListPass1str.Count; iii++)
                {
                    string allStr = sortedListPass1str[iii];
                    if (allStr.Contains(" - all"))
                    {
                        startIndex = iii + 1;
                        string basetype = allStr.Replace(" - all", "");
                        var stringsToMove = sortedListPass1str.FindAll((ftype) => ftype.Contains(basetype) && ftype != allStr);
                        foreach (string stringToMove in stringsToMove)
                        {
                            sortedListPass1str.Remove(stringToMove);
                            sortedListPass1str.Insert(startIndex, stringToMove);
                            startIndex += 1;
                        }
                        bases--;
                        break;
                    }
                }
            }

            var dataItems = chipDescriptions.Select(str=>new ChipDataItem(ChipType.FileCount,false, str));
            dataItems.Last().LastInGroup = true;
            var dataItemsList = dataItems.ToList();
            dataItemsList.AddRange(sortedListPass1str.Select(str=>new ChipDataItem(ChipType.FileType,false,str)));
            return dataItemsList;
        }
    }

    public class ChipsItemRecyclerAdapter : RecyclerView.Adapter
    {
        private List<ChipDataItem> localDataSet; //tab id's
        public override int ItemCount => localDataSet.Count;
        private int position = -1;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) //so view Type is a real thing that the recycler adapter knows about.
        {

            ChipItemView view = ChipItemView.inflate(parent);
            view.setupChildren();
            //view.Chip.CheckedChange += Chip_CheckedChange;

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
            int pos = ((sender as View).Parent.Parent as ChipItemView).ViewHolder.AdapterPosition;
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
        }


        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ChipItemViewHolder).chipItemView.setItem(localDataSet[position]);
        }


        //private void SearchTabLayout_Click(object sender, EventArgs e)
        //{
        //    position = ((sender as View).Parent.Parent as SearchTabView).ViewHolder.AdapterPosition;
        //    int tabToGoTo = localDataSet[position];
        //    SearchFragment.Instance.GoToTab(tabToGoTo, false);
        //    SearchTabDialog.Instance.Dismiss();
        //}

        public ChipsItemRecyclerAdapter(List<ChipDataItem> ti)
        {
            localDataSet = ti;
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

    public enum ChipType
    {
        FileType = 0,
        FileCount = 1,
        Keyword = 2
    }

    public class ChipDataItem
    {
        public readonly string DisplayText;
       // public readonly List<string> Children; //this is for Keyword.  In that case we can identify our children as those which contain us (minus parent prefix of '- all'). (i.e. "mp3 - all" (parent) children are mp3 (vbr), mp3 (320).
        public readonly ChipType ChipType;
        public bool LastInGroup; //last in group AND there is more after it
        public bool IsChecked = false;
        public bool IsEnabled = true; //(-all case)
        public ChipDataItem(ChipType chipType, bool lastInGroup, string displayText)
        {
            this.ChipType = chipType;
            this.LastInGroup = lastInGroup;
            this.DisplayText = displayText;
            //this.Children = children;
        }
    }


    public class ChipItemView : LinearLayout
    {
        //public Chip Chip;
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
            ChipItemView itemView = (ChipItemView)LayoutInflater.From(parent.Context).Inflate(Resource.Layout.chip_item_view_dummy, parent, false);
            return itemView;
        }

        public void setupChildren()
        {
            //Chip = FindViewById<Chip>(Resource.Id.chip1);
            ChipSeparator = FindViewById<View>(Resource.Id.chipSeparator);
            ChipLayout = FindViewById<View>(Resource.Id.chipLayout);
        }

        public void setItem(ChipDataItem item)
        {
            //Chip.Text = item.DisplayText;
            //Chip.Checked = item.IsChecked;

            //Chip.Enabled = item.IsEnabled;
            //Chip.Clickable = item.IsEnabled;

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