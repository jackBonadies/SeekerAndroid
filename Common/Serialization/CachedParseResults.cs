using Soulseek;
using System;
using System.Collections.Generic;

namespace Seeker
{
    public class CachedParseResults
    {
        public CachedParseResults(
            Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> presentableNameToFullFileInfo,
            int directoryCount,
            BrowseResponse browseResponse,
            List<Directory> browseResponseHiddenPortion,
            List<Tuple<string, string>> presentableDirectoryNameToDirectoryUriMappings,
            Dictionary<string, List<int>> searchTermTokenToListOfFileKeys,
            Dictionary<int, string> fileKeyToPresentableName,
            int nonHiddenFileCount)
        {
            this.PresentableNameToFullFileInfo = presentableNameToFullFileInfo;
            this.DirectoryCount = directoryCount;
            this.BrowseResponse = browseResponse;
            this.BrowseResponseHiddenPortion = browseResponseHiddenPortion;
            this.PresentableDirectoryNameToDirectoryUriMappings = presentableDirectoryNameToDirectoryUriMappings;
            this.SearchTermTokenToListOfFileKeys = searchTermTokenToListOfFileKeys;
            this.FileKeyToPresentableName = fileKeyToPresentableName;
            this.NonHiddenFileCount = nonHiddenFileCount;
        }

        public Dictionary<string, Tuple<long, string, Tuple<int, int, int, int>, bool, bool>> PresentableNameToFullFileInfo { get; private set; }
        public int DirectoryCount { get; private set; } = -1;
        public BrowseResponse BrowseResponse { get; private set; }
        public List<Soulseek.Directory> BrowseResponseHiddenPortion { get; private set; }
        public List<Tuple<string, string>> PresentableDirectoryNameToDirectoryUriMappings { get; private set; }
        public Dictionary<string, List<int>> SearchTermTokenToListOfFileKeys { get; private set; }
        public Dictionary<int, string> FileKeyToPresentableName { get; private set; }
        public int NonHiddenFileCount { get; private set; } = -1;
    }
}
