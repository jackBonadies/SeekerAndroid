// <copyright file="TransferEventArgsTests.cs" company="JP Dillingham">
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
    using Xunit;

    public class TransferEventArgsTests
    {
        [Trait("Category", "TransferEventArgs Instantiation")]
        [Theory(DisplayName = "TransferEventArgs Instantiates with the given data"), AutoData]
        internal void TransferEventArgs_Instantiates_With_The_Given_Data(TransferDirection direction, string username, string filename, int token, TransferOptions options)
        {
            var dl = new TransferInternal(direction, username, filename, token, options);
            var xfer = new Transfer(dl);
            var d = new TransferEventArgs(xfer);

            Assert.Equal(xfer, d.Transfer);
        }

        [Trait("Category", "TransferProgressUpdatedEventArgs Instantiation")]
        [Theory(DisplayName = "TransferProgressUpdatedEventArgs Instantiates with the given data"), AutoData]
        internal void TransferProgressUpdatedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, int size, int bytesDownloaded)
        {
            var dl = new TransferInternal(TransferDirection.Download, username, filename, token)
            {
                Size = size,
            };

            var d = new TransferProgressUpdatedEventArgs(bytesDownloaded, new Transfer(dl));

            Assert.Equal(bytesDownloaded, d.PreviousBytesTransferred);
        }

        [Trait("Category", "TransferStateChangedEventArgs Instantiation")]
        [Theory(DisplayName = "TransferStateChangedEventArgs Instantiates with the given data"), AutoData]
        internal void TransferStateChangedEventArgs_Instantiates_With_The_Given_Data(string username, string filename, int token, TransferStates transferStates)
        {
            var dl = new TransferInternal(TransferDirection.Download, username, filename, token);
            var d = new TransferStateChangedEventArgs(transferStates, new Transfer(dl));

            Assert.Equal(transferStates, d.PreviousState);
        }
    }
}
