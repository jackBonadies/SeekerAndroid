using Seeker.Transfers;
using Soulseek;
using System;
using System.Linq;
using System.Threading;

namespace Seeker
{
    [Serializable]
    public class TransferItem : ITransferItem
    {
        public string Filename;
        public string Username;
        public string FolderName;
        public string FullFilename;
        public int Progress;
        public bool Failed;
        public TransferStates State;
        public long Size;

        public bool isUpload;
        private int queuelength = int.MaxValue;
        public bool CancelAndRetryFlag = false;
        public bool WasFilenameLatin1Decoded = false;
        public bool WasFolderLatin1Decoded = false;
        public string FinalUri = string.Empty; //final uri of downloaded item
        public string IncompleteParentUri = null; //incomplete parent directory uri.  will be null if successfully downloaded or not yet created.
        public string IncompleteUri = null; //incomplete file uri.  will be null if successfully downloaded or not yet created.
        public TransferItemExtras TransferItemExtra;

        [System.Xml.Serialization.XmlIgnoreAttribute]
        public TimeSpan? RemainingTime;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public double AvgSpeed = 0;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool CancelAndClearFlag = false;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool InProcessing = false; //whether its currently a task in Soulseek.Net.  so from Intialized / Queued to the end of the main download continuation task...
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public CancellationTokenSource CancellationTokenSource = null;

        public int QueueLength
        {
            get
            {
                return queuelength;
            }
            set
            {
                queuelength = value;
            }
        }

        public string GetDisplayName()
        {
            return Filename;
        }

        public string GetFolderName()
        {
            return FolderName;
        }

        public string GetDisplayFolderName()
        {
            //this is similar to QT (in the case of Seeker multiple subdirectories)
            //but not quite.
            //QT will show subdirs/complete/Soulseek Downloads/Music/H: (where everything after subdirs is your download folder)
            //whereas we just show subdirs
            //subdirs is folder name in both cases for single folder, 
            // and say (01 / 2020 / test_folder) for nested.
            if (GetDirectoryLevel() == 1)
            {
                //they are the same
                return FolderName;
            }
            else
            {
                //split reverse.
                var reversedArray = this.FolderName.Split('\\').Reverse();
                return string.Join('\\', reversedArray);
            }
        }

        public int GetDirectoryLevel()
        {
            //just parent folder = level 1 (search result and browse single dir case)
            //grandparent = level 2 (browse download subdirs case - i.e. Album, Album > covers)
            //etc.
            if (this.FolderName == null || !this.FolderName.Contains('\\'))
            {
                return 1;
            }
            return this.FolderName.Split('\\').Count();
        }

        public string GetUsername()
        {
            return Username;
        }

        public TimeSpan? GetRemainingTime()
        {
            return RemainingTime;
        }

        public int GetQueueLength()
        {
            return queuelength;
        }

        public bool IsUpload()
        {
            return isUpload;
        }

        public double GetAvgSpeed()
        {
            return AvgSpeed;
        }

        public long? GetSizeForDL()
        {
            if (this.Size == -1)
            {
                return null;
            }
            else
            {
                return this.Size;
            }
        }

        public bool ShouldEncodeFolderLatin1()
        {
            return WasFolderLatin1Decoded;
        }

        public bool ShouldEncodeFileLatin1()
        {
            return WasFilenameLatin1Decoded;
        }
    }
}