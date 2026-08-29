// <copyright file="DistributedEventArgsTests.cs" company="JP Dillingham">
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
    using System.Net;
    using AutoFixture.Xunit2;
    using Xunit;

    public class DistributedEventArgsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "DistributedParentEventArgs instantiates properly"), AutoData]
        public void DistributedParentEventArgs_Instantiates_Properly(string username, IPEndPoint ipEndPoint, int branchLevel, string branchRoot)
        {
            var args = new DistributedParentEventArgs(username, ipEndPoint, branchLevel, branchRoot);

            Assert.Equal(username, args.Username);
            Assert.Equal(ipEndPoint, args.IPEndPoint);
            Assert.Equal(branchLevel, args.BranchLevel);
            Assert.Equal(branchRoot, args.BranchRoot);
            Assert.False(args.IsBranchRoot);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "DistributedParentEventArgs IsBranchRoot returns true if user is branch root"), AutoData]
        public void DistributedParentEventArgs_IsBranchRoot_Returns_True_If_User_Is_Branch_Root(string username, IPEndPoint ipEndPoint)
        {
            var args = new DistributedParentEventArgs(username, ipEndPoint, branchLevel: 0, branchRoot: username);

            Assert.Equal(username, args.Username);
            Assert.Equal(ipEndPoint, args.IPEndPoint);
            Assert.Equal(0, args.BranchLevel);
            Assert.Equal(username, args.BranchRoot);
            Assert.True(args.IsBranchRoot);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "DistributedParentEventArgs nstantiates properly"), AutoData]
        public void DistributedChildEventArgs_Instantiates_Properly(string username, IPEndPoint ipEndPoint)
        {
            var args = new DistributedChildEventArgs(username, ipEndPoint);

            Assert.Equal(username, args.Username);
            Assert.Equal(ipEndPoint, args.IPEndPoint);
        }
    }
}
