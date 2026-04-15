namespace Seeker
{
    public enum SearchTarget
    {
        AllUsers = 0,
        UserList = 1,
        ChosenUser = 2,
        Wishlist = 3,
        Room = 4
    }

    public enum SearchResultStyleEnum
    {
        MinimalLegacy = 0,
        MediumLegacy = 11,
        ExpandableLegacy = 2,
        MediumModernBitrateBottom = 1, // the new default
        ExpandableModern = 4,
        MediumModernBitrateTop = 5,
    }

    public enum TabType
    {
        Search = 0,
        Wishlist = 1
    }

    public enum SearchResultSorting
    {
        Available = 0,
        Fastest = 1,
        FolderAlphabetical = 2,
        BitRate = 3,
    }

    public enum ChipType
    {
        FileType = 0,
        FileCount = 1,
        Keyword = 2
    }

    public enum SmartFilterStyle
    {
        Flat = 0,
        Grouped = 1
    }

    public enum FormatFilterType
    {
        Any = 0,
        Lossless = 1,
        Lossy = 2
    }

    public class ConfigureChipItems
    {
        public bool Enabled;
        public string Name;
    }
}
