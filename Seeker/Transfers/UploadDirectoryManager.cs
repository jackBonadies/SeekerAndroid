/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */
using Seeker.Services;
using Android.Content;
using Android.Provider;
using AndroidX.DocumentFile.Provider;
using Seeker.Helpers;
using Seeker.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;

namespace Seeker
{
    public static class UploadDirectoryManager
    {
        public static string GetCompositeErrorString()
        {
            if (UploadDirectoryManager.UploadDirectories.Any(d => d.Info.ErrorState == UploadDirectoryError.CannotWrite))
            {
                return GetErrorString(UploadDirectoryError.CannotWrite);
            }
            else if (UploadDirectoryManager.UploadDirectories.Any(d => d.Info.ErrorState == UploadDirectoryError.DoesNotExist))
            {
                return GetErrorString(UploadDirectoryError.DoesNotExist);
            }
            else if (UploadDirectoryManager.UploadDirectories.Any(d => d.Info.ErrorState == UploadDirectoryError.Unknown))
            {
                return GetErrorString(UploadDirectoryError.Unknown);
            }
            else
            {
                return null;
            }
        }

        public static string GetErrorString(UploadDirectoryError errorCode)
        {
            switch (errorCode)
            {
                case UploadDirectoryError.CannotWrite:
                    return SeekerApplication.GetString(Resource.String.PermissionErrorShared);
                case UploadDirectoryError.DoesNotExist:
                    return SeekerApplication.GetString(Resource.String.FolderNotAccessible);
                case UploadDirectoryError.Unknown:
                    return SeekerApplication.GetString(Resource.String.UnknownErrorShared);
                case UploadDirectoryError.NoError:
                default:
                    return "No Error.";
            }
        }

        public static void RestoreFromSavedState(ISharedPreferences sharedPreferences)
        {
            string sharedDirInfo = sharedPreferences.GetString(KeyConsts.M_SharedDirectoryInfo, string.Empty);
            if (string.IsNullOrEmpty(sharedDirInfo))
            {
                string legacyUploadDataDirectory = sharedPreferences.GetString(KeyConsts.M_UploadDirectoryUri, string.Empty);
                bool fromTree = sharedPreferences.GetBoolean(KeyConsts.M_UploadDirectoryUriIsFromTree, true);

                if (!string.IsNullOrEmpty(legacyUploadDataDirectory))
                {
                    var uploadDir = new UploadDirectoryEntry(new UploadDirectoryInfo(legacyUploadDataDirectory, fromTree, false, false, null));
                    SetDirectories(new List<UploadDirectoryEntry> { uploadDir });

                    SaveToSharedPreferences(sharedPreferences);
                    var editor = sharedPreferences.Edit();
                    editor.PutString(KeyConsts.M_UploadDirectoryUri, string.Empty);
                    editor.Apply();
                }
                else
                {
                    SetDirectories(null);
                }
            }
            else
            {
                var infos = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(sharedDirInfo);
                SetDirectories(infos.Select(info => new UploadDirectoryEntry(info)));
            }
        }

        public static void SaveToSharedPreferences(ISharedPreferences sharedPreferences)
        {
            using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
            {
                string userDirsString = SerializationHelper.SerializeToString(UploadDirectories.Select(e => e.Info).ToList());
                lock (sharedPreferences)
                {
                    var editor = sharedPreferences.Edit();
                    editor.PutString(KeyConsts.M_SharedDirectoryInfo, userDirsString);
                    editor.Apply();
                }
            }
        }

        // Copy-on-write: UploadDirectories is shared across the UI thread and multiple ThreadPool
        // background threads (folder add/remove, parse/rescan).
        private static volatile List<UploadDirectoryEntry> _uploadDirectories = new List<UploadDirectoryEntry>();
        private static readonly object _uploadDirectoriesWriteLock = new object();

        public static List<UploadDirectoryEntry> UploadDirectories => _uploadDirectories;

        /// <summary>
        /// Replace the whole directory list (used on restore). Snapshots the source so the caller
        /// can't mutate it out from under readers afterwards.
        /// </summary>
        public static void SetDirectories(IEnumerable<UploadDirectoryEntry> entries)
        {
            lock (_uploadDirectoriesWriteLock)
            {
                _uploadDirectories = entries == null
                    ? new List<UploadDirectoryEntry>()
                    : new List<UploadDirectoryEntry>(entries);
            }
        }

        public static void AddDirectory(UploadDirectoryEntry entry)
        {
            lock (_uploadDirectoriesWriteLock)
            {
                var copy = new List<UploadDirectoryEntry>(_uploadDirectories);
                copy.Add(entry);
                _uploadDirectories = copy;
            }
        }

        public static bool RemoveDirectory(UploadDirectoryEntry entry)
        {
            lock (_uploadDirectoriesWriteLock)
            {
                var copy = new List<UploadDirectoryEntry>(_uploadDirectories);
                bool removed = copy.Remove(entry);
                if (removed)
                {
                    _uploadDirectories = copy;
                }
                return removed;
            }
        }

