using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Provider;
using Android.Util;
using Common;
using Seeker;
using Seeker.Helpers;
using NUnit.Framework;

namespace InstrumentationTests
{
    // Checks SimpleHelpers.ToValidFileName against what the real storage provider stores.
    [TestFixture]
    public class ProviderFileNameParityTests
    {
        const string Tag = "SeekerTests";

        private Android.Net.Uri _scratchDir;

        private static IEnumerable<(string label, string name)> Cases()
        {
            foreach (char c in "\"*/:<>?\\|")
            {
                yield return ("char " + c, "a" + c + "b");
            }
            yield return ("tab", "a\tb");
            yield return ("DEL", "a\u007fb");
            yield return (":3", ":3");
            yield return ("only invalid", ":");
            yield return ("full test", "fulltest|\\?*<\":>+[]/'~!@#$%^&*()_+/\\.,");
            yield return (".", ".");
            yield return ("..", "..");
            yield return ("...", "...");
            yield return ("empty", "");
            yield return ("ampersand quote", "Rock & Roll's");
            yield return ("japanese", "東京事変 - 教育");
            yield return ("ascii 300 bytes", new string('a', 150) + new string('b', 150));
            yield return ("cjk 300 bytes", new string('東', 50) + new string('京', 50));
            yield return ("ascii 254 + 3-byte char", new string('a', 254) + "東");
            yield return ("ascii 255", new string('a', 255));
            yield return ("ascii 256", new string('a', 256));
            // trimming deletes single UTF-16 chars from the middle, so these split surrogate pairs
            yield return ("emoji 300 bytes", string.Concat(Enumerable.Repeat("😀", 75)));
            yield return ("ascii + emoji middle", new string('a', 140) + string.Concat(Enumerable.Repeat("😀", 5)) + new string('b', 140));
            yield return ("ascii + emoji middle odd", new string('a', 141) + string.Concat(Enumerable.Repeat("😀", 5)) + new string('b', 140));
            yield return ("mixed emoji cjk", string.Concat(Enumerable.Repeat("東😀", 50)));
            // AOSP and a .NET byte count disagree here (a lone surrogate is 1 byte in Java, 3 in .NET)
            yield return ("emoji count divergence", new string('a', 101) + string.Concat(Enumerable.Repeat("😀", 9)) + new string('b', 119));
        }

        private static IEnumerable<string> MediaFileNames()
        {
            foreach (string ext in new[] { "mp3", "flac", "ogg", "oga", "opus", "m4a", "aac", "ape", "wav", "aiff", "aif", "wma", "wv", "alac", "mp4", "mka", "dsf", "weirdext" })
            {
                yield return "01 Track." + ext;
            }
            yield return "cover.jpg";
            yield return "folder.jpeg";
            yield return "cover.png";
            yield return "album.cue";
            yield return "rip.log";
            yield return "info.nfo";
            yield return "playlist.m3u";
            yield return "readme.txt";
            yield return "01 Track.FLAC";
            yield return "01 Track.Mp3";
            yield return "a.b.flac";
            yield return "noextension";
            yield return "trailing.";
        }

        [SetUp]
        public void SetUp()
        {
            var resolver = Application.Context.ContentResolver;
            var tree = resolver.PersistedUriPermissions.FirstOrDefault(p => p.IsWritePermission)?.Uri;
            if (tree == null)
            {
                throw new InvalidOperationException("no persisted tree grant: launch the Seeker Tests app once and pick a folder");
            }
            var root = DocumentsContract.BuildDocumentUriUsingTree(tree, DocumentsContract.GetTreeDocumentId(tree));
            _scratchDir = DocumentsContract.CreateDocument(resolver, root, DocumentsContract.Document.MimeTypeDir, "parity_" + Guid.NewGuid().ToString("N"));
            Assert.That(_scratchDir, Is.Not.Null, "could not create scratch dir under " + tree);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scratchDir != null)
            {
                DocumentsContract.DeleteDocument(Application.Context.ContentResolver, _scratchDir);
            }
        }

