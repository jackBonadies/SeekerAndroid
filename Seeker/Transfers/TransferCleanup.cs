using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using Seeker.Services;
using System.Linq;

using Common;
using System;

namespace Seeker
{
    public static class TransferCleanup
    {
        public static void PerformCleanupItem(TransferItem ti)
        {
            Logger.Debug("cleaning up: " + ti.Filename);
            DocumentFile parent = null;
            Android.Net.Uri parentIncompleteUri = Android.Net.Uri.Parse(ti.IncompleteParentUri);
            if (SettingsActivity.UseTempDirectory() || parentIncompleteUri.Scheme == "file")
            {
                parent = DocumentFile.FromFile(new Java.IO.File(parentIncompleteUri.Path));
            }
            else
            {
                parent = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, parentIncompleteUri); //if from single uri then listing files will give unsupported operation exception...  //if temp (file: //)this will throw (which makes sense as it did not come from open tree uri)
            }

            DocumentFile df = parent.FindFile(ti.Filename);
            if (df == null || !df.Exists())
            {
                // i.e. legacy storage only creates the file once the stream opens, so a download cleared while queued has none
                Logger.Debug("delete failed - null or not exist");
                Logger.InfoFirebase("df is null or not exist: " + parentIncompleteUri + " " + PreferencesState.CreateCompleteAndIncompleteFolders + " " + parent.Uri + " " + SettingsActivity.UseIncompleteManualFolder());
            }
            else if (!df.Delete())
            {
                Logger.Debug("delete failed");
            }
            FileSystemService.Instance.DeleteParentIfEmpty(parent);
        }
    }
}
