// <copyright file="DiagnosticEventArgsTests.cs" company="JP Dillingham">
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
    using Soulseek.Diagnostics;
    using Xunit;

    public class DiagnosticEventArgsTests
    {
        [Trait("Category", "DiagnosticEventArgs Instantiation")]
        [Theory(DisplayName = "DiagnosticEventArgs Instantiates with the given data"), AutoFixture.Xunit2.AutoData]
        public void Instantiates_With_The_Given_Data(DiagnosticLevel level, string message, Exception exception)
        {
            var e = new DiagnosticEventArgs(level, message, exception);

            Assert.Equal(level, e.Level);
            Assert.Equal(message, e.Message);
            Assert.Equal(exception, e.Exception);

            Assert.True(e.Timestamp <= DateTime.UtcNow);
        }

        [Trait("Category", "DiagnosticEventArgs Instantiation")]
        [Theory(DisplayName = "DiagnosticEventArgs Instantiates with null Exception given null"), AutoFixture.Xunit2.AutoData]
        public void Instantiates_With_Null_Exception_Given_Null(DiagnosticLevel level, string message)
        {
            var e = new DiagnosticEventArgs(level, message);

            Assert.Equal(level, e.Level);
            Assert.Equal(message, e.Message);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "DiagnosticEventArgs Properties")]
        [Theory(DisplayName = "DiagnosticEventArgs IncludesException returns false given null Exception"), AutoFixture.Xunit2.AutoData]
        public void IncludesException_Returns_False_Given_Null_Exception(DiagnosticLevel level, string message)
        {
            var e = new DiagnosticEventArgs(level, message);

            Assert.False(e.IncludesException);
        }
    }
}
