using Android.Content;
using AndroidX.DocumentFile.Provider;
using Common;
using Seeker.Helpers;
using System;

namespace Seeker.Services
{
    /// <summary>
    /// Roots of the user-selected download / incomplete directories, plus the
    /// fallback "Music" tree URI used when nothing has been picked yet.
    /// Owns the four pieces of state that must move in lockstep: Root*DocumentFile,
    /// PreferencesState.*Uri, PreferencesState.*UriIsFromTree, and TakePersistableUriPermission.
    /// </summary>
    public static class StorageState
    {
        public const string DefaultMusicUri = "content://com.android.externalstorage.documents/tree/primary%3AMusic";

        public static DocumentFile RootDocumentFile { get; private set; }

        /// <summary>
        /// Only set if we can write to the directory.
        /// </summary>
        public static DocumentFile RootIncompleteDocumentFile { get; private set; }

        public static EventHandler<EventArgs> DirectoryUpdatedEvent;

        public static void SetRootDownloadDirectory(Context context, Android.Net.Uri uri, bool isFromTree, bool raiseUpdatedEvent = false)
        {
            RootDocumentFile = BuildDocumentFile(context, uri, isFromTree);
            PreferencesState.SaveDataDirectoryUri = uri.ToString();
            PreferencesState.SaveDataDirectoryUriIsFromTree = isFromTree;
            if (isFromTree)
            {
                context.ContentResolver.TakePersistableUriPermission(uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
            }
            if (raiseUpdatedEvent)
            {
                DirectoryUpdatedEvent?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void SetRootIncompleteDirectory(Context context, Android.Net.Uri uri, bool isFromTree, bool raiseUpdatedEvent = false)
        {
            RootIncompleteDocumentFile = BuildDocumentFile(context, uri, isFromTree);
            PreferencesState.ManualIncompleteDataDirectoryUri = uri.ToString();
            PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree = isFromTree;
            if (isFromTree)
            {
                context.ContentResolver.TakePersistableUriPermission(uri, ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantReadUriPermission);
            }
            if (raiseUpdatedEvent)
            {
                DirectoryUpdatedEvent?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Runs once per process in <see cref="SeekerApplication.OnCreate"/>. Restores
        /// <see cref="RootDocumentFile"/> and <see cref="RootIncompleteDocumentFile"/> from
        /// <see cref="PreferencesState"/> if the URI is still writable. For non-legacy storage,
        /// leaves <see cref="RootDocumentFile"/> null if the download directory permission has been
        /// revoked; MainActivity checks for null and shows the re-selection dialog. Does NOT
        /// modify preferences and does NOT raise <see cref="DirectoryUpdatedEvent"/>.
        /// </summary>
        public static void LoadFromPreferences(Context context)
        {
            if (PlatformInfo.UseLegacyStorage())
            {
                if (!string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri))
                {
                    var chosenUri = Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, chosenUri, PreferencesState.SaveDataDirectoryUriIsFromTree, "legacy download"))
                    {
                        RootDocumentFile = DocumentFile.FromTreeUri(context, chosenUri);
                    }
                }
                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    var chosenUri = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, chosenUri, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree, "legacy incomplete"))
                    {
                        RootIncompleteDocumentFile = DocumentFile.FromTreeUri(context, chosenUri);
                    }
                }
            }
            else
            {
                Android.Net.Uri res = string.IsNullOrEmpty(PreferencesState.SaveDataDirectoryUri)
                    ? Android.Net.Uri.Parse(DefaultMusicUri)
                    : Android.Net.Uri.Parse(PreferencesState.SaveDataDirectoryUri);

                if (CheckDirectoryForWritePermission(context, res, PreferencesState.SaveDataDirectoryUriIsFromTree, "download"))
                {
                    RootDocumentFile = BuildDocumentFile(context, res, PreferencesState.SaveDataDirectoryUriIsFromTree);
                }
                // else: RootDocumentFile stays null — MainActivity will detect this and show the re-selection dialog

                if (!string.IsNullOrEmpty(PreferencesState.ManualIncompleteDataDirectoryUri))
                {
                    var incompleteRes = Android.Net.Uri.Parse(PreferencesState.ManualIncompleteDataDirectoryUri);
                    if (CheckDirectoryForWritePermission(context, incompleteRes, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree, "incomplete"))
                    {
                        RootIncompleteDocumentFile = BuildDocumentFile(context, incompleteRes, PreferencesState.ManualIncompleteDataDirectoryUriIsFromTree);
                    }
                }
            }
        }

        private static DocumentFile BuildDocumentFile(Context context, Android.Net.Uri uri, bool isFromTree)
        {
            return isFromTree
                ? DocumentFile.FromTreeUri(context, uri)
                : DocumentFile.FromFile(new Java.IO.File(uri.Path));
        }

        private static bool CheckDirectoryForWritePermission(Context context, Android.Net.Uri chosenUri, bool directoryUriFromTree, string logContext)
        {
            bool canWrite = false;
            try
            {
                if (!directoryUriFromTree)
                {
                    canWrite = DocumentFile.FromFile(new Java.IO.File(chosenUri.Path)).CanWrite();
                }
                else
                {
                    canWrite = DocumentFile.FromTreeUri(context, chosenUri).CanWrite();
                }
            }
            catch (Exception e)
            {
                if (chosenUri != null)
                {
                    Logger.Firebase($"{logContext} DocumentFile.FromTreeUri failed with URI: " + chosenUri.ToString() + " " + e.Message + " scheme " + chosenUri.Scheme);
                }
                else
                {
                    Logger.Firebase($"{logContext} DocumentFile.FromTreeUri failed with null URI");
                }
            }
            if (!canWrite)
            {
                Logger.Firebase($"canWrite = false for {logContext} Uri: " + chosenUri.ToString());
            }
            return canWrite;
        }
    }
}
