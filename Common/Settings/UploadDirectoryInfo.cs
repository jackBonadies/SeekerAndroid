using System;
using System.Text.Json.Serialization;

namespace Seeker
{
    /// <summary>
    /// Small info about which directories the user shared.
    /// </summary>
    [Serializable]
    public class UploadDirectoryInfo
    {
        public string UploadDataDirectoryUri;
        public bool UploadDataDirectoryUriIsFromTree;
        public bool IsLocked;
        public bool IsHidden;
        public string DisplayNameOverride;

        public bool HasError()
        {
            return ErrorState != UploadDirectoryError.NoError;
        }

        [JsonIgnore]
        [NonSerialized]
        public UploadDirectoryError ErrorState;

        public void Reset()
        {
            UploadDataDirectoryUri = null;
            UploadDataDirectoryUriIsFromTree = true;
            IsLocked = false;
            IsHidden = false;
            DisplayNameOverride = null;
        }

        public UploadDirectoryInfo()
        {
            ErrorState = UploadDirectoryError.NoError;
        }

        public UploadDirectoryInfo(string UploadDataDirectoryUri, bool UploadDataDirectoryUriIsFromTree, bool IsLocked, bool IsHidden, string DisplayNameOverride)
        {
            this.UploadDataDirectoryUri = UploadDataDirectoryUri;
            this.UploadDataDirectoryUriIsFromTree = UploadDataDirectoryUriIsFromTree;
            this.IsLocked = IsLocked;
            this.IsHidden = IsHidden;
            this.DisplayNameOverride = DisplayNameOverride;
            ErrorState = UploadDirectoryError.NoError;
        }
    }

    public enum UploadDirectoryError
    {
        NoError = 0,
        DoesNotExist = 1,
        CannotWrite = 2,
        Unknown = 3,
    }
}
