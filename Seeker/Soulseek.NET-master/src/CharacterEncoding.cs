// <copyright file="CharacterEncoding.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek
{
    /// <summary>
    ///     Character encodings.
    /// </summary>
    public class CharacterEncoding
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterEncoding"/> class.
        /// </summary>
        /// <param name="name">The encoding; UTF-8 or ISO-8859-1.</param>
        internal CharacterEncoding(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (name != "UTF-8" && name != "ISO-8859-1")
            {
                throw new System.ArgumentException("Invalid character encoding; must be one of UTF-8, ISO-8859-1", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        ///     Gets "ISO-8859-1" encoding.
        /// </summary>
        public static CharacterEncoding ISO88591 { get; } = new CharacterEncoding("ISO-8859-1");

        /// <summary>
        ///     Gets "UTF-8" encoding.
        /// </summary>
        public static CharacterEncoding UTF8 { get; } = new CharacterEncoding("UTF-8");

        private string Name { get; }

        /// <summary>
        ///     Converts an instance of <see cref="CharacterEncoding"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="encoding">The encoding to convert.</param>
        public static implicit operator string(CharacterEncoding encoding) => encoding.Name;

        /// <summary>
        ///     Gets the string representation of this encoding.
        /// </summary>
        /// <returns>The string representation of this encoding.</returns>
        public override string ToString() => Name;
    }
}