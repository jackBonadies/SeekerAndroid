using System.Collections.Generic;
using Common;

namespace Seeker
{
    public class TransfersViewState
    {
        public static readonly TransfersViewState Instance = new TransfersViewState();

        public bool GroupByFolder;
        public volatile bool InUploadsMode;
        public FolderItem CurrentlySelectedDLFolder;
        public FolderItem CurrentlySelectedUploadFolder;
        public List<int> BatchSelectedItems = new List<int>();
        public int ScrollPositionBeforeMovingIntoFolder = int.MinValue;
        public int ScrollOffsetBeforeMovingIntoFolder = int.MinValue;

        public bool CurrentlyInFolder()
        {
            return CurrentlySelectedDLFolder != null || CurrentlySelectedUploadFolder != null;
        }

        public FolderItem GetCurrentlySelectedFolder()
        {
            if (InUploadsMode)
                return CurrentlySelectedUploadFolder;
            else
                return CurrentlySelectedDLFolder;
        }

        public TransferUIState CreateDLUIState()
        {
            return new TransferUIState
            {
                GroupByFolder = GroupByFolder,
                CurrentlySelectedFolder = CurrentlySelectedDLFolder,
                BatchSelectedItems = BatchSelectedItems,
            };
        }
    }
}
