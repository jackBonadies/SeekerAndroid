using Seeker.Transfers;

namespace Seeker.Helpers
{
    public static class FileFilterHelper
    {
        public static bool IsHiddenFolder(string presentableName)
        {
            if (IsHiddenFile(presentableName))
            {
                return true;
            }
            foreach (string hiddenDir in UploadDirectoryManager.PresentableNameHiddenDirectories)
            {
                if (presentableName == hiddenDir)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLockedFolder(string presentableName)
        {
            if (IsLockedFile(presentableName))
            {
                return true;
            }
            foreach (string lockedDir in UploadDirectoryManager.PresentableNameLockedDirectories)
            {
                if (presentableName == lockedDir)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsLockedFile(string presentableName)
        {
            foreach (string lockedDir in UploadDirectoryManager.PresentableNameLockedDirectories)
            {
                if (presentableName.StartsWith($"{lockedDir}\\")) //no need for == bc files
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsHiddenFile(string presentableName)
        {
            foreach (string hiddenDir in UploadDirectoryManager.PresentableNameHiddenDirectories)
            {
                if (presentableName.StartsWith($"{hiddenDir}\\")) //no need for == bc files
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetVolumeName(string lastPathSegment, bool alwaysReturn, out bool entireString)
        {
            entireString = false;
            //if the first part of the path has a colon in it, then strip it.
            int endOfFirstPart = lastPathSegment.IndexOf('\\');
            if (endOfFirstPart == -1)
            {
                endOfFirstPart = lastPathSegment.Length;
            }
            int volumeIndex = lastPathSegment.Substring(0, endOfFirstPart).IndexOf(':');
            if (volumeIndex == -1)
            {
                return null;
            }
            else
            {
                string volumeName = lastPathSegment.Substring(0, volumeIndex + 1);
                if (volumeName.Length == lastPathSegment.Length)
                {   //special case where root is primary:.  in this case we return null which gets treated as "dont strip out anything"
                    entireString = true;
                    if (alwaysReturn)
                    {
                        return volumeName;
                    }
                    return null;
                }
                else
                {
                    return volumeName;
                }
            }
        }

        public static string GetPresentableName(Android.Net.Uri uri, string folderToStripForPresentableNames, string volName)
        {
            if (uri.LastPathSegment == null)
            {
                Logger.Firebase($"{uri} has null last path segment");
                // next line throws
            }

            string presentableName = uri.LastPathSegment.Replace('/', '\\');

            if (folderToStripForPresentableNames == null) //this means that the primary: is in the path so at least convert it from primary: to primary:\
            {
                if (volName != null && volName.Length != presentableName.Length) //i.e. if it has something after it.. primary: should be primary: not primary:\ but primary:Alarms should be primary:\Alarms
                {
                    presentableName = presentableName.Substring(0, volName.Length) + '\\' + presentableName.Substring(volName.Length);
                }
            }
            else
            {
                presentableName = presentableName.Substring(folderToStripForPresentableNames.Length);
            }
            return presentableName;
        }
    }
}
