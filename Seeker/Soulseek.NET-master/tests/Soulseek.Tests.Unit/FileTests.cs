// <copyright file="FileTests.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using AutoFixture.Xunit2;
    using Xunit;

    public class FileTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        public void Instantiates_With_The_Given_Data(int code, string filename, long size, string extension, List<FileAttribute> attributeList)
        {
            var f = default(File);

            var ex = Record.Exception(() => f = new File(code, filename, size, extension, attributeList));

            Assert.Null(ex);

            Assert.Equal(code, f.Code);
            Assert.Equal(filename, f.Filename);
            Assert.Equal(size, f.Size);
            Assert.Equal(extension, f.Extension);
            Assert.Equal(attributeList.Count, f.AttributeCount);
            Assert.Equal(attributeList, f.Attributes);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with empty Attributes given no attributeList"), AutoData]
        public void Instantiates_With_Empty_Attributes_Given_No_AttributeList(int code, string filename, long size, string extension)
        {
            var f = default(File);

            var ex = Record.Exception(() => f = new File(code, filename, size, extension));

            Assert.Null(ex);

            Assert.NotNull(f.Attributes);
            Assert.Empty(f.Attributes);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "BitDepth attribute returns matching value when value"), AutoData]
        public void BitDepth_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, value) };

            var f = new File(code, filename, size, extension, list);

            Assert.Equal(list[0], f.Attributes.ToList()[0]);
            Assert.Equal(value, f.BitDepth);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "BitDepth attribute returns null when no value"), AutoData]
        public void BitDepth_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
        {
            var f = new File(code, filename, size, extension);

            Assert.Empty(f.Attributes);
            Assert.Null(f.BitDepth);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "BitRate attribute returns matching value when value"), AutoData]
        public void BitRate_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, value) };

            var f = new File(code, filename, size, extension, list);

            Assert.Equal(list[0], f.Attributes.ToList()[0]);
            Assert.Equal(value, f.BitRate);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "BitRate attribute returns null when no value"), AutoData]
        public void BitRate_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
        {
            var f = new File(code, filename, size, extension);

            Assert.Empty(f.Attributes);
            Assert.Null(f.BitRate);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "SampleRate attribute returns matching value when value"), AutoData]
        public void SampleRate_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.SampleRate, value) };

            var f = new File(code, filename, size, extension, list);

            Assert.Equal(list[0], f.Attributes.ToList()[0]);
            Assert.Equal(value, f.SampleRate);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "SampleRate attribute returns null when no value"), AutoData]
        public void SampleRate_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
        {
            var f = new File(code, filename, size, extension);

            Assert.Empty(f.Attributes);
            Assert.Null(f.SampleRate);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "Length attribute returns matching value when value"), AutoData]
        public void Length_Attribute_Returns_Matching_Value_When_Value(int code, string filename, long size, string extension, int value)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.Length, value) };

            var f = new File(code, filename, size, extension, list);

            Assert.Equal(list[0], f.Attributes.ToList()[0]);
            Assert.Equal(value, f.Length);
        }

        [Trait("Category", "Attributes")]
        [Theory(DisplayName = "Length attribute returns null when no value"), AutoData]
        public void Length_Attribute_Returns_Null_When_No_Value(int code, string filename, long size, string extension)
        {
            var f = new File(code, filename, size, extension);

            Assert.Empty(f.Attributes);
            Assert.Null(f.Length);
        }

        [Trait("Category", "IsVariableBitRate")]
        [Theory(DisplayName = "IsVariableBitRate returns true when attribute is 1"), AutoData]
        public void IsVariableBitRate_Returns_True_When_Attribute_Is_1(int code, string filename, long size, string extension)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.VariableBitRate, 1) };

            var f = new File(code, filename, size, extension, list);

            Assert.True(f.IsVariableBitRate);
        }

        [Trait("Category", "IsVariableBitRate")]
        [Theory(DisplayName = "IsVariableBitRate returns false when attribute is 0"), AutoData]
        public void IsVariableBitRate_Returns_False_When_Attribute_Is_0(int code, string filename, long size, string extension)
        {
            var list = new List<FileAttribute>() { new FileAttribute(FileAttributeType.VariableBitRate, 0) };

            var f = new File(code, filename, size, extension, list);

            Assert.False(f.IsVariableBitRate);
        }

        [Trait("Category", "IsVariableBitRate")]
        [Theory(DisplayName = "IsVariableBitRate returns null when attribute is not present"), AutoData]
        public void IsVariableBitRate_Returns_Null_When_Attribute_Is_Not_Present(int code, string filename, long size, string extension)
        {
            var list = new List<FileAttribute>() { };

            var f = new File(code, filename, size, extension, list);

            Assert.Null(f.IsVariableBitRate);
        }
    }
}
