using System.Collections.Generic;

namespace Seeker
{
    public class TransferUIState
    {
        public bool GroupByFolder;
        public FolderItem? CurrentlySelectedFolder;
        public HashSet<ITransferItem>? BatchSelectedItems;
    }
}
