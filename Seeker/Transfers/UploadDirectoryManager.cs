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
                    UploadDirectories = new List<UploadDirectoryEntry>();
                    UploadDirectories.Add(uploadDir);

                    SaveToSharedPreferences(sharedPreferences);
                    var editor = sharedPreferences.Edit();
                    editor.PutString(KeyConsts.M_UploadDirectoryUri, string.Empty);
                    editor.Commit();
                }
                else
                {
                    UploadDirectories = new List<UploadDirectoryEntry>();
                }
            }
            else
            {
                var infos = SerializationHelper.DeserializeFromString<List<UploadDirectoryInfo>>(sharedDirInfo);
                UploadDirectories = infos.Select(info => new UploadDirectoryEntry(info)).ToList();
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
                    editor.Commit();
                }
            }
        }

        public static String UploadDataDirectoryUri = null;
        public static bool UploadDataDirectoryUriIsFromTree = true;

        public static List<UploadDirectoryEntry> UploadDirectories;

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
                    SharedFileService.GetAllFolderInfo(dirEntry, out _, out _, out _, out _, out string presentableName);
                    currentRootNames.Add(presentableName);
                }
            }
            SharedFileService.GetAllFolderInfo(newDirEntry, out _, out _, out _, out _, out string presentableNameNew);
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

        public static List<string> PresentableNameLockedDirectories = new List<string>();
        public static List<string> PresentableNameHiddenDirectories = new List<string>();

        public static void UpdateWithDocumentFileAndErrorStates()
        {
            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryEntry entry = UploadDirectories[i];

                Android.Net.Uri uploadDirUri = Android.Net.Uri.Parse(entry.Info.UploadDataDirectoryUri);
                try
                {
                    entry.Info.ErrorState = UploadDirectoryError.NoError;
                    if (SeekerState.PreOpenDocumentTree() || !entry.Info.UploadDataDirectoryUriIsFromTree)
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

            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryEntry entry = UploadDirectories[i];
                var ourUri = Android.Net.Uri.Parse(entry.Info.UploadDataDirectoryUri);

                for (int j = 0; j < UploadDirectories.Count; j++)
                {
                    if (i != j)
                    {
                        if (ourUri.LastPathSegment.Contains(Android.Net.Uri.Parse(UploadDirectories[j].Info.UploadDataDirectoryUri).LastPathSegment))
                        {
                            entry.IsSubdir = true;
                        }
                    }
                }
            }

            PresentableNameLockedDirectories.Clear();
            PresentableNameHiddenDirectories.Clear();
            for (int i = 0; i < UploadDirectories.Count; i++)
            {
                UploadDirectoryEntry entry = UploadDirectories[i];
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

                    for (int j = 0; j < UploadDirectories.Count; j++)
                    {
                        if (i != j)
                        {
                            if (!UploadDirectories[j].IsSubdir && ourUri.LastPathSegment.Contains(Android.Net.Uri.Parse(UploadDirectories[j].Info.UploadDataDirectoryUri).LastPathSegment))
                            {
                                ourTopLevelParent = UploadDirectories[j];
                                break;
                            }
                        }
                    }

                    if (!entry.Info.HasError() && !ourTopLevelParent.Info.HasError())
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
