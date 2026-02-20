using Common;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Seeker
{
    public static class SimpleHelpers
    {
        public static readonly string LOCK_EMOJI = char.ConvertFromUtf32(0x1F512);

        public static string AvoidLineBreaks(string orig)
        {
            return orig.Replace(' ', '\u00A0').Replace("\\", "\\\u2060");
        }

        /// <summary>
        /// This is necessary since DocumentFile.ListFiles() returns files in an incomprehensible order (not by name, size, modified, inode, etc.)
        /// </summary>
        public static void SortSlskDirFiles(List<Soulseek.File> files)
        {
            files.Sort((x, y) => x.Filename.CompareTo(y.Filename));
        }

        public static string GenerateIncompleteFolderName(string username, string fullFileName, int depth)
        {
            string albumFolderName = null;
            if (depth == 1)
            {
                albumFolderName = Common.Helpers.GetFolderNameFromFile(fullFileName, depth);
            }
            else
            {
                albumFolderName = Common.Helpers.GetFolderNameFromFile(fullFileName, depth);
                albumFolderName = albumFolderName.Replace('\\', '_');
            }
            string incompleteFolderName = username + "_" + albumFolderName;
            //Path.GetInvalidPathChars() doesnt seem like enough bc I still get failures on ''' and '&'
            foreach (char c in System.IO.Path.GetInvalidPathChars().Union(new[] { '&', '\'' }))
            {
                incompleteFolderName = incompleteFolderName.Replace(c, '_');
            }
            return incompleteFolderName;
        }

        private static string GetLockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.LockedFiles.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }
        private static string GetUnlockedFileName(SearchResponse item)
        {
            try
            {
                Soulseek.File f = item.Files.First();
                return f.Filename;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// This will prepend the lock when applicable..
        /// </summary>
        /// <returns></returns>
        public static string GetFolderNameForSearchResult(SearchResponse item)
        {
            if (item.FileCount > 0)
            {
                return Common.Helpers.GetFolderNameFromFile(GetUnlockedFileName(item));
            }
            else if (item.LockedFileCount > 0)
            {
                return LOCK_EMOJI + Common.Helpers.GetFolderNameFromFile(GetLockedFileName(item));
            }
            else
            {
                return "\\Locked\\";
            }
        }

        public static bool IsFileUri(string uriString)
        {
            if (uriString.StartsWith("file:"))
            {
                return true;
            }
            else if (uriString.StartsWith("content:"))
            {
                return false;
            }
            else
            {
                throw new Exception("IsFileUri failed: " + uriString);
            }
        }

        [Flags]
        public enum SpecialMessageType : short
        {
            None = 0,
            SlashMe = 1,
            MagnetLink = 2,
            SlskLink = 4,
        }

        /// <summary>
        /// true if '/me ' message
        /// </summary>
        /// <returns>true if special message</returns>
        public static bool IsSpecialMessage(string msg, out SpecialMessageType specialMessageType)
        {
            specialMessageType = SpecialMessageType.None;
            if (string.IsNullOrEmpty(msg))
            {
                return false;
            }
            if (msg.StartsWith(@"/me "))
            {
                specialMessageType = SpecialMessageType.SlashMe;
                return true;
            }
            if (msg.Contains(@"magnet:?xt=urn:"))
            {
                specialMessageType = SpecialMessageType.MagnetLink;
                return true;
            }
            if (msg.Contains(@"slsk://"))
            {
                specialMessageType = SpecialMessageType.SlskLink;
                return true;
            }
            return false;
        }

        public static readonly Regex MagnetLinkRegex = new Regex(@"magnet:\?xt=urn:[^ ""]+");
        public static readonly Regex SlskLinkRegex = new Regex(@"slsk://[^ ""]+");

        public static string ParseSpecialMessage(string msg)
        {
            if (IsSpecialMessage(msg, out SpecialMessageType specialMessageType))
            {
                //if slash me dont include other special links, too excessive.
                if (specialMessageType == SpecialMessageType.SlashMe)
                {
                    //"/me goes to the store"
                    //"goes to the store" + style
                    return msg.Substring(4, msg.Length - 4);
                }
                else
                {
                    return msg;
                }
            }
            else
            {
                return msg;

            }
        }

        public static DateTime GetDateTimeNowSafe()
        {
            try
            {
                return DateTime.Now;
            }
            catch (System.TimeZoneNotFoundException)
            {
                return DateTime.UtcNow;
            }
        }

        public const string NoDocumentOpenTreeToHandle = "No Activity found to handle Intent";

        public static bool IsUploadCompleteOrAborted(TransferStates state)
        {
            return (state.HasFlag(TransferStates.Succeeded) || state.HasFlag(TransferStates.Cancelled) || state.HasFlag(TransferStates.Errored) || state.HasFlag(TransferStates.TimedOut) || state.HasFlag(TransferStates.Completed) || state.HasFlag(TransferStates.Rejected));
        }

        public static string SlskLinkClickedData = null;
        public static bool ShowSlskLinkContextMenu = false;

        public static string GetTransferSpeedString(double bytesPerSecond)
        {
            if (bytesPerSecond > 1048576) //more than 1MB
            {
                return string.Format("{0:F1}mbs", bytesPerSecond / 1048576.0);
            }
            else
            {
                return string.Format("{0:F1}kbs", bytesPerSecond / 1024.0);
            }
        }

        public static string GetDateTimeSinceAbbrev(DateTime dtThen)
        {
            var dtNow = GetDateTimeNowSafe(); //2.5 microseconds
            if (dtNow.Day == dtThen.Day)
            {
                //if on same day then show time. 24 hour time? maybe option to change?
                //ex. 2:45, 20:34
                //hh:mm
                return dtThen.ToString("H:mm");
            }
            else if (dtNow.Year == dtThen.Year)
            {
                //if different day but same year show month day
                //ex. Jan 4
                return dtThen.ToString("MMM d"); // d = 7 or 17.
            }
            else
            {
                //if different year show full.
                //ex. Dec 30 2021
                return dtThen.ToString("MMM d yyyy");
            }
        }

        public static string GetSubHeaderText(SearchResponse searchResponse)
        {
            int numFiles = 0;
            long totalBytes = -1;
            if (PreferencesState.HideLockedResultsInSearch)
            {
                numFiles = searchResponse.FileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size);
            }
            else
            {
                numFiles = searchResponse.FileCount + searchResponse.LockedFileCount;
                totalBytes = searchResponse.Files.Sum(f => f.Size) + searchResponse.LockedFiles.Sum(f => f.Size);
            }

            //if total bytes greater than 1GB
            string sizeString = GetHumanReadableSize(totalBytes);

            var filesWithLength = searchResponse.Files.Where(f => f.Length.HasValue);
            if (!PreferencesState.HideLockedResultsInSearch)
            {
                filesWithLength = filesWithLength.Concat(searchResponse.LockedFiles.Where(f => f.Length.HasValue));
            }
            string timeString = null;
            if (filesWithLength.Count() > 0)
            {
                //translate length into human readable
                timeString = GetHumanReadableTime(filesWithLength.Sum(f => f.Length.Value));
            }
            if (timeString == null)
            {
                return string.Format("{0} files • {1}", numFiles, sizeString);
            }
            else
            {
                return string.Format("{0} files • {1} • {2}", numFiles, sizeString, timeString);
            }


        }

        public static string GetSizeLengthAttrString(Soulseek.File f)
        {

            string sizeString = string.Format("{0:0.##} mb", f.Size / (1024.0 * 1024.0));
            string lengthString = f.Length.HasValue ? GetHumanReadableTime(f.Length.Value) : null;
            string attrString = GetHumanReadableAttributesForSingleItem(f);
            if (attrString == null && lengthString == null)
            {
                return sizeString;
            }
            else if (attrString == null)
            {
                return String.Format("{0} • {1}", sizeString, lengthString);
            }
            else if (lengthString == null)
            {
                return String.Format("{0} • {1}", sizeString, attrString);
            }
            else
            {
                return String.Format("{0} • {1} • {2}", sizeString, lengthString, attrString);
            }
        }


        public static string GetHumanReadableAttributesForSingleItem(Soulseek.File f)
        {

            int bitRate = -1;
            int bitDepth = -1;
            double sampleRate = double.NaN;
            foreach (var attr in f.Attributes)
            {
                switch (attr.Type)
                {
                    case FileAttributeType.BitRate:
                        bitRate = attr.Value;
                        break;
                    case FileAttributeType.BitDepth:
                        bitDepth = attr.Value;
                        break;
                    case FileAttributeType.SampleRate:
                        sampleRate = attr.Value / 1000.0;
                        break;
                }
            }
            if (bitRate == -1 && bitDepth == -1 && double.IsNaN(sampleRate))
            {
                return null; //nothing to add
            }
            else if (bitDepth != -1 && !double.IsNaN(sampleRate))
            {
                return bitDepth + ", " + sampleRate + SimpleHelpers.STRINGS_KHZ;
            }
            else if (!double.IsNaN(sampleRate))
            {
                return sampleRate + SimpleHelpers.STRINGS_KHZ;
            }
            else if (bitRate != -1)
            {
                return bitRate + SimpleHelpers.STRINGS_KBS;
            }
            else
            {
                return null;
            }
        }

        public static string GetHumanReadableSize(long totalBytes)
        {
            if (totalBytes > 1024 * 1024 * 1024)
            {
                return string.Format("{0:0.##} gb", totalBytes / (1024.0 * 1024.0 * 1024.0));
            }
            else
            {
                return string.Format("{0:0.##} mb", totalBytes / (1024.0 * 1024.0));
            }
        }


        public static string GetHumanReadableTime(int totalSeconds)
        {
            int sec = totalSeconds % 60;
            int minutes = (totalSeconds % 3600) / 60;
            int hours = (totalSeconds / 3600);
            if (minutes == 0 && hours == 0 && sec == 0)
            {
                return null;
            }
            else if (minutes == 0 && hours == 0)
            {
                return string.Format("{0}s", sec);
            }
            else if (hours == 0)
            {
                return string.Format("{0}m{1}s", minutes, sec);
            }
            else
            {
                return string.Format("{0}h{1}m{2}s", hours, minutes, sec);
            }
        }


        /// <summary>
        /// Get all BUT the filename
        /// </summary>
        public static string GetDirectoryRequestFolderName(string filename)
        {
            try
            {
                int end = filename.LastIndexOf("\\");
                string clipped = filename.Substring(0, end);
                return clipped;
            }
            catch
            {
                return "";
            }
        }

        public static string GetFileNameFromFile(string filename) //is also used to get the last folder
        {
            int begin = filename.LastIndexOf("\\");
            string clipped = filename.Substring(begin + 1);
            return clipped;
        }

        public static IUserListService UserListService;
        //this is a cache for localized strings accessed in tight loops...
        private static string strings_kbs;
        public static string STRINGS_KBS
        {
            get
            {
                return strings_kbs;
            }
            set
            {
                strings_kbs = value;
            }
        }

        private static string strings_kHz;
        public static string STRINGS_KHZ
        {
            get
            {
                return strings_kHz;
            }
            set
            {
                strings_kHz = value;
            }
        }

        public static UserData UserStatisticsToUserData(UserStatistics stats)
        {
            return new UserData(stats.Username, UserPresence.Online, stats.AverageSpeed, stats.UploadCount, stats.FileCount, stats.DirectoryCount, null);
        }

        static SimpleHelpers()
        {
            KNOWN_TYPES = new List<string>() { ".mp3", ".flac", ".wav", ".aiff", ".wma", ".aac" }.AsReadOnly();
        }
        public static ReadOnlyCollection<string> KNOWN_TYPES;
    }
}
