using Android.Content;
using Common;
using Seeker.Helpers;
using Seeker.Managers;
using Seeker.Services;
using Seeker.Transfers;
using System;

namespace Seeker.Settings.Rows
{
    internal static class SettingValueFormat
    {
        public static string GetFriendlyDownloadDirectoryName()
        {
            if (StorageState.RootDocumentFile == null)            
            {
                if (PlatformInfo.UseLegacyStorage())
                {
                    //if not set and legacy storage, then the directory is simple the default music
                    string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                    return Android.Net.Uri.Parse(new Java.IO.File(path).ToURI().ToString()).LastPathSegment;
                }
                else
                {
                    //if not set and not legacy storage, then that is bad.  user must set it.
                    return SeekerApplication.GetString(Resource.String.NotSet);
                }
            }
            else
            {
                return StorageState.RootDocumentFile.Uri.LastPathSegment;
            }
        }

        public static string GetFriendlyIncompleteDirectoryName()
        {
            if (PreferencesState.MemoryBackedDownload)
            {
                return SeekerApplication.GetString(Resource.String.NotInUse);
            }
            if (PreferencesState.OverrideDefaultIncompleteLocations && StorageState.RootIncompleteDocumentFile != null) //if doc file is null that means we could not write to it.
            {
                return StorageState.RootIncompleteDocumentFile.Uri.LastPathSegment;
            }
            else
            {
                if (!PreferencesState.CreateCompleteAndIncompleteFolders)
                {
                    return SeekerApplication.GetString(Resource.String.AppLocalStorage);
                }
                //if not override then its whatever the download directory is...
                if (StorageState.RootDocumentFile == null)                
                {
                    if (PlatformInfo.UseLegacyStorage())
                    {
                        //if not set and legacy storage, then the directory is simple the default music
                        string path = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        return Android.Net.Uri.Parse(new Java.IO.File(path).ToURI().ToString()).LastPathSegment; //this is to prevent line breaks.
                    }
                    else
                    {
                        //if not set and not legacy storage, then that is bad.  user must set it.
                        return SeekerApplication.GetString(Resource.String.NotSet);
                    }
                }
                else
                {
                    return StorageState.RootDocumentFile.Uri.LastPathSegment;
                }
            }
        }

        public static string SpeedLimitSummary(Context ctx, bool enabled, int bytesPerSec, bool perTransfer)
        {
            if (!enabled) return ctx.GetString(Resource.String.speed_limit_off);
            int kbs = Math.Max(1, bytesPerSec / 1024);
            return string.Format(
                ctx.GetString(perTransfer
                    ? Resource.String.speed_limit_kbs_per_transfer
                    : Resource.String.speed_limit_kbs_total),
                kbs);
        }

        public static string ConcurrentLimitSummary(Context ctx, bool enabled, int value)
        {
            if (!enabled) return ctx.GetString(Resource.String.speed_limit_off);
            return value.ToString();
        }

        public static string LanguageLabel(Context ctx)
        {
            string current = LocaleHelper.GetLegacyLanguageString();
            foreach (var (label, value) in SettingPickers.BuildLanguageOptions(ctx))
            {
                if (value == current)
                {
                    return label;
                }
            }
            // Unknown / unsupported locale: show the raw identifier rather than nothing.
            return string.IsNullOrEmpty(current) ? ctx.GetString(Resource.String.Automatic) : current;
        }

        public static string DayNightModeLabel(Context ctx, int mode)
        {
            return mode switch
            {
                1 => ctx.GetString(Resource.String.always_light),
                2 => ctx.GetString(Resource.String.always_dark),
                _ => ctx.GetString(Resource.String.follow_system),
            };
        }

        public static string DayVariantLabel(Context ctx, DayThemeType v) => v switch
        {
            DayThemeType.ClassicPurple => ThemeHelper.ClassicPurple,
            DayThemeType.Red => ThemeHelper.Red,
            DayThemeType.Blue => ThemeHelper.Blue,
            DayThemeType.Grey => ThemeHelper.Grey,
            _ => v.ToString(),
        };

        public static string NightVariantLabel(Context ctx, NightThemeType v) => v switch
        {
            NightThemeType.ClassicPurple => ThemeHelper.ClassicPurple,
            NightThemeType.Grey => ThemeHelper.Grey,
            NightThemeType.Blue => ThemeHelper.Blue,
            NightThemeType.Red => ThemeHelper.Red,
            NightThemeType.AmoledClassicPurple => ThemeHelper.AmoledClassicPurple,
            NightThemeType.AmoledGrey => ThemeHelper.AmoledGrey,
            _ => v.ToString(),
        };


        public static string ParsingStatusText(Context ctx)
        {
            var status = Seeker.Services.SharedFileService.ParseStatus;
            if (status.IsFinishingUp)
            {
                return ctx.GetString(Resource.String.sharing_parsing_finishing);
            }
            if (status.NumberParsed > 0)
            {
                return string.Format(ctx.GetString(Resource.String.sharing_parsing_count), status.NumberParsed);
            }
            return ctx.GetString(Resource.String.sharing_currently_parsing);
        }

