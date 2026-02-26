using Soulseek;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public class CachedParseResults
    {
        public CachedParseResults(
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys,
            int directoryCount,
            BrowseResponse browseResponse,
            List<Directory> browseResponseHiddenPortion,
            List<Tuple<string, string>> friendlyDirNameToUriMapping,
            Dictionary<string, List<int>> tokenIndex,
            Dictionary<int, string> helperIndex,
            int nonHiddenFileCount)
        {
            this.keys = keys;
            this.directoryCount = directoryCount;
            this.browseResponse = browseResponse;
            this.browseResponseHiddenPortion = browseResponseHiddenPortion;
            this.friendlyDirNameToUriMapping = friendlyDirNameToUriMapping;
            this.tokenIndex = tokenIndex;
            this.helperIndex = helperIndex;
            this.nonHiddenFileCount = nonHiddenFileCount;
        }

        public CachedParseResults()
        {
        }

        public Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> keys = null;
        public int directoryCount = -1;
        public BrowseResponse browseResponse = null;
        public List<Soulseek.Directory> browseResponseHiddenPortion = null;
        public List<Tuple<string, string>> friendlyDirNameToUriMapping = null;
        public Dictionary<string, List<int>> tokenIndex = null;
        public Dictionary<int, string> helperIndex = null;
        public int nonHiddenFileCount = -1;
    }
}
