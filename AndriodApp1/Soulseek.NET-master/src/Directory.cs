// <copyright file="Directory.cs" company="JP Dillingham">
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
    ///     A file directory within a peer's shared files.
    /// </summary>
    [System.Serializable]
    public class Directory
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Directory"/> class.
        /// </summary>
        /// <param name="name">The directory name.</param>
        /// <param name="fileList">The optional list of <see cref="File"/> s.</param>
        public Directory(string name, IEnumerable<File> fileList = null)
        {
            Name = name;

            Files = (fileList?.ToList() ?? new List<File>()).AsReadOnly();
            FileCount = Files.Count;
        }

        //for serializer..
        private Directory()
        {

        }

        /// <summary>
        ///     Gets the directory name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the number of files within the directory.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the collection of files contained within the directory.
        /// </summary>
        public IReadOnlyCollection<File> Files { get; }

        /// <summary>
        ///     Gets the collection of files in alphabetical order.
        /// </summary>
        public IEnumerable<File> OrderedFiles
        {
            get
            {
                return this.Files.OrderBy(x => x.Filename);
            }
        }

        public Directory DeepCopy()
        {
            Directory d = new Directory(this.Name,this.Files.ToList()); //this creates a new list.. you can add or remove without affecting original...
            return d;
        }
    }
}