        public static void ClearDirectories()
        {
            lock (_uploadDirectoriesWriteLock)
            {
                _uploadDirectories = new List<UploadDirectoryEntry>();
            }
        }

        /// <summary>
        /// Atomically remove <paramref name="oldEntry"/> and add <paramref name="newEntry"/> in a
        /// single swap (the reselect case), so readers never observe an intermediate state.
        /// </summary>
        public static void ReplaceDirectory(UploadDirectoryEntry oldEntry, UploadDirectoryEntry newEntry)
        {
            lock (_uploadDirectoriesWriteLock)
            {
                var copy = new List<UploadDirectoryEntry>(_uploadDirectories);
                copy.Remove(oldEntry);
                copy.Add(newEntry);
                _uploadDirectories = copy;
            }
        }

        public static bool IsFromTree(string presentablePath)
        {
            if (UploadDirectories.All(dir => dir.Info.UploadDataDirectoryUriIsFromTree))
            {
                return true;
            }

            if (UploadDirectories.All(dir => !dir.Info.UploadDataDirectoryUriIsFromTree))
            {
                return false;
            }

            return true;
        }

        public static bool AreAnyFromLegacy()
        {
            return UploadDirectories.Where(dir => !dir.Info.UploadDataDirectoryUriIsFromTree).Any();
        }

        /// <summary>
        /// If so then we turn off sharing. If only 1+ failed we let the user know, but keep sharing on.
        /// </summary>
        public static bool AreAllFailed()
        {
            return UploadDirectories.All(dir => dir.Info.HasError());
        }

        public static bool DoesNewDirectoryHaveUniqueRootName(UploadDirectoryEntry newDirEntry, bool updateItToHaveUniqueName)
        {
            bool isUnique = true;
            List<string> currentRootNames = new List<string>();
            foreach (UploadDirectoryEntry dirEntry in UploadDirectories)
            {
                if (dirEntry.IsSubdir || (dirEntry == newDirEntry))
                {
                    continue;
                }
                else
                {
                    SharedFileService.GetAllFolderInfo(dirEntry, out _, out string presentableName);
                    currentRootNames.Add(presentableName);
                }
            }
            SharedFileService.GetAllFolderInfo(newDirEntry, out _, out string presentableNameNew);
            if (currentRootNames.Contains(presentableNameNew))
            {
                isUnique = false;
                if (updateItToHaveUniqueName)
                {
                    while (currentRootNames.Contains(presentableNameNew))
                    {
                        presentableNameNew = presentableNameNew + " (1)";
                    }
                    newDirEntry.Info.DisplayNameOverride = presentableNameNew;
                }
            }
            return isUnique;
        }

        /// <summary>
        /// If only 1+ failed we let the user know, but keep sharing on.
        /// </summary>
        public static bool AreAnyFailed()
        {
            return UploadDirectories.Any(dir => dir.Info.HasError());
        }

        /// <summary>
        /// I think this should just return "external" (TODO - implement and test)
        /// https://developer.android.google.cn/reference/android/provider/MediaStore#VOLUME_EXTERNAL
        /// </summary>
        public static HashSet<string> GetInterestedVolNames()
        {
            HashSet<string> interestedVolnames = new HashSet<string>();
            foreach (var uploadDir in UploadDirectories)
            {
                if (!uploadDir.IsSubdir && uploadDir.UploadDirectory != null)
                {
                    string lastPathSegment = CommonHelpers.GetLastPathSegmentWithSpecialCaseProtection(uploadDir.UploadDirectory, out bool msdCase);
                    if (msdCase)
                    {
                        interestedVolnames.Add(string.Empty);
                    }
                    else
                    {
                        string volName = FileFilterHelper.GetVolumeName(lastPathSegment, true, out _);

                        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
                        {
                            interestedVolnames.Add("external");
                            return interestedVolnames;
                        }
                        var volumeNames = MediaStore.GetExternalVolumeNames(SeekerState.ActiveActivityRef);
                        string chosenVolume = null;
                        if (volName != null)
                        {
                            string volToCompare = volName.Replace(":", "");
                            foreach (string mediaStoreVolume in volumeNames)
                            {
                                if (mediaStoreVolume.ToLower() == volToCompare.ToLower())
                                {
                                    chosenVolume = mediaStoreVolume;
                                }
                            }
                        }

                        if (chosenVolume == null)
                        {
                            interestedVolnames.Add(string.Empty);
                        }
                        else
                        {
                            interestedVolnames.Add(chosenVolume);
                        }
                    }
                }
            }
            return interestedVolnames;
        }

        public static List<string> PresentableNameLockedDirectories { get; private set; } = new List<string>();
        public static List<string> PresentableNameHiddenDirectories { get; private set; } = new List<string>();

        public static void RecomputeDirectoryState()
        {
            ResolveDocumentFilesAndErrorStates();
            RecomputeSubdirFlags();
            RebuildLockedAndHiddenPrefixLists();
        }

