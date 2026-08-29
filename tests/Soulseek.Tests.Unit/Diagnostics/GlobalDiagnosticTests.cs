// <copyright file="GlobalDiagnosticTests.cs" company="JP Dillingham">
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
    using Moq;
    using Soulseek.Diagnostics;
    using Xunit;

    public class GlobalDiagnosticTests
    {
        [Trait("Category", "GlobalDiagnostic")]
        [Fact]
        public void Behaves_As_Expected()
        {
            // because this is static and there's no great way to ensure the order
            // in which these tests can run, test *everything* serially in this one test
            // this is shitty, as is the need for GlobalDiagnostic in the first place
            // but it works and the behavior correct, so ¯\_(ツ)_/¯
            GlobalDiagnostic.Init(null);

            var ex = Record.Exception(() =>
            {
                GlobalDiagnostic.Trace("asdf");
                GlobalDiagnostic.Trace("asdf", new Exception("xyz"));
                GlobalDiagnostic.Debug("foo");
                GlobalDiagnostic.Debug("foo", new Exception("bar"));
                GlobalDiagnostic.Info("asdfasdfa");
                GlobalDiagnostic.Warning("warn");
                GlobalDiagnostic.Warning("asdf", new Exception("asdf"));
            });

            Assert.Null(ex);

            var f = new Mock<IDiagnosticFactory>();
            ex = new Exception();

            GlobalDiagnostic.Init(f.Object);

            GlobalDiagnostic.Trace("asdf");
            GlobalDiagnostic.Trace("asdf", ex);
            GlobalDiagnostic.Debug("foo");
            GlobalDiagnostic.Debug("foo", ex);
            GlobalDiagnostic.Info("asdfasdfa");
            GlobalDiagnostic.Warning("warn");
            GlobalDiagnostic.Warning("asdf", ex);

            f.Verify(m => m.Trace("asdf"), Times.Exactly(1));
            f.Verify(m => m.Trace("asdf", ex), Times.Exactly(1));
            f.Verify(m => m.Debug("foo"), Times.Exactly(1));
            f.Verify(m => m.Debug("foo", ex), Times.Exactly(1));
            f.Verify(m => m.Info("asdfasdfa"), Times.Exactly(1));
            f.Verify(m => m.Warning("warn", null), Times.Exactly(1));
            f.Verify(m => m.Warning("asdf", ex), Times.Exactly(1));

            // try to clean up.  probably doesn't matter much
            GlobalDiagnostic.Init(null);
        }
    }
}
