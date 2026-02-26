using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using NUnit.Framework;
using Common;

namespace InstrumentationTests
{
    /// <summary>
    /// Tests that ContentResolver.OpenOutputStream works correctly
    /// when wrapped with PositionTrackingOutputStream — the same pattern
    /// used by DownloadService.OpenIncompleteStream for SAF downloads.
    /// </summary>
    [TestFixture]
    public class OpenOutputStreamTests
    {
        private Android.Net.Uri _testFileUri;
        private Java.IO.File _testJavaFile;

        [SetUp]
        public void SetUp()
        {
            var cachePath = Application.Context.CacheDir;
            var fileName = $"test_{System.Guid.NewGuid():N}.tmp";
            _testJavaFile = new Java.IO.File(cachePath, fileName);
            _testJavaFile.CreateNewFile();
            _testFileUri = Android.Net.Uri.FromFile(_testJavaFile);
        }

        [TearDown]
        public void TearDown()
        {
            _testJavaFile?.Delete();
        }

        [Test]
        public void OpenOutputStream_WritesDataCorrectly()
        {
            var contentResolver = Application.Context.ContentResolver;
            using var rawStream = contentResolver.OpenOutputStream(_testFileUri);
            using var stream = new PositionTrackingOutputStream(rawStream);

            var data = new byte[] { 1, 2, 3, 4, 5 };
            stream.Write(data, 0, data.Length);
            stream.Flush();
            stream.Close();

            using var input = contentResolver.OpenInputStream(_testFileUri);
            var buffer = new byte[16];
            int bytesRead = input.Read(buffer, 0, buffer.Length);

            Assert.That(bytesRead, Is.EqualTo(5));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[4], Is.EqualTo(5));
        }

        [Test]
        public void OpenOutputStream_PositionTracksWrites()
        {
            var contentResolver = Application.Context.ContentResolver;
            using var rawStream = contentResolver.OpenOutputStream(_testFileUri);
            using var stream = new PositionTrackingOutputStream(rawStream);

            Assert.That(stream.Position, Is.EqualTo(0));

            stream.Write(new byte[100], 0, 100);
            Assert.That(stream.Position, Is.EqualTo(100));

            stream.Write(new byte[50], 0, 50);
            Assert.That(stream.Position, Is.EqualTo(150));
        }

        [Test]
        public async Task OpenOutputStream_WriteAsyncTracksPosition()
        {
            var contentResolver = Application.Context.ContentResolver;
            using var rawStream = contentResolver.OpenOutputStream(_testFileUri);
            using var stream = new PositionTrackingOutputStream(rawStream);

            await stream.WriteAsync(new byte[200], 0, 200, CancellationToken.None);
            Assert.That(stream.Position, Is.EqualTo(200));

            await stream.WriteAsync(new byte[300], 0, 300, CancellationToken.None);
            Assert.That(stream.Position, Is.EqualTo(500));
        }

        [Test]
        public void OpenOutputStream_AppendMode_WritesToEnd()
        {
            var contentResolver = Application.Context.ContentResolver;

            using (var rawStream = contentResolver.OpenOutputStream(_testFileUri))
            {
                rawStream.Write(new byte[] { 1, 2, 3 }, 0, 3);
            }

            using (var rawStream = contentResolver.OpenOutputStream(_testFileUri, "wa"))
            using (var stream = new PositionTrackingOutputStream(rawStream))
            {
                Assert.That(stream.Position, Is.EqualTo(0), "PositionTrackingOutputStream starts at 0 (doesn't know about prior data)");

                stream.Write(new byte[] { 4, 5 }, 0, 2);
                Assert.That(stream.Position, Is.EqualTo(2));
            }

            using var input = contentResolver.OpenInputStream(_testFileUri);
            var buffer = new byte[16];
            int bytesRead = input.Read(buffer, 0, buffer.Length);

            Assert.That(bytesRead, Is.EqualTo(5));
            Assert.That(buffer[0], Is.EqualTo(1));
            Assert.That(buffer[4], Is.EqualTo(5));
        }

        [Test]
        public void OpenOutputStream_LargeWrite_TracksCorrectly()
        {
            var contentResolver = Application.Context.ContentResolver;
            using var rawStream = contentResolver.OpenOutputStream(_testFileUri);
            using var stream = new PositionTrackingOutputStream(rawStream);

            int totalWritten = 0;
            var chunk = new byte[4096];
            for (int i = 0; i < 100; i++)
            {
                stream.Write(chunk, 0, chunk.Length);
                totalWritten += chunk.Length;
                Assert.That(stream.Position, Is.EqualTo(totalWritten));
            }

            Assert.That(stream.Position, Is.EqualTo(4096 * 100));
        }
    }
}
