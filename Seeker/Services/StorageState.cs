using Android.Content;
using AndroidX.DocumentFile.Provider;
using System;

namespace Seeker.Services
{
    /// <summary>
    /// Roots of the user-selected download / incomplete directories, plus the
    /// fallback "Music" tree URI used when nothing has been picked yet.
    /// </summary>
    public static class StorageState
    {
        public const string DefaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";

        public static DocumentFile RootDocumentFile = null;

        /// <summary>
        /// Only set if we can write to the directory.
        /// </summary>
        public static DocumentFile RootIncompleteDocumentFile = null;

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;

        public static DocumentFile OpenRootFile(Context context, Android.Net.Uri chosenUri)
        {
            return DocumentFile.FromTreeUri(context, chosenUri);
        }
    }
}
