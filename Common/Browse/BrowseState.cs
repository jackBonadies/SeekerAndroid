using Common;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public class BrowseState
    {
        public List<DataItem> DataItems { get; } = new List<DataItem>();
        public List<DataItem> FilteredDataItems { get; set; } = new List<DataItem>();
        public Tuple<string, List<DataItem>> CachedFilteredDataItems { get; set; }
        public List<PathItem> PathItems { get; set; } = new List<PathItem>();
        public List<DataItem> DataItemsForDownload { get; set; }
        public List<DataItem> FilteredDataItemsForDownload { get; set; }
        public TextFilter Filter { get; } = new TextFilter();
        public string CurrentUsername { get; set; }
        public bool HasResponse()
        {
            return !string.IsNullOrEmpty(CurrentUsername);
        }
    }
}
