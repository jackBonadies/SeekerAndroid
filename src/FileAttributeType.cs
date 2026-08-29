// <copyright file="FileAttributeType.cs" company="JP Dillingham">
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
    /// <summary>
    ///     File attribute types.
    /// </summary>
    public enum FileAttributeType
    {
        /// <summary>
        ///     Bit rate, in kbps.
        /// </summary>
        BitRate = 0,

        /// <summary>
        ///     Length, in seconds.
        /// </summary>
        Length = 1,

        /// <summary>
        ///     Variable bit rate flag; 0 = constant, 1 = VBR.
        /// </summary>
        VariableBitRate = 2,

        /// <summary>
        ///     Sample rate, in khz.
        /// </summary>
        SampleRate = 4,

        /// <summary>
        ///     Bit depth, in bits.
        /// </summary>
        BitDepth = 5,
    }
}