using Common;
using Soulseek;

namespace Seeker
{
    public class BrowseResponseEvent
    {
        public TreeNode<Directory> BrowseResponseTree;
        public BrowseResponse OriginalBrowseResponse;
        public string Username;
        public string StartingLocation;
        public BrowseResponseEvent(BrowseResponse origBR, TreeNode<Directory> t, string u, string startingLocation)
        {
            OriginalBrowseResponse = origBR;
            Username = u;
            BrowseResponseTree = t;
            StartingLocation = startingLocation;
        }
    }
}
