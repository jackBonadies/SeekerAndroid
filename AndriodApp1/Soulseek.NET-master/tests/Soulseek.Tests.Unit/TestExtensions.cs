// <copyright file="TestExtensions.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;

    public static class TestExtensions
    {
        public static bool Matches(this byte[] a1, byte[] a2)
        {
            return a1.AsSpan().SequenceEqual(a2);
        }

        public static bool ContainsInsensitive(this string str, string match)
        {
            return str.Contains(match, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
