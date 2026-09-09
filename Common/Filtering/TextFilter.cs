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
            // always create new consistent version, never .Clear() bc it will be iterated over on other
            //   threads leading to InvalidOperation_EnumFailedVersion on WordsToAvoid/WordsToInclude
            List<string> wordsToAvoid = new List<string>();
            List<string> wordsToInclude = new List<string>();
            FilterSpecialFlags? specialFlags = supportsSpecialFlags ? new FilterSpecialFlags() : null;
            SearchFilter.ParseFilterString(filterString, wordsToAvoid, wordsToInclude, specialFlags);

            FilterString = filterString;
            WordsToAvoid = wordsToAvoid;
            WordsToInclude = wordsToInclude;
            FilterSpecialFlags = specialFlags;
        }

        public void Reset()
        {
            FilterString = string.Empty;
            WordsToAvoid = new List<string>();
            WordsToInclude = new List<string>();
            FilterSpecialFlags = supportsSpecialFlags ? new FilterSpecialFlags() : null;
        }
    }
}