        private static void ResolveDocumentFilesAndErrorStates()
        {
            // Snapshot once so a concurrent copy-on-write swap can't tear Count vs. [i].
            var dirs = _uploadDirectories;
            for (int i = 0; i < dirs.Count; i++)
            {
                UploadDirectoryEntry entry = dirs[i];

                Android.Net.Uri uploadDirUri = Android.Net.Uri.Parse(entry.Info.UploadDataDirectoryUri);
                try
                {
                    entry.Info.ErrorState = UploadDirectoryError.NoError;
                    if (!entry.Info.UploadDataDirectoryUriIsFromTree)
                    {
                        entry.UploadDirectory = DocumentFile.FromFile(new Java.IO.File(uploadDirUri.Path));
                    }
                    else
                    {
                        entry.UploadDirectory = DocumentFile.FromTreeUri(SeekerState.ActiveActivityRef, uploadDirUri);
                        if (!entry.UploadDirectory.Exists())
                        {
                            entry.UploadDirectory = null;
                            entry.Info.ErrorState = UploadDirectoryError.DoesNotExist;
                        }
                        else if (!entry.UploadDirectory.CanWrite())
                        {
                            entry.UploadDirectory = null;
                            entry.Info.ErrorState = UploadDirectoryError.CannotWrite;
                        }
                    }
                }
                catch (Exception e)
                {
                    entry.Info.ErrorState = UploadDirectoryError.Unknown;
                }
            }
        }

        public static bool IsNestedUnder(Android.Net.Uri childUri, Android.Net.Uri parentUri)
        {
            string child = childUri?.LastPathSegment;
            string parent = parentUri?.LastPathSegment;
            if (string.IsNullOrEmpty(child) || string.IsNullOrEmpty(parent))
            {
                return false;
            }
            if (child.Length <= parent.Length || !child.StartsWith(parent, StringComparison.Ordinal))
            {
                return false;
            }
            // Require a real path boundary right after the parent id, so "Music" doesn't match "Music2": either the
            // next char is the path separator, or the parent is a volume root ending in ':' (e.g. "primary:").
            char boundary = child[parent.Length];
            return boundary == '/' || parent.EndsWith(":", StringComparison.Ordinal);
        }

        private static void RecomputeSubdirFlags()
        {
            // Snapshot once so a concurrent copy-on-write swap can't tear Count vs. [i].
            var dirs = _uploadDirectories;
            for (int i = 0; i < dirs.Count; i++)
            {
                UploadDirectoryEntry entry = dirs[i];
                var ourUri = Android.Net.Uri.Parse(entry.Info.UploadDataDirectoryUri);

                entry.IsSubdir = false;
                for (int j = 0; j < dirs.Count; j++)
                {
                    if (i != j)
                    {
                        if (IsNestedUnder(ourUri, Android.Net.Uri.Parse(dirs[j].Info.UploadDataDirectoryUri)))
                        {
                            entry.IsSubdir = true;
                        }
                    }
                }
            }
        }

        private static void RebuildLockedAndHiddenPrefixLists()
        {
            PresentableNameLockedDirectories.Clear();
            PresentableNameHiddenDirectories.Clear();
            // Snapshot once so a concurrent copy-on-write swap can't tear Count vs. [i].
            var dirs = _uploadDirectories;
            for (int i = 0; i < dirs.Count; i++)
            {
                UploadDirectoryEntry entry = dirs[i];
                if (!entry.Info.IsLocked && !entry.Info.IsHidden)
                {
                    continue;
                }

                if (!entry.IsSubdir)
                {
                    if (entry.Info.IsLocked)
                    {
                        PresentableNameLockedDirectories.Add(entry.GetPresentableName());
                    }

                    if (entry.Info.IsHidden)
                    {
                        PresentableNameHiddenDirectories.Add(entry.GetPresentableName());
                    }
                }
                else
                {
                    var ourUri = Android.Net.Uri.Parse(entry.Info.UploadDataDirectoryUri);

                    UploadDirectoryEntry ourTopLevelParent = null;

                    for (int j = 0; j < dirs.Count; j++)
                    {
                        if (i != j)
                        {
                            if (!dirs[j].IsSubdir && IsNestedUnder(ourUri, Android.Net.Uri.Parse(dirs[j].Info.UploadDataDirectoryUri)))
                            {
                                ourTopLevelParent = dirs[j];
                                break;
                            }
                        }
                    }

                    if (entry.Info.HasError())
                    {
                        // error adding dir
                    }
                    else if (ourTopLevelParent == null)
                    {
                        Logger.Firebase("RebuildLockedAndHiddenPrefixLists: subdir has no non-subdir parent: " + entry.Info.UploadDataDirectoryUri);
                    }
                    else if (!ourTopLevelParent.Info.HasError())
                    {
                        if (entry.Info.IsLocked)
                        {
                            PresentableNameLockedDirectories.Add(entry.GetPresentableName(ourTopLevelParent));
                        }

                        if (entry.Info.IsHidden)
                        {
                            PresentableNameHiddenDirectories.Add(entry.GetPresentableName(ourTopLevelParent));
                        }
                    }
                }
            }
        }
    }
}
