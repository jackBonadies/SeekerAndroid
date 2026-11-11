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
        public TransferSortKey SortKey
        {
            get => PreferencesState.TransferViewSortKey;
            set => PreferencesState.TransferViewSortKey = value;
        }

        public bool SortDescending
        {
            get => PreferencesState.TransferViewSortDescending;
            set => PreferencesState.TransferViewSortDescending = value;
        }

        public FolderItem? CurrentlySelectedDLFolder;
        public FolderItem? CurrentlySelectedUploadFolder;
        public HashSet<ITransferItem> BatchSelectedItems = new HashSet<ITransferItem>();
        // the row at the top of the folder UI list, to keep users position
        public ITransferItem? ScrollAnchorBeforeMovingIntoFolder;
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

        public TransferUIState CreateUIState()
        {
            return new TransferUIState
            {
                GroupByFolder = GroupByFolder,
                CurrentlySelectedFolder = GetCurrentlySelectedFolder(),
                BatchSelectedItems = BatchSelectedItems,
                SortKey = SortKey,
                SortDescending = SortDescending,
            };
        }

        public TransferUIState CreateDLUIState()
        {
            return new TransferUIState
            {
                GroupByFolder = GroupByFolder,
                CurrentlySelectedFolder = CurrentlySelectedDLFolder,
                BatchSelectedItems = BatchSelectedItems,
                SortKey = SortKey,
                SortDescending = SortDescending,
            };
        }

        public void SwitchToUploadsMode()
        {
            InUploadsMode = true;
            CurrentlySelectedDLFolder = null;
            CurrentlySelectedUploadFolder = null;
            BatchSelectedItems.Clear();
            ScrollAnchorBeforeMovingIntoFolder = null;
            ScrollPositionBeforeMovingIntoFolder = int.MinValue;
            ScrollOffsetBeforeMovingIntoFolder = int.MinValue;
        }
    }
}
