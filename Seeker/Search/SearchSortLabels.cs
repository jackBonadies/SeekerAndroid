using Android.Content;
using Common;

namespace Seeker.Search
{
    /// <summary>
    /// Single source of truth for the user-visible name of every search sort option, shared by
    /// the result header, the in-search sort dialog and the settings default-sort picker.
    /// </summary>
    public static class SearchSortLabels
    {
        public static int NameResource(SearchResultSorting sorting)
        {
            switch (sorting)
            {
                case SearchResultSorting.Fastest:
                    return Resource.String.Speed;
                case SearchResultSorting.FolderAlphabetical:
                    return Resource.String.FolderName;
                case SearchResultSorting.BitRate:
                    return Resource.String.bitrate;
                case SearchResultSorting.Filename:
                    return Resource.String.Filename;
                case SearchResultSorting.FileSize:
                    return Resource.String.FileSize;
                case SearchResultSorting.Available:
                default:
                    return Resource.String.Availability;
            }
        }

        public static string Label(Context context, SearchResultSorting sorting)
        {
            return context.GetString(NameResource(sorting));
        }
    }
}
