using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.DocumentFile.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace AndriodApp1
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

        public string GetLastPathSegment()
        {
            return Android.Net.Uri.Parse(this.UploadDataDirectoryUri).LastPathSegment;
        }

        [JsonIgnore]
        [NonSerialized]
        public UploadDirectoryError ErrorState;

        [JsonIgnore]
        [NonSerialized]
        public DocumentFile UploadDirectory;
        
        [JsonIgnore]
        [NonSerialized]
        public bool IsSubdir;
        //[System.Xml.Serialization.XmlIgnoreAttribute]
        //public Android.Net.Uri UploadDirectoryUri;

        public void Reset()
        {
            UploadDataDirectoryUri = null;
            UploadDataDirectoryUriIsFromTree = true;
            IsLocked = false;
            IsHidden = false;
            DisplayNameOverride = null;
        }

        public UploadDirectoryInfo(string UploadDataDirectoryUri, bool UploadDataDirectoryUriIsFromTree, bool IsLocked, bool IsHidden, string DisplayNameOverride)
        {
            this.UploadDataDirectoryUri = UploadDataDirectoryUri;
            this.UploadDataDirectoryUriIsFromTree = UploadDataDirectoryUriIsFromTree;
            this.IsLocked = IsLocked;
            this.IsHidden = IsHidden;
            this.DisplayNameOverride = DisplayNameOverride;
            ErrorState = UploadDirectoryError.NoError;
            UploadDirectory = null;
            IsSubdir = false;
        }

        public string GetPresentableName()
        {
            if (string.IsNullOrEmpty(this.DisplayNameOverride))
            {
                MainActivity.GetAllFolderInfo(this, out _, out _, out _, out _, out string presentableName);
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
                MainActivity.GetAllFolderInfo(ourTopMostParent, out bool overrideCase, out _, out _, out string rootOverrideName, out string parentPresentableName);
                return parentPresentableName + ourLastPathSegment.Substring(parentLastPathSegment.Length); //remove parent part and replace it with the parent presentable name.
            }
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