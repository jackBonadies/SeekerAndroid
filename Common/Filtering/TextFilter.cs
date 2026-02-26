using System.Collections.Generic;

namespace Seeker
{
    public class TextFilter
    {
        public string FilterString { get; private set; }
        public List<string> WordsToAvoid { get; private set; } = new List<string>();
        public List<string> WordsToInclude { get; private set; } = new List<string>();
        public FilterSpecialFlags? FilterSpecialFlags { get; private set; } = null;
        public bool IsFiltered => !string.IsNullOrEmpty(FilterString);

        private readonly bool supportsSpecialFlags;

        public TextFilter(bool supportsSpecialFlags = false)
        {
            this.supportsSpecialFlags = supportsSpecialFlags;
            if (supportsSpecialFlags)
            {
                FilterSpecialFlags = new FilterSpecialFlags();
            }
        }

        public void Set(string filterString)
        {
            FilterString = filterString;
            WordsToAvoid.Clear();
            WordsToInclude.Clear();
            FilterSpecialFlags?.Clear();
            if (supportsSpecialFlags)
            {
                SearchFilter.ParseFilterString(filterString, WordsToAvoid, WordsToInclude, FilterSpecialFlags);
            }
            else
            {
                SearchFilter.ParseFilterString(filterString, WordsToAvoid, WordsToInclude);
            }
        }

        public void Reset()
        {
            FilterString = null;
            WordsToAvoid.Clear();
            WordsToInclude.Clear();
            FilterSpecialFlags?.Clear();
        }
    }
}
