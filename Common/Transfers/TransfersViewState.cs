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
        public HashSet<ITransferItem> BatchSelectedItems = new HashSet<ITransferItem>();
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
            SwitchToMode(true);
        }

        /// <summary>
        /// A downloads/uploads change is a change of dataset, not just a visual toggle.
        /// Folder and batch selections refer to objects in the old dataset and must never
        /// be carried into the new one.
        /// </summary>
        public void SwitchToMode(bool uploadsMode)
        {
            InUploadsMode = uploadsMode;
            CurrentlySelectedDLFolder = null;
            CurrentlySelectedUploadFolder = null;
            BatchSelectedItems.Clear();
            ScrollPositionBeforeMovingIntoFolder = int.MinValue;
            ScrollOffsetBeforeMovingIntoFolder = int.MinValue;
        }
    }
}
