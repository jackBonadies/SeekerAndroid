// <copyright file="FileAttribute.cs" company="JP Dillingham">
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
    ///     A file attribute.
    /// </summary>
    public class FileAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FileAttribute"/> class.
        /// </summary>
        /// <param name="type">The attribute type.</param>
        /// <param name="value">The attribute value.</param>
        public FileAttribute(FileAttributeType type, int value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>
        ///     Gets the attribute type.
        /// </summary>
        public FileAttributeType Type { get; }

        /// <summary>
        ///     Gets the attribute value.
        /// </summary>
        public int Value { get; }
    }
}