        // Per folder variant of ParsingStatusText
        public static string ParsingStatusText(Context ctx, UploadDirectoryEntry entry)
        {
            var status = Seeker.Services.SharedFileService.ParseStatus;
            int n = status.GetCountForRoot(entry.Info.UploadDataDirectoryUri);
            if (n >= 0)
            {
                return string.Format(ctx.GetString(Resource.String.sharing_parsing_count), n);
            }
            if (status.IsFinishingUp)
            {
                return ctx.GetString(Resource.String.sharing_parsing_finishing);
            }
            return ctx.GetString(Resource.String.sharing_currently_parsing);
        }

        /// <summary>Aggregate status shown on the "Enable sharing" toggle, e.g. "3 folders · 2,669 files".</summary>
        public static string SharedAggregateSummary(Context ctx)
        {
            int folders = 0;
            int files = 0;
            try
            {
                var dirs = UploadDirectoryManager.UploadDirectories;
                if (dirs != null) folders = dirs.Count;
                var cache = Seeker.Services.SharedFileService.SharedFileCache;
                if (cache != null) files = cache.FileCount;
            }
            catch (Exception ex)
            {
                Logger.FirebaseError("Error getting shared aggregate summary", ex);
            }
            return string.Format(ctx.GetString(Resource.String.shared_summary_folders_files), folders, files);
        }

        /// <summary>Line-2 text for a shared-folder row in its success state: the per-folder file
        /// count plus an optional Hidden/Locked suffix. Empty when the count isn't available yet.</summary>
        public static string SharedFolderSubtitle(Context ctx, UploadDirectoryEntry entry)
        {
            int count = SharedFolderFileCount(entry);
            string text;
            if (count < 0)
            {
                text = string.Empty;
            }
            else if (count == 1)
            {
                text = ctx.GetString(Resource.String.folder_file_count_one);
            }
            else
            {
                text = string.Format(ctx.GetString(Resource.String.folder_file_count), count);
            }

            if (entry.Info.IsHidden)
            {
                text = AppendStatus(text, ctx.GetString(Resource.String.folder_hidden));
            }
            else if (entry.Info.IsLocked)
            {
                text = AppendStatus(text, ctx.GetString(Resource.String.folder_locked));
            }
            return text;
        }

        private static string AppendStatus(string text, string suffix)
        {
            if (string.IsNullOrEmpty(suffix)) return text;
            return string.IsNullOrEmpty(text) ? suffix : text + " · " + suffix;
        }

        // Per-root file counts, memoized against the SharedFileCache instance they were derived from.
        private static object _countsSource;
        private static System.Collections.Generic.Dictionary<string, int> _rootFileCounts;

        /// <summary>Number of files shared under the given root folder, derived from the parsed
        /// browse tree. Returns -1 when the cache isn't available (not parsed yet).</summary>
        public static int SharedFolderFileCount(UploadDirectoryEntry entry)
        {
            // Prefer the live per-root count from the in-progress / most-recent parse. This is the
            // authoritative source for a folder that finished mid-parse (the SharedFileCache isn't
            // replaced until the whole parse completes) and persists afterwards.
            int live = Seeker.Services.SharedFileService.ParseStatus.GetCountForRoot(entry.Info.UploadDataDirectoryUri);
            if (live >= 0)
            {
                return live;
            }
            try
            {
                var cache = Seeker.Services.SharedFileService.SharedFileCache;
                var dict = cache?.PresentableNameToFullFileInfo;
                if (dict == null) return -1;
                EnsureRootCounts(cache, dict);
                string root = entry.GetPresentableName();
                if (string.IsNullOrEmpty(root)) return -1;
                return _rootFileCounts.TryGetValue(root, out int c) ? c : 0;
            }
            catch
            {
                return -1;
            }
        }

        private static void EnsureRootCounts(object cache,
            System.Collections.Generic.Dictionary<string, System.Tuple<long, string, System.Tuple<int, int, int, int>, bool, bool>> dict)
        {
            if (ReferenceEquals(_countsSource, cache) && _rootFileCounts != null) return;

            // Roots = presentable name of each (non-subdir) shared folder. A file belongs to the
            // longest root that is a prefix of its presentable path (keys look like "root\\sub\\file").
            var roots = new System.Collections.Generic.List<string>();
            foreach (var e in UploadDirectoryManager.UploadDirectories)
            {
                if (e.IsSubdir) continue;
                try
                {
                    string r = e.GetPresentableName();
                    if (!string.IsNullOrEmpty(r)) roots.Add(r);
                }
                catch (Exception ex)
                { 
                    Logger.FirebaseError("Error ensure roots", ex);
                    
                }
            }

            var counts = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var r in roots) counts[r] = 0;

            foreach (var key in dict.Keys)
            {
                string best = null;
                foreach (var r in roots)
                {
                    if ((key.Length == r.Length && key == r)
                        || (key.Length > r.Length && key.StartsWith(r + "\\", System.StringComparison.Ordinal)))
                    {
                        if (best == null || r.Length > best.Length) best = r;
                    }
                }
                if (best != null) counts[best] = counts[best] + 1;
            }

            _rootFileCounts = counts;
            _countsSource = cache;
        }

        public static string SmartFilterSummary(Context ctx)
        {
            var items = PreferencesState.SmartFilterOptions.GetAdapterItems();
            if (items == null || items.Count == 0) return string.Empty;
            var parts = new System.Collections.Generic.List<string>(items.Count);
            foreach (var item in items)
            {
                if (item.Enabled)
                {
                    parts.Add(item.Name);
                }
            }
            return string.Join(", ", parts);
        }
    }
}
