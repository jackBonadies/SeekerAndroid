namespace AndriodApp1
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
        Minimal = 0,
        Medium = 1,
        CollapsedAll = 2,
        ExpandedAll = 3,
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
}