// <copyright file="MessageBuilderExtensions.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging
{
    using System;

    /// <summary>
    ///     Extensions for <see cref="MessageBuilder"/>.
    /// </summary>
    /// <remarks>
    ///     This keeps domain logic out of the builder.
    /// </remarks>
    internal static class MessageBuilderExtensions
    {
        /// <summary>
        ///     Writes the specified <paramref name="file"/> to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The builder to which to write the file.</param>
        /// <param name="file">The file to write.</param>
        /// <returns>The builder.</returns>
        internal static MessageBuilder WriteFile(this MessageBuilder builder, File file)
        {
            file = file ?? throw new ArgumentNullException(nameof(file));

            builder
                .WriteByte((byte)file.Code)
                .WriteString(file.Filename)
                .WriteLong(file.Size)
                .WriteString(file.Extension)
                .WriteInteger(file.AttributeCount);

            foreach (var attribute in file.Attributes)
            {
                builder
                    .WriteInteger((int)attribute.Type)
                    .WriteInteger(attribute.Value);
            }

            return builder;
        }

        /// <summary>
        ///     Writes the specified <paramref name="directory"/> to the <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The builder to which to write the directory.</param>
        /// <param name="directory">The directory to write.</param>
        /// <returns>The builder.</returns>
        internal static MessageBuilder WriteDirectory(this MessageBuilder builder, Directory directory)
        {
            directory = directory ?? throw new ArgumentNullException(nameof(directory));

            builder
                .WriteString(directory.Name)
                .WriteInteger(directory.FileCount);

            foreach (var file in directory.Files)
            {
                builder.WriteFile(file);
            }

            return builder;
        }
    }
}
