using NUnit.Framework;
using Seeker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace UnitTestCommon
{
    /// <summary>
    /// Simulates an Android content-resolver stream from a pipe-backed provider (e.g. a cloud
    /// drive): not seekable, Length unsupported, and Read returns at most 1 byte per call
    /// (short reads are legal for any Stream and common for Java-backed pipe streams).
    /// </summary>
    internal class PipeLikeStream : Stream
    {
        private readonly Stream inner;
        public PipeLikeStream(Stream inner)
        {
            this.inner = inner;
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, Math.Min(count, 1));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush()
        {
        }
    }

    [TestFixture]
    public class ImportHelperEdgeCaseTests
    {
        private static string NicotinePath(string fileName)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Nicotine", fileName);
        }

        private const string MinimalConfig =
            "[server]\n" +
            "userlist = [['usernameB', 'a note', False, False, False, 'Never seen', ''], ['usernameC', '', False, False, False, '', 'flag_US']]\n" +
            "banlist = ['usernameD']\n" +
            "ignorelist = []\n" +
            "autosearch = ['classical piano']\n";

        private static void AssertMinimalConfigResult(ImportedData result)
        {
            Assert.That(result.UserList, Is.EqualTo(new List<string> { "usernameB", "usernameC" }));
            Assert.That(result.IgnoredBanned, Is.EqualTo(new List<string> { "usernameD" }));
            Assert.That(result.Wishlist, Is.EqualTo(new List<string> { "classical piano" }));
            Assert.That(result.UserNotes.Count, Is.EqualTo(1));
            Assert.That(result.UserNotes[0].Item2, Is.EqualTo("a note"));
        }

        [Test]
        public void EmptyFile_ThrowsCleanError()
        {
            var ex = Assert.Throws<Exception>(() => ImportHelper.ImportFile("backup", new MemoryStream()));
            Assert.That(ex.Message, Does.Contain("empty"));
        }

        [Test]
        public void UnrecognizedTextFile_ThrowsInsteadOfEmptySuccess()
        {
            var bytes = Encoding.UTF8.GetBytes("hello world\nthis is not a config file\n");
            var ex = Assert.Throws<ImportHelper.NicotineParsingException>(
                () => ImportHelper.ImportFile("notes.txt", new MemoryStream(bytes)));
            Assert.That(ex.MessageToToast, Does.Contain("[server]"));
        }

        [Test]
        public void IniFileWithoutServerSection_ImportsAsEmpty()
        {
            //config-shaped (has 'key = value' lines) so it gets the benefit of the doubt,
            //even though there is no [server] section
            var bytes = Encoding.UTF8.GetBytes("[general]\nsomekey = somevalue\n");
            var result = ImportHelper.ImportFile("settings.ini", new MemoryStream(bytes));
            Assert.That(result.UserList, Is.Empty);
            Assert.That(result.IgnoredBanned, Is.Empty);
            Assert.That(result.Wishlist, Is.Empty);
            Assert.That(result.UserNotes, Is.Empty);
        }

        [Test]
        public void UppercaseExtension_StillRecognizedAsBz2()
        {
            using var stream = File.OpenRead(NicotinePath("basic3_7.tar.bz2"));
            var result = ImportHelper.ImportFile("BACKUP.TAR.BZ2", stream);
            Assert.That(result.UserList.Count, Is.EqualTo(2));
            Assert.That(result.Wishlist.Count, Is.EqualTo(1));
        }

        [Test]
        public void PipeLikeStream_PlainConfig_Parses()
        {
            var bytes = Encoding.UTF8.GetBytes(MinimalConfig);
            var result = ImportHelper.ImportFile("config", new PipeLikeStream(new MemoryStream(bytes)));
            AssertMinimalConfigResult(result);
        }

        [Test]
        public void PipeLikeStream_TarBz2_Parses()
        {
            using var fileStream = File.OpenRead(NicotinePath("basic3_7.tar.bz2"));
            var result = ImportHelper.ImportFile("basic3_7.tar.bz2", new PipeLikeStream(fileStream));
            Assert.That(result.UserList.Count, Is.EqualTo(2));
        }

        [Test]
        public void SeekerXml_MissingOptionalLists_Parses()
        {
            //an export from an older version may lack fields added later - XmlSerializer leaves
            //those lists null, not empty
            var data = new SeekerImportExportData() { Userlist = new List<string> { "usernameB" } };
            var ms = new MemoryStream();
            new XmlSerializer(typeof(SeekerImportExportData)).Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            var result = ImportHelper.ImportFile("seeker_export", ms);
            Assert.That(result.UserList, Is.EqualTo(new List<string> { "usernameB" }));
            Assert.That(result.IgnoredBanned, Is.Empty);
            Assert.That(result.Wishlist, Is.Empty);
            Assert.That(result.UserNotes, Is.Empty);
        }

        [Test]
        public void GarbageQtFile_ThrowsCleanErrorNotOutOfMemory()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            writer.Write(5);            //number of tables - passes the 1..10000 leniency check
            writer.Write(999999999);    //"table name length" - would be a ~1GB allocation without validation
            writer.Write(new byte[32]); //filler
            ms.Seek(0, SeekOrigin.Begin);
            var ex = Assert.Throws<Exception>(() => ImportHelper.ImportFile("backup.dat", ms));
            Assert.That(ex.Message, Does.Contain("QT"));
        }
    }
}
