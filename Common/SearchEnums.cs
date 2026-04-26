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

    [System.Flags]
    public enum SearchResultStyleEnum
    {
        Simple = 0,
        Modern = 1 << 0,
        BitrateTop = 1 << 1,
        Expandable = 1 << 2,
        // Compact is a standalone family — bitrate position and expandable do
        // not apply, so it must not be combined with BitrateTop / Expandable.
        Compact = 1 << 3,

        SimpleBottom = Simple,
        SimpleTop = Simple | BitrateTop,
        SimpleBottomExpandable = Simple | Expandable,
        SimpleTopExpandable = Simple | BitrateTop | Expandable,
        ModernBottom = Modern,
        ModernTop = Modern | BitrateTop,
        ModernBottomExpandable = Modern | Expandable,
        ModernTopExpandable = Modern | BitrateTop | Expandable,
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
