using Common;
using System.Collections.Generic;

namespace Seeker
{
    public class TransfersViewState
    {
        public static readonly TransfersViewState Instance = new TransfersViewState();

        public bool GroupByFolder
        {
            get => PreferencesState.TransferViewGroupByFolder;
            set => PreferencesState.TransferViewGroupByFolder = value;
        }

        public bool InUploadsMode
        {
            get => PreferencesState.TransferViewInUploadsMode;
            set => PreferencesState.TransferViewInUploadsMode = value;
        }
        public FolderItem? CurrentlySelectedDLFolder;
        public FolderItem? CurrentlySelectedUploadFolder;
        public List<int> BatchSelectedItems = new List<int>();
        public int ScrollPositionBeforeMovingIntoFolder = int.MinValue;
        public int ScrollOffsetBeforeMovingIntoFolder = int.MinValue;

        public bool CurrentlyInFolder()
        {
            return CurrentlySelectedDLFolder != null || CurrentlySelectedUploadFolder != null;
        }

        public FolderItem? GetCurrentlySelectedFolder()
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

        public void SwitchToUploadsMode()
        {
            InUploadsMode = true;
            CurrentlySelectedDLFolder = null;
            CurrentlySelectedUploadFolder = null;
            BatchSelectedItems.Clear();
            ScrollPositionBeforeMovingIntoFolder = int.MinValue;
            ScrollOffsetBeforeMovingIntoFolder = int.MinValue;
        }
    }
}
