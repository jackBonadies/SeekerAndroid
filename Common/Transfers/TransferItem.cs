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
        // OBSOLETE - TODO - after enough release cycles remove
        public int Progress;
        public long BytesTransferred;
        public bool Failed;
        private TransferStates state;
        // when the row entered its current band (i.e. was last meaningfully "touched", TransferBands.NextStamp)
        // 0 for rows saved before this existed
        public long BandStamp;
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
        public double AvgSpeed = 0;
        // only used for live time remaining calculations so do not serialize
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public DateTime AvgSpeedSampledUtc;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool CancelAndClearFlag = false;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool InProcessing = false; //whether its currently a task in Soulseek.Net.  so from Intialized / Queued to the end of the main download continuation task...
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public CancellationTokenSource CancellationTokenSource = null;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public FolderItem Folder;
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public long LastProgressPostMs;
        // date added order
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public long Seq;
        // the stamp the next band change takes, see HintBandStamp
        //   reason being: at say Cancel time, we set the cancellation token
        //   but we do not transition into state cancelled until later.
        //   this allows us to set the "next" bandstamp ahead of time
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public long BandStampHint;

        // stamps BandStamp when the write moves the row to another band
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public TransferStates State
        {
            get
            {
                return state;
            }
            set
            {
                TransferBand previous = TransferBands.Of(state);
                state = value;
                if (TransferBands.Of(value) != previous)
                {
                    long hint = Interlocked.Exchange(ref BandStampHint, 0);
                    Interlocked.Exchange(ref BandStamp, hint != 0 ? hint : TransferBands.NextStamp());
                }
            }
        }

        // it never stamps since its restored from persisted
        [System.Xml.Serialization.XmlElement("State")]
        public TransferStates PersistedState
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        // hint for the next bandstamp
        public void HintBandStamp(long stamp, bool onlyIfActive = false)
        {
            if (stamp == 0)
            {
                return;
            }
            if (onlyIfActive && TransferBands.Of(state) != TransferBand.Active)
            {
                return;
            }
            Interlocked.Exchange(ref BandStampHint, stamp);
        }

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
            return State.HasFlag(TransferStates.InProgress) ? RemainingTime : null;
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
            const TransferStates showing = TransferStates.InProgress | TransferStates.Initializing
                | TransferStates.Requested | TransferStates.Succeeded;
            return (State & showing) != 0 ? AvgSpeed : 0;
        }

        // -1 for legacy "unknown" sentinel. We re-request size if 0.
        public long GetSizeForDL()
        {
            if (this.Size < 0)
            {
                return 0;
            }
            return this.Size;
        }

        public bool ShouldEncodeFolderLatin1()
        {
            return WasFolderLatin1Decoded;
        }

        public bool ShouldEncodeFileLatin1()
        {
            return WasFilenameLatin1Decoded;
        }

        /// <summary>
        /// Estimate of bytes transferred thus far (falls back to Progress for backwards compat - i.e. users 
        ///   with saved transfers before BytesTransferred was a field)
        /// </summary>
        public long GetBytesTransferred()
        {
            if (BytesTransferred > 0)
            {
                return BytesTransferred;
            }
            if (Size > 0)
            {
                return (long)((Progress / 100.0) * Size);
            }
            return 0;
        }

        /// <summary>
        /// Progress as an integer percent (0-100) for the progress bar. This owns the
        ///   bar's UI policy and is deliberately divorced from GetBytesTransferred(): a
        ///   failed transfer shows a full (red) bar and a succeeded transfer shows 100%
        ///   even if byte bookkeeping rounds low. Everything else is byte-derived.
        /// </summary>
        public int GetProgressForPresentation()
        {
            if (Failed || State.HasFlag(TransferStates.Succeeded))
            {
                return 100;
            }
            if (Size > 0)
            {
                return (int)Math.Clamp(GetBytesTransferred() * 100 / Size, 0, 100);
            }
            return 0;
        }

        public void ClearStateForRetry()
        {
            Progress = 0; //no longer red... some good user feedback
            BytesTransferred = 0;
            QueueLength = int.MaxValue; //let the State Changed update this for us...
            Failed = false;
            TransferItemExtra &= ~TransferItemExtras.DirNotSet;
        }
    }
}