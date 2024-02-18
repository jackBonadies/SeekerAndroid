// <copyright file="SearchResponse.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A response to a file search.
    /// </summary>
    [System.Serializable]
    public class SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The file list.</param>
        /// <param name="lockedFileList">The optional locked file list.</param>
        public SearchResponse(string username, int token, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
        {
            Username = username;
            Token = token;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;

            LockedFiles = (lockedFileList?.ToList() ?? new List<File>()).AsReadOnly();
            LockedFileCount = LockedFiles.Count;
        }

        //public override int GetHashCode()
        //{
        //    int hash = this.Username.GetHashCode();
        //    if(this.Files.Count!=0)
        //    { 
        //        hash = (hash*7) + this.Files.First().Filename.GetHashCode();
        //    }
        //    return hash;
        //}

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            if (this.Username  != ((SearchResponse)(obj)).Username)
            {
                return false;
            }
            if(this.Files.Count != 0 && ((SearchResponse)(obj)).Files.Count != 0)
            {
                return this.Files.First().Filename == ((SearchResponse)(obj)).Files.First().Filename;
            }
            else
            {
                return true;
            }
        }

        private string cachedDominantFileType = null;
        private double cachedCalcBitRate = double.NaN;
        //we used to do this in SetItem.  That might get called too many times.. so lets cache the result so that we only do it once.
        //similar to before we only compute it when we actually need it (i.e. it scrolls into view).
        //if hideLocked is true, then only iterate over unlocked files. else iterate over everything.
        public string GetDominantFileType(bool hideLocked, out double calcBitRate)
        {
            if(!string.IsNullOrEmpty(cachedDominantFileType))
            {
                calcBitRate = cachedCalcBitRate;
                return cachedDominantFileType;
            }
            //basically this works in two ways.  if the first file has a type of .mp3, .flac, .wav, .aiff, .wma, .aac then thats likely the type.
            //if not then we do a more expensive parsing, where we get the most common
            string ext = System.IO.Path.GetExtension(this.FileCount > 0 ? this.Files.First().Filename : this.LockedFiles.First().Filename);  //do not use Soulseek.File.Extension that will be "" most of the time...
            string dominantTypeToReturn = "";
            if (SlskHelp.CommonHelpers.KNOWN_TYPES.Contains(ext))
            {
                dominantTypeToReturn = ext;
            }
            else
            {
                Dictionary<string, int> countTypes = new Dictionary<string, int>();
                ext = "";
                var toIterate1 = hideLocked ? this.Files : this.Files.Concat(this.LockedFiles);
                foreach (Soulseek.File f in toIterate1)
                {
                    ext = System.IO.Path.GetExtension(f.Filename);
                    if (countTypes.ContainsKey(ext))
                    {
                        countTypes[ext] = countTypes[ext] + 1;
                    }
                    else
                    {
                        countTypes.Add(ext, 1);
                    }
                }
                string dominantType = "";
                int count = 0;
                foreach (var pair in countTypes)
                {
                    if (pair.Value > count)
                    {
                        dominantType = pair.Key;
                        count = pair.Value;
                    }
                    else if (pair.Value == count)
                    {
                        // if equal but this type is a known type and the other is not
                        // then replace it.  (ex. this is case of 1 mp3, 1 jpg)
                        if(SlskHelp.CommonHelpers.KNOWN_TYPES.Contains(pair.Key) &&
                            !SlskHelp.CommonHelpers.KNOWN_TYPES.Contains(dominantType))
                        {
                            dominantType = pair.Key;
                            count = pair.Value;
                        }
                    }
                }
                dominantTypeToReturn = dominantType;
            }
            //now get a representative file and get some extra info (if any)
            Soulseek.File representative = null;
            Soulseek.File representative2 = null;
            var toIterate = hideLocked ? this.Files : this.Files.Concat(this.LockedFiles);
            foreach (Soulseek.File f in toIterate)
            {
                if (representative == null && dominantTypeToReturn == System.IO.Path.GetExtension(f.Filename))
                {
                    representative = f;
                    continue;
                }
                if (dominantTypeToReturn == System.IO.Path.GetExtension(f.Filename))
                {
                    representative2 = f;
                    break;
                }
            }
            if (representative == null)
            {
                //shouldnt happen
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                cachedCalcBitRate = calcBitRate = -1;
                return cachedDominantFileType;
            }



            //vbr flags never work so just get two representative files and see if bitrate is same...


            bool isVbr = (representative.IsVariableBitRate == null) ? false : representative.IsVariableBitRate.Value;

            if (representative2 != null)
            {
                if (representative.BitRate != null && representative2.BitRate != null)
                {
                    if (representative.BitRate != representative2.BitRate)
                    {
                        isVbr = true;
                    }
                }
            }

            int bitRate = -1;
            int bitDepth = -1;
            double sampleRate = double.NaN;
            foreach (var attr in representative.Attributes)
            {
                switch (attr.Type)
                {
                    case FileAttributeType.VariableBitRate:
                        if (attr.Value == 1)
                        {
                            isVbr = true;
                        }
                        break;
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
            if (!isVbr && bitRate == -1 && bitDepth == -1 && double.IsNaN(sampleRate))
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                if(representative.Length.HasValue && representative.Length.Value > 0)
                {
                    // sometimes (though rarely) we do get length but not bitrate.
                    cachedCalcBitRate = calcBitRate = calcBitRateFromSizeAndLength(representative.Size, representative.Length.Value);
                }
                else
                {
                    cachedCalcBitRate = calcBitRate = -1;
                }
                return cachedDominantFileType; //nothing to add
            }
            else if (isVbr)
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (vbr)";
                cachedCalcBitRate = calcBitRate = bitRate; // using that of first file.
                return cachedDominantFileType;
            }
            else if (bitDepth != -1 && !double.IsNaN(sampleRate))
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + bitDepth + ", " + sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ + ")";
                cachedCalcBitRate = calcBitRate = bitDepth * sampleRate * 2;
                return cachedDominantFileType;
            }
            else if (!double.IsNaN(sampleRate))
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + sampleRate + SlskHelp.CommonHelpers.STRINGS_KHZ + ")";
                cachedCalcBitRate = calcBitRate = bitRate != -1 ? bitRate : 16 * sampleRate * 2;
                return cachedDominantFileType;
            }
            else if (bitRate != -1)
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + bitRate + SlskHelp.CommonHelpers.STRINGS_KBS + ")";
                cachedCalcBitRate = calcBitRate = bitRate;
                return cachedDominantFileType;
            }
            else
            {
                cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                cachedCalcBitRate = calcBitRate = -1;
                return cachedDominantFileType;
            }
        }

        /// <summary>
        /// returns kilobits per second
        /// </summary>
        private static double calcBitRateFromSizeAndLength(long bytes, int seconds)
        {
            return (8 * (bytes / 1024) / seconds);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="searchResponse">An existing instance from which to copy properties.</param>
        /// <param name="fileList">The file list with which to replace the existing file list.</param>
        /// <param name="lockedFileList">The optional locked file list with which to replace the existing locked file list.</param>
        internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList, IEnumerable<File> lockedFileList = null)
            : this(searchResponse.Username, searchResponse.Token, searchResponse.FreeUploadSlots, searchResponse.UploadSpeed, searchResponse.QueueLength, fileList, lockedFileList)
        {
        }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="Files"/>.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the list of files.
        /// </summary>
        public IReadOnlyCollection<File> Files { get; }

        /// <summary>
        ///     Gets the number of free upload slots for the peer.
        /// </summary>
        public int FreeUploadSlots { get; }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="LockedFiles"/>.
        /// </summary>
        public int LockedFileCount { get; }

        /// <summary>
        ///     Gets the list of locked files.
        /// </summary>
        public IReadOnlyCollection<File> LockedFiles { get; }

        /// <summary>
        ///     Gets the length of the peer's upload queue.
        /// </summary>
        public long QueueLength { get; }

        /// <summary>
        ///     Gets the unique search token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the upload speed of the peer.
        /// </summary>
        public int UploadSpeed { get; }

        /// <summary>
        ///     Gets the username of the responding peer.
        /// </summary>
        public string Username { get; }
    }
}