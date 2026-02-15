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
using Seeker.Extensions.SearchResponseExtensions;
using Soulseek;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public class SearchResponseComparer : IEqualityComparer<SearchResponse>
    {
        private bool hideLockedResults = true;

        public SearchResponseComparer(bool _hideLocked)
        {
            hideLockedResults = _hideLocked;
        }

        public bool Equals(SearchResponse s1, SearchResponse s2)
        {
            if (s1.Username == s2.Username)
            {
                if (s1.Files.Count == s2.Files.Count)
                {
                    if (s1.Files.Count == 0)
                    {
                        return s1.LockedFiles.First().Filename == s2.LockedFiles.First().Filename;
                    }
                    if (s1.Files.First().Filename == s2.Files.First().Filename)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        public int GetHashCode(SearchResponse s1)
        {
            return s1.Username.GetHashCode() + s1.GetElementAtAdapterPosition(hideLockedResults, 0).Filename.GetHashCode();
        }
    }
}
