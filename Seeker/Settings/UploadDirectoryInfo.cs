using Seeker.Services;
using AndroidX.DocumentFile.Provider;
using System;
using System.Text.Json.Serialization;

namespace Seeker
{
    /// <summary>
    /// Android-specific partial for UploadDirectoryInfo.
    /// </summary>
    public partial class UploadDirectoryInfo
    {
        [JsonIgnore]
        [NonSerialized]
        public DocumentFile UploadDirectory;

        [JsonIgnore]
        [NonSerialized]
        public bool IsSubdir;

        public string GetLastPathSegment()
        {
            return Android.Net.Uri.Parse(this.UploadDataDirectoryUri).LastPathSegment;
        }

        public string GetPresentableName()
        {
            if (string.IsNullOrEmpty(this.DisplayNameOverride))
            {
                SharedFileService.GetAllFolderInfo(this, out _, out _, out _, out _, out string presentableName);
                return presentableName;
            }
            else
            {
                return this.DisplayNameOverride;
            }
        }

        public string GetPresentableName(UploadDirectoryInfo ourTopMostParent)
        {
            string parentLastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(ourTopMostParent.UploadDirectory, out bool msdCase);
            string ourLastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(this.UploadDirectory, out bool ourMsdCase);
            if (ourMsdCase || msdCase)
            {
                return ourLastPathSegment; //not great but no good solution for msd. TODO test
            }
            else
            {
                SharedFileService.GetAllFolderInfo(ourTopMostParent, out bool overrideCase, out _, out _, out string rootOverrideName, out string parentPresentableName);
                return parentPresentableName + ourLastPathSegment.Substring(parentLastPathSegment.Length); //remove parent part and replace it with the parent presentable name.
            }
        }
    }
}
