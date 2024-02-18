namespace SlskHelp
{
    using Soulseek;
    using System;
    using System.Collections.Generic;

    internal interface ISharedFileCache
    {
        event EventHandler<(int Directories, int Files)> Refreshed;

        //string Directory { get; }
        //DateTime? LastFill { get; }
        //long TTL { get; }

        void Fill();

        IEnumerable<Soulseek.File> Search(SearchQuery query, string username, out IEnumerable<Soulseek.File> lockedFiles);
    }
}