        [Test]
        public void ProviderStoresToValidFileName()
        {
            Log.Info(Tag, "provider: " + _scratchDir.Authority + " api " + (int)Android.OS.Build.VERSION.SdkInt);
            var mismatches = new List<string>();
            foreach (var (label, name) in Cases())
            {
                CheckOne(label + " (dir)", name, DocumentsContract.Document.MimeTypeDir, name, mismatches, providerMayFail: true);
                CheckOne(label + " (file)", name + ".mp3", "audio/mpeg", name + ".mp3", mismatches, providerMayFail: true);
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        [Test]
        public void SanitizedNamesRoundTrip()
        {
            Log.Info(Tag, "provider: " + _scratchDir.Authority + " api " + (int)Android.OS.Build.VERSION.SdkInt);
            var mismatches = new List<string>();
            foreach (var (label, name) in Cases())
            {
                string dirName = SimpleHelpers.ToValidFileName(name);
                string fileName = SimpleHelpers.ToValidFileName(name + ".mp3");
                CheckOne(label + " (dir, sanitized)", dirName, DocumentsContract.Document.MimeTypeDir, dirName, mismatches, providerMayFail: false);
                CheckOne(label + " (file, sanitized)", fileName, "audio/mpeg", fileName, mismatches, providerMayFail: false);
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        // CommonHelpers.CreateMediaFile now passes the full name with MediaMimeTypes.GetMimeTypeForCreate.
        [Test]
        public void MediaFilesKeepTheirName()
        {
            Log.Info(Tag, "provider: " + _scratchDir.Authority + " api " + (int)Android.OS.Build.VERSION.SdkInt);
            var mismatches = new List<string>();
            foreach (string name in MediaFileNames())
            {
                string mime = MediaMimeTypes.GetMimeTypeForCreate(name);
                CheckOne(name + " [" + (mime ?? "null") + "]", name, mime, name, mismatches, providerMayFail: false);

                // PREVIOUS BEHAVIOR: since we stripped the extension, Android would take the liberty or renaming 
                //   track.ogg -> track.oga
                //   track.opus -> track.oga
                //   noexttrack -> noexttrack.mp3
                string legacyMime = MediaMimeTypes.GetMimeTypeFromFilename(name);
                bool legacyFullName = legacyMime == null || legacyMime == MediaMimeTypes.M4A_MIME || legacyMime == MediaMimeTypes.APE_MIME;
                string legacyRequested = legacyFullName ? name : System.IO.Path.GetFileNameWithoutExtension(name);
                Log.Info(Tag, "      legacy would store '" + CreateAndReadName(legacyRequested, legacyMime) + "'");
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        private string CreateAndReadName(string requested, string mimeType)
        {
            var resolver = Application.Context.ContentResolver;
            Android.Net.Uri created = null;
            try
            {
                created = DocumentsContract.CreateDocument(resolver, _scratchDir, mimeType, requested);
                return created == null ? "<null>" : QueryDisplayName(created);
            }
            catch (Exception e)
            {
                Log.Warn(Tag, "      create threw for '" + requested + "': " + e);
                return "<threw" + e.GetType().Name + ": " + e.Message + ">";
            }
            finally
            {
                if (created != null)
                {
                    DocumentsContract.DeleteDocument(resolver, created);
                }
            }
        }

        private void CheckOne(string label, string requested, string mimeType, string original, List<string> mismatches, bool providerMayFail)
        {
            string expected = SimpleHelpers.ToValidFileName(original);
            string actual = CreateAndReadName(requested, mimeType);
            if (providerMayFail && actual.StartsWith("<threw"))
            {
                // android cannot create this name itself (i.e. its trim left half a surrogate pair), nothing to match
                Log.Info(Tag, "  skip " + label + ": provider cannot create it, ours '" + Escape(expected) + "'");
                return;
            }

            bool ok = actual == expected;
            Log.Info(Tag, (ok ? "  same " : "  DIFF ") + label + ": expected '" + Escape(expected) + "' actual '" + Escape(actual) + "'");
            if (!ok)
            {
                mismatches.Add(label + ": expected '" + Escape(expected) + "' actual '" + Escape(actual) + "'");
            }
        }

        private static string QueryDisplayName(Android.Net.Uri uri)
        {
            using var cursor = Application.Context.ContentResolver.Query(uri, new[] { DocumentsContract.Document.ColumnDisplayName }, null, null, null);
            if (cursor == null || !cursor.MoveToFirst())
            {
                return "<no row>";
            }
            return cursor.GetString(0);
        }

        private static string Escape(string s)
        {
            if (s.Length > 40)
            {
                int bytes = System.Text.Encoding.UTF8.GetByteCount(s);
                s = s.Substring(0, 18) + "~" + s.Substring(s.Length - 18) + " [" + bytes + "B]";
            }
            return string.Concat(s.Select(c => c < 0x20 || c == 0x7f ? "\\x" + ((int)c).ToString("x2") : c.ToString()));
        }
    }
}
