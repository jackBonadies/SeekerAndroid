using Common.Share;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

namespace Seeker.Extensions
{
    namespace SearchResponseExtensions
    {
        public static class SearchResponseExtensions
        {
            public static IEnumerable<Soulseek.File> GetFiles(this SearchResponse searchResponse, bool hideLocked)
            {
                return hideLocked ? searchResponse.Files : searchResponse.Files.Concat(searchResponse.LockedFiles);
            }

            public static bool IsLockedOnly(this SearchResponse searchResponse)
            {
                return searchResponse.FileCount == 0 && searchResponse.LockedFileCount != 0;
            }
            
            public static string GetDominantFileTypeAndBitRate(this SearchResponse searchResponse, bool hideLocked, out double calcBitRate)
            {
                if(!string.IsNullOrEmpty(searchResponse.cachedDominantFileType))
                {
                    calcBitRate = searchResponse.cachedCalcBitRate;
                    return searchResponse.cachedDominantFileType;
                }
                //basically this works in two ways.  if the first file has a type of .mp3, .flac, .wav, .aiff, .wma, .aac then thats likely the type.
                //if not then we do a more expensive parsing, where we get the most common
                string ext = System.IO.Path.GetExtension(searchResponse.FileCount > 0 ? searchResponse.Files.First().Filename : searchResponse.LockedFiles.First().Filename);  //do not use Soulseek.File.Extension that will be "" most of the time...
                string dominantTypeToReturn = "";
                if (SimpleHelpers.KNOWN_TYPES.Contains(ext))
                {
                    dominantTypeToReturn = ext;
                }
                else
                {
                    Dictionary<string, int> countTypes = new Dictionary<string, int>();
                    ext = "";
                    var toIterate1 = hideLocked ? searchResponse.Files : searchResponse.Files.Concat(searchResponse.LockedFiles);
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
                            if(SimpleHelpers.KNOWN_TYPES.Contains(pair.Key) &&
                                !SimpleHelpers.KNOWN_TYPES.Contains(dominantType))
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
                var toIterate = hideLocked ? searchResponse.Files : searchResponse.Files.Concat(searchResponse.LockedFiles);
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
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                    searchResponse.cachedCalcBitRate = calcBitRate = -1;
                    return searchResponse.cachedDominantFileType;
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
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                    if(representative.Length.HasValue && representative.Length.Value > 0)
                    {
                        // sometimes (though rarely) we do get length but not bitrate.
                        searchResponse.cachedCalcBitRate = calcBitRate = calcBitRateFromSizeAndLength(representative.Size, representative.Length.Value);
                    }
                    else
                    {
                        searchResponse.cachedCalcBitRate = calcBitRate = -1;
                    }
                    return searchResponse.cachedDominantFileType; //nothing to add
                }
                else if (isVbr)
                {
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (vbr)";
                    searchResponse.cachedCalcBitRate = calcBitRate = bitRate; // using that of first file.
                    return searchResponse.cachedDominantFileType;
                }
                else if (bitDepth != -1 && !double.IsNaN(sampleRate))
                {
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + bitDepth + ", " + sampleRate + SimpleHelpers.STRINGS_KHZ + ")";
                    searchResponse.cachedCalcBitRate = calcBitRate = bitDepth * sampleRate * 2;
                    return searchResponse.cachedDominantFileType;
                }
                else if (!double.IsNaN(sampleRate))
                {
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + sampleRate + SimpleHelpers.STRINGS_KHZ + ")";
                    searchResponse.cachedCalcBitRate = calcBitRate = bitRate != -1 ? bitRate : 16 * sampleRate * 2;
                    return searchResponse.cachedDominantFileType;
                }
                else if (bitRate != -1)
                {
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.') + " (" + bitRate + SimpleHelpers.STRINGS_KBS + ")";
                    searchResponse.cachedCalcBitRate = calcBitRate = bitRate;
                    return searchResponse.cachedDominantFileType;
                }
                else
                {
                    searchResponse.cachedDominantFileType = dominantTypeToReturn.TrimStart('.');
                    searchResponse.cachedCalcBitRate = calcBitRate = -1;
                    return searchResponse.cachedDominantFileType;
                }
            }

            /// <summary>
            /// returns kilobits per second
            /// </summary>
            private static double calcBitRateFromSizeAndLength(long bytes, int seconds)
            {
                return (8 * (bytes / 1024) / seconds);
            }

            public static Soulseek.File GetElementAtAdapterPosition(this SearchResponse searchResponse, bool hideLocked, int position)
            {
                if (hideLocked)
                {
                    return searchResponse.Files.ElementAt(position);
                }
                else
                {
                    //we always do Files, then LockedFiles
                    if (position >= searchResponse.Files.Count)
                    {
                        return searchResponse.LockedFiles.ElementAt(position - searchResponse.Files.Count);
                    }
                    else
                    {
                        return searchResponse.Files.ElementAt(position);
                    }

                }

            }
        }
    }
}
