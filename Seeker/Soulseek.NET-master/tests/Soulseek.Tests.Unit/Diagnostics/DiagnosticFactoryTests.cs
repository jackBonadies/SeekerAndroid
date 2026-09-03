// <copyright file="DiagnosticFactoryTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Soulseek.Diagnostics;
    using Xunit;

    public class DiagnosticFactoryTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(DiagnosticLevel level, Action<DiagnosticEventArgs> handler)
        {
            var d = new DiagnosticFactory(level, handler);

            Assert.Equal(level, d.GetProperty<DiagnosticLevel>("MinimumLevel"));
            Assert.Equal(handler, d.GetProperty<Action<DiagnosticEventArgs>>("EventHandler"));
        }

        [Trait("Category", "Trace")]
        [Theory(DisplayName = "Raises event on trace"), AutoData]
        public void Raises_Event_On_Trace(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Trace, (args) =>
            {
                e = args;
            });

            d.Trace(message);

            Assert.Equal(message, e.Message);
            Assert.Equal(DiagnosticLevel.Trace, e.Level);
            Assert.False(e.IncludesException);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "Trace")]
        [Theory(DisplayName = "Raises event on trace with Exception"), AutoData]
        public void Raises_Event_On_Trace_With_Exception(string message, Exception ex)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Trace, (args) =>
            {
                e = args;
            });

            d.Trace(message, ex);

            Assert.Equal(message, e.Message);
            Assert.Equal(ex, e.Exception);
            Assert.Equal(DiagnosticLevel.Trace, e.Level);
            Assert.True(e.IncludesException);
            Assert.NotNull(e.Exception);
        }

        [Trait("Category", "Debug")]
        [Theory(DisplayName = "Raises event on debug"), AutoData]
        public void Raises_Event_On_Debug(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Debug, (args) =>
            {
                e = args;
            });

            d.Debug(message);

            Assert.Equal(message, e.Message);
            Assert.Equal(DiagnosticLevel.Debug, e.Level);
            Assert.False(e.IncludesException);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "Debug")]
        [Theory(DisplayName = "Raises event on debug with Exception"), AutoData]
        public void Raises_Event_On_Debug_With_Exception(string message, Exception ex)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Debug, (args) =>
            {
                e = args;
            });

            d.Debug(message, ex);

            Assert.Equal(message, e.Message);
            Assert.Equal(ex, e.Exception);
            Assert.Equal(DiagnosticLevel.Debug, e.Level);
            Assert.True(e.IncludesException);
            Assert.NotNull(e.Exception);
        }

        [Trait("Category", "Debug")]
        [Theory(DisplayName = "Does not raise event on debug when level is > Debug"), AutoData]
        public void Does_Not_Raise_Event_On_Debug_When_Level_Is_Gt_Debug(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Info, (args) =>
            {
                e = args;
            });

            d.Debug(message);

            Assert.Null(e);
        }

        [Trait("Category", "Info")]
        [Theory(DisplayName = "Raises event on info"), AutoData]
        public void Raises_Event_On_Info(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Info, (args) =>
            {
                e = args;
            });

            d.Info(message);

            Assert.Equal(message, e.Message);
            Assert.Equal(DiagnosticLevel.Info, e.Level);
            Assert.False(e.IncludesException);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "Info")]
        [Theory(DisplayName = "Does not raise event on info when level is > Info"), AutoData]
        public void Does_Not_Raise_Event_On_Info_When_Level_Is_Gt_Info(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Warning, (args) =>
            {
                e = args;
            });

            d.Info(message);

            Assert.Null(e);
        }

        [Trait("Category", "Warning")]
        [Theory(DisplayName = "Raises event on warning"), AutoData]
        public void Raises_Event_On_Warning(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.Warning, (args) =>
            {
                e = args;
            });

            d.Warning(message);

            Assert.Equal(message, e.Message);
            Assert.Equal(DiagnosticLevel.Warning, e.Level);
            Assert.False(e.IncludesException);
            Assert.Null(e.Exception);
        }

        [Trait("Category", "Warning")]
        [Theory(DisplayName = "Does not raise event on warning when level is > Warning"), AutoData]
        public void Does_Not_Raise_Event_On_Warning_When_Level_Is_Gt_Warning(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.None, (args) =>
            {
                e = args;
            });

            d.Warning(message);

            Assert.Null(e);
        }

        [Trait("Category", "None")]
        [Theory(DisplayName = "Does not raise event when level is None"), AutoData]
        public void Does_Not_Raise_Event_When_Level_Is_None(string message)
        {
            DiagnosticEventArgs e = null;

            var d = new DiagnosticFactory(DiagnosticLevel.None, (args) =>
            {
                e = args;
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);

            Assert.Null(e);
        }

        [Trait("Category", "Filter")]
        [Theory(DisplayName = "Filters properly"), AutoData]
        public void Filters_Properly_Debug(string message)
        {
            List<DiagnosticEventArgs> x = new List<DiagnosticEventArgs>();

            var d = new DiagnosticFactory(DiagnosticLevel.Debug, (args) =>
            {
                x.Add(args);
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);
            d.Trace(message);

            Assert.Equal(3, x.Count);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Debug);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Info);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Warning);
        }

        [Trait("Category", "Filter")]
        [Theory(DisplayName = "Filters properly"), AutoData]
        public void Filters_Properly_Trace(string message)
        {
            List<DiagnosticEventArgs> x = new List<DiagnosticEventArgs>();

            var d = new DiagnosticFactory(DiagnosticLevel.Trace, (args) =>
            {
                x.Add(args);
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);
            d.Trace(message);

            Assert.Equal(4, x.Count);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Trace);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Debug);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Info);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Warning);
        }

        [Trait("Category", "Filter")]
        [Theory(DisplayName = "Filters properly"), AutoData]
        public void Filters_Properly_Info(string message)
        {
            List<DiagnosticEventArgs> x = new List<DiagnosticEventArgs>();

            var d = new DiagnosticFactory(DiagnosticLevel.Info, (args) =>
            {
                x.Add(args);
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);
            d.Trace(message);

            Assert.Equal(2, x.Count);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Info);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Warning);
        }

        [Trait("Category", "Filter")]
        [Theory(DisplayName = "Filters properly"), AutoData]
        public void Filters_Properly_Warning(string message)
        {
            List<DiagnosticEventArgs> x = new List<DiagnosticEventArgs>();

            var d = new DiagnosticFactory(DiagnosticLevel.Warning, (args) =>
            {
                x.Add(args);
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);
            d.Trace(message);

            Assert.Single(x);
            Assert.Single(x, y => y.Level == DiagnosticLevel.Warning);
        }

        [Trait("Category", "Filter")]
        [Theory(DisplayName = "Filters properly"), AutoData]
        public void Filters_Properly_None(string message)
        {
            List<DiagnosticEventArgs> x = new List<DiagnosticEventArgs>();

            var d = new DiagnosticFactory(DiagnosticLevel.None, (args) =>
            {
                x.Add(args);
            });

            d.Debug(message);
            d.Info(message);
            d.Warning(message);
            d.Trace(message);

            Assert.Empty(x);
        }
    }
}
