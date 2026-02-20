// <copyright file="DistributedParentEventArgs.cs" company="JP Dillingham">
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
    using System.Net;

    /// <summary>
    ///     Event arguments for the event raised when the distributed parent connection changes.
    /// </summary>
    public class DistributedParentEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedParentEventArgs"/> class.
        /// </summary>
        /// <param name="username">The username associated with the connection.</param>
        /// <param name="ipEndPoint">The IP endpoint of the connection.</param>
        /// <param name="branchLevel">The branch level of the parent.</param>
        /// <param name="branchRoot">The root of the distributed branch.</param>
        public DistributedParentEventArgs(string username, IPEndPoint ipEndPoint, int branchLevel, string branchRoot)
        {
            Username = username;
            IPEndPoint = ipEndPoint;
            BranchLevel = branchLevel;
            BranchRoot = branchRoot;
            IsBranchRoot = Username == BranchRoot && BranchLevel == 0;
        }

        /// <summary>
        ///     Gets the branch level of the parent.
        /// </summary>
        public int BranchLevel { get; }

        /// <summary>
        ///     Gets root of the distributed branch.
        /// </summary>
        public string BranchRoot { get; }

        /// <summary>
        ///     Gets the IP endpoint of the connection.
        /// </summary>
        public IPEndPoint IPEndPoint { get; }

        /// <summary>
        ///     Gets a value indicating whether the parent is a branch root.
        /// </summary>
        public bool IsBranchRoot { get; }

        /// <summary>
        ///     Gets the username associated with the connection.
        /// </summary>
        public string Username { get; }
    }
}