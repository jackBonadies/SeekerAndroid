using Common;
using Common.Browse;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public enum BrowseStateError
    {
        None = 0,
        CannotFindStartDirectory = 1
    }

    public class BrowseState
    {
        public List<DataItem> DataItems { get; } = new List<DataItem>();
        public List<DataItem> FilteredDataItems { get; set; } = new List<DataItem>();
        public Tuple<string, List<DataItem>>? CachedFilteredDataItems { get; set; }
        public List<PathItem> PathItems { get; set; } = new List<PathItem>();
        public TextFilter Filter { get; } = new TextFilter();
        public string? CurrentUsername { get; set; }

        public bool HasResponse()
        {
            return !string.IsNullOrEmpty(CurrentUsername);
        }

        public BrowseStateError SetBrowseResponse(string username, TreeNode<Directory> browseResponseTree, string startingLocation)
        {
            BrowseStateError errorState = BrowseStateError.None;
            ClearFilter();
            CurrentUsername = username;

            lock (DataItems) //on non UI thread.
            {
                DataItems.Clear();//clear old
                //originalBrowseTree = e.BrowseResponseTree; //the already parsed tree
                if (startingLocation != null && startingLocation != string.Empty)
                {
                    var startingPoint = BrowseUtils.GetNodeByName(browseResponseTree, startingLocation);

                    if (startingPoint == null)
                    {
                        errorState = BrowseStateError.CannotFindStartDirectory;
                        DataItems.AddRange(BrowseUtils.GetDataItemsForNode(browseResponseTree));
                    } 
                    else
                    {
                        DataItems.AddRange(BrowseUtils.GetDataItemsForNode(startingPoint));
                    }
                }
                else
                {
                    DataItems.AddRange(BrowseUtils.GetDataItemsForNode(browseResponseTree));
                }
            }
            return errorState;
        }

        public void ClearFilter()
        {
            Filter.Reset();
            FilteredDataItems = new List<DataItem>();
            CachedFilteredDataItems = null;
        }

        public void UpdateFilteredResponses()
        {
            lock (FilteredDataItems)
            {
                FilteredDataItems.Clear();
                if (CachedFilteredDataItems != null && BrowseUtils.IsCurrentSearchMoreRestrictive(Filter.FilterString, CachedFilteredDataItems.Item1))
                {
                    var test = BrowseUtils.FilterBrowseList(CachedFilteredDataItems.Item2, Filter);
                    FilteredDataItems.AddRange(test);
                }
                else
                {
                    var test = BrowseUtils.FilterBrowseList(DataItems, Filter);
                    FilteredDataItems.AddRange(test);
                }
                CachedFilteredDataItems = new Tuple<string, List<DataItem>>(Filter.FilterString, FilteredDataItems.ToList());
            }
        }

        public bool IsAtRoot()
        {
            if (DataItems.Count == 0) 
            {
                return true; 
            }
            var first = DataItems[0];
            if (first.File != null)
            {
                return first.Node?.Parent == null;
            }
            else if (first.Directory != null)
            {
                return first.Node?.Parent?.Parent == null;
            }
            return true;
        }

        public bool GoUpDirectory(Action<bool, List<DataItem>, bool, bool> setBrowseAdapters, int additionalLevels)
        {
            CachedFilteredDataItems = null; 
            bool filteredResults = Filter.IsFiltered;
            lock (DataItems)
            {
                TreeNode<Directory>? item = null;
                try
                {
                    if (DataItems[0].File != null)
                    {
                        item = DataItems[0]?.Node?.Parent;  //?.Parent; //This used to do grandparent.  Which is a bug I think, so I changed it.
                    }
                    else if (DataItems[0].Directory != null)
                    {
                        item = DataItems[0]?.Node?.Parent?.Parent;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false; //bad... 
                }
                if (item == null)
                {
                    return false; //we must be at or near the highest
                }
                for (int i = 0; i < additionalLevels; i++)
                {
                    item = item.Parent;
                }
                DataItems.Clear();
                DataItems.AddRange(BrowseUtils.GetDataItemsForNode(item));
            }
            lock (DataItems)
            {
                lock (FilteredDataItems)
                {
                    setBrowseAdapters(filteredResults, DataItems, false, true);
                }
            }

            return true;
        }
    }
}
