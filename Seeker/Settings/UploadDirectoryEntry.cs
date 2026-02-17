using Seeker.Services;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;

namespace Seeker
{
    /// <summary>
    /// Android-specific wrapper around UploadDirectoryInfo.
    /// Holds runtime state (DocumentFile, IsSubdir) and Android-dependent methods.
    /// </summary>
    public class UploadDirectoryEntry
    {
        public UploadDirectoryInfo Info { get; }

        public DocumentFile UploadDirectory;
        public bool IsSubdir;

        public UploadDirectoryEntry(UploadDirectoryInfo info)
        {
            Info = info;
        }

        public string GetLastPathSegment()
        {
            return Android.Net.Uri.Parse(Info.UploadDataDirectoryUri).LastPathSegment;
        }

        public string GetPresentableName()
        {
            if (string.IsNullOrEmpty(Info.DisplayNameOverride))
            {
                SharedFileService.GetAllFolderInfo(this, out _, out _, out _, out _, out string presentableName);
                return presentableName;
            }
            else
            {
                return Info.DisplayNameOverride;
            }
        }

        public string GetPresentableName(UploadDirectoryEntry ourTopMostParent)
        {
            string parentLastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(ourTopMostParent.UploadDirectory, out bool msdCase);
            string ourLastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(this.UploadDirectory, out bool ourMsdCase);
            if (ourMsdCase || msdCase)
            {
                return ourLastPathSegment;
            }
            else
            {
                SharedFileService.GetAllFolderInfo(ourTopMostParent, out bool overrideCase, out _, out _, out string rootOverrideName, out string parentPresentableName);
                return parentPresentableName + ourLastPathSegment.Substring(parentLastPathSegment.Length);
            }
        }
    }
}
