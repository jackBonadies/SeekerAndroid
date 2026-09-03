// <copyright file="UserEventArgsTests.cs" company="JP Dillingham">
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
    using AutoFixture.Xunit2;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class UserEventArgsTests
    {
        [Trait("Category", "UserCannotConnectEventArgs Instantiation")]
        [Theory(DisplayName = "UserCannotConnectEventArgs Instantiates with the given data"), AutoData]
        public void UserCannotConnectEventArgs_Instantiates_With_The_Given_Data(int token, string username)
        {
            var e = new UserCannotConnectEventArgs(new CannotConnect(token, username));

            Assert.Equal(username, e.Username);
            Assert.Equal(token, e.Token);
        }

        [Trait("Category", "DownloadDeniedEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadDeniedEventArgs Instantiates with the given data"), AutoData]
        public void DownloadDeniedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, string message)
        {
            var e = new DownloadDeniedEventArgs(username, filename, message);

            Assert.Equal(username, e.Username);
            Assert.Equal(filename, e.Filename);
            Assert.Equal(message, e.Message);
        }

        [Trait("Category", "DownloadFailedEventArgs Instantiation")]
        [Theory(DisplayName = "DownloadFailedEventArgs Instantiates with the given data"), AutoData]
        public void DownloadFailedEventArgs_Instantiates_With_The_Given_Data(string username, string filename)
        {
            var e = new DownloadFailedEventArgs(username, filename);

            Assert.Equal(username, e.Username);
            Assert.Equal(filename, e.Filename);
        }
    }
}
