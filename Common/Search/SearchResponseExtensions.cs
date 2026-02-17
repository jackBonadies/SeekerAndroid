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
