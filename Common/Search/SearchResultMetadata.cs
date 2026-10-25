using Seeker;
using Seeker.Extensions.SearchResponseExtensions;
using Soulseek;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Common.Search
{
    /// <summary>
    /// User visible labels used by <see cref="SearchResultMetadata"/>. Kept separate so the
    /// formatter stays Android free and unit testable while the UI can pass localized strings.
    /// </summary>
    public sealed class SearchMetadataLabels
    {
        public string BitrateUnit { get; set; } = "kbps";
        public string SpeedUnit { get; set; } = "kb/s";
        public string FreeSlot { get; set; } = "Free slot";
        public string Queue { get; set; } = "Queue";
        public string Files { get; set; } = "files";
        public string File { get; set; } = "file";
        public string Separator { get; set; } = " \u2022 ";

        public static SearchMetadataLabels Default { get; } = new SearchMetadataLabels();

        public static SearchMetadataLabels Create(
            string bitrateUnit,
            string speedUnit,
            string freeSlot,
            string queue,
            string files,
            string file = null)
        {
            return new SearchMetadataLabels
            {
                BitrateUnit = bitrateUnit ?? Default.BitrateUnit,
                SpeedUnit = speedUnit ?? Default.SpeedUnit,
                FreeSlot = freeSlot ?? Default.FreeSlot,
                Queue = queue ?? Default.Queue,
                Files = files ?? Default.Files,
                File = file ?? Default.File,
            };
        }
    }

    /// <summary>
    /// Pure metadata projection of a search result row. It does not touch Android and only
    /// uses the existing search extensions / formatting helpers, so it can be covered by
    /// UnitTestCommon without a device. One instance is produced per row bind; the expensive
    /// dominant type/bitrate calculation is already cached on <see cref="SearchResponse"/>.
    /// </summary>
    public sealed class SearchResultMetadata
    {
        /// <summary>Name of the representative (first) file of the result.</summary>
        public string FileName { get; private set; } = string.Empty;

        /// <summary>Folder of the group. A <see cref="SearchResponse"/> row is a folder/group result.</summary>
        public string FolderName { get; private set; } = string.Empty;

        public string Peer { get; private set; } = string.Empty;
        public string FormatName { get; private set; } = string.Empty;
        public string DominantFileType { get; private set; } = string.Empty;
        public string Bitrate { get; private set; } = string.Empty;
        public string Size { get; private set; } = string.Empty;
        public string Duration { get; private set; } = string.Empty;
        public string Speed { get; private set; } = string.Empty;
        public string Slots { get; private set; } = string.Empty;
        public string FileCountText { get; private set; } = string.Empty;
        public int FileCount { get; private set; }
        public bool HasFreeUploadSlot { get; private set; }
        public bool IsLockedOnly { get; private set; }

        /// <summary>True when the row stands for more than one file, i.e. a folder/group.</summary>
        public bool IsGroup { get; private set; }

        /// <summary>
        /// Primary visible row identity: the folder for a group, the file name for a single
        /// file result. Rows are group results, so the folder must stay visible.
        /// </summary>
        public string PrimaryText { get; private set; } = string.Empty;

        /// <summary>
        /// Secondary identity, the one not used by <see cref="PrimaryText"/>: the representative
        /// file name for a group, the folder for a single file result.
        /// </summary>
        public string SecondaryText { get; private set; } = string.Empty;

        /// <summary>
        /// Facts of the representative file only (size, length, that file's own quality).
        /// Never a mix of one file's size and a group aggregate bitrate.
        /// </summary>
        public string FileDetail { get; private set; } = string.Empty;

        /// <summary>
        /// Compact line of the metadata not already shown by a dedicated row field, led by the
        /// secondary identity so a group row shows its representative file name.
        /// </summary>
        public string DetailLine { get; private set; } = string.Empty;

        /// <summary>
        /// <see cref="DetailLine"/> with the file count prefixed, for variants without a
        /// dedicated file count field.
        /// </summary>
        public string DetailLineWithFileCount { get; private set; } = string.Empty;

        /// <summary>Everything except the primary identity and peer, useful for accessibility.</summary>
        public string Summary { get; private set; } = string.Empty;

        /// <summary>Single line used by the compact variant, which has no peer field.</summary>
        public string CompactLine { get; private set; } = string.Empty;

        public static SearchResultMetadata Create(
            SearchResponse response,
            bool hideLocked,
            SearchMetadataLabels labels = null)
        {
            labels ??= SearchMetadataLabels.Default;
            var meta = new SearchResultMetadata();
            if (response == null)
            {
                return meta;
            }

            meta.Peer = response.Username ?? string.Empty;
            meta.HasFreeUploadSlot = response.HasFreeUploadSlot;
            meta.FileCount = hideLocked ? response.FileCount : response.FileCount + response.LockedFileCount;
            meta.IsLockedOnly = response.FileCount == 0 && response.LockedFileCount != 0;
            meta.IsGroup = meta.FileCount > 1;

            Soulseek.File first = response.Files.FirstOrDefault();
            if (first == null && !hideLocked)
            {
                first = response.LockedFiles.FirstOrDefault();
            }

            if (first != null)
            {
                string path = first.Filename ?? string.Empty;
                string name = SimpleHelpers.GetFileNameFromFile(path).ToString();
                string folder = SimpleHelpers.GetFolderNameFromFile(path).ToString();
                if (meta.IsLockedOnly)
                {
                    name = SimpleHelpers.LOCK_EMOJI + name;
                    folder = SimpleHelpers.LOCK_EMOJI + folder;
                }
                meta.FileName = name;
                meta.FolderName = folder;

                if (first.Size > 0)
                {
                    meta.Size = SimpleHelpers.GetHumanReadableSize(first.Size);
                }
                if (first.Length.HasValue && first.Length.Value > 0)
                {
                    meta.Duration = SimpleHelpers.GetHumanReadableTime(first.Length.Value, true);
                }

                // The dominant type/quality belongs to the whole group and stays in the format
                // chip. The row metadata must only describe the representative file itself.
                meta.DominantFileType = response.GetDominantFileTypeAndBitRate(hideLocked, out _);
                meta.FormatName = ExtractFormatName(meta.DominantFileType);
                meta.Bitrate = FormatFileBitrate(first, labels);
            }
            else
            {
                meta.DominantFileType = string.Empty;
            }

            if (response.UploadSpeed > 0)
            {
                meta.Speed = (response.UploadSpeed / 1024).ToString(CultureInfo.InvariantCulture) + " " + labels.SpeedUnit;
            }

            meta.Slots = response.HasFreeUploadSlot
                ? labels.FreeSlot
                : labels.Queue + " " + response.QueueLength.ToString(CultureInfo.InvariantCulture);

            meta.FileCountText = meta.FileCount.ToString(CultureInfo.InvariantCulture) + " "
                + (meta.FileCount == 1 ? labels.File : labels.Files);

            meta.PrimaryText = meta.IsGroup && !string.IsNullOrEmpty(meta.FolderName)
                ? meta.FolderName
                : meta.FileName;
            meta.SecondaryText = meta.IsGroup ? meta.FileName : meta.FolderName;
            if (meta.SecondaryText == meta.PrimaryText)
            {
                meta.SecondaryText = string.Empty;
            }

            meta.FileDetail = JoinNonEmpty(
                labels.Separator,
                meta.Size,
                meta.Duration,
                meta.Bitrate);

            // The count only matters visually when the row stands for more than one file.
            string groupCount = meta.IsGroup ? meta.FileCountText : string.Empty;

            meta.DetailLine = JoinNonEmpty(
                labels.Separator,
                meta.SecondaryText,
                meta.FileDetail);

            meta.DetailLineWithFileCount = JoinNonEmpty(
                labels.Separator,
                groupCount,
                meta.DetailLine);

            meta.Summary = JoinNonEmpty(
                labels.Separator,
                meta.SecondaryText,
                meta.FormatName,
                meta.Size,
                meta.Duration,
                meta.Bitrate,
                meta.Speed,
                meta.Slots,
                groupCount);

            meta.CompactLine = JoinNonEmpty(
                labels.Separator,
                meta.Peer,
                meta.Summary);

            return meta;
        }

        private static string ExtractFormatName(string dominantFileType)
        {
            if (string.IsNullOrWhiteSpace(dominantFileType))
            {
                return string.Empty;
            }
            string trimmed = dominantFileType.Trim();
            int end = trimmed.IndexOfAny(new[] { ' ', '(' });
            return end > 0 ? trimmed.Substring(0, end) : trimmed;
        }

        /// <summary>
        /// Bitrate reported by the representative file itself. The group's dominant quality is
        /// intentionally not reused here: pairing one file's size/length with the group bitrate
        /// would read as a group aggregate that the row does not have.
        /// </summary>
        private static string FormatFileBitrate(Soulseek.File file, SearchMetadataLabels labels)
        {
            if (file?.Attributes == null)
            {
                return string.Empty;
            }

            int bitRate = -1;
            foreach (FileAttribute attribute in file.Attributes)
            {
                if (attribute.Type == FileAttributeType.BitRate)
                {
                    bitRate = attribute.Value;
                }
            }

            return bitRate > 0
                ? FormatNumber(bitRate) + " " + labels.BitrateUnit
                : string.Empty;
        }

        private static string FormatNumber(double value)
        {
            return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
        }

        private static string JoinNonEmpty(string separator, params string[] parts)
        {
            if (parts == null)
            {
                return string.Empty;
            }
            var nonEmpty = new List<string>();
            foreach (string part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    nonEmpty.Add(part.Trim());
                }
            }
            return string.Join(separator, nonEmpty);
        }
    }
}
