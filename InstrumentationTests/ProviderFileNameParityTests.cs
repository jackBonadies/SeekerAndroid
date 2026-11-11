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

        // same text, NFC (precomposed) vs NFD (base + combining mark). print identically.
        private static readonly (string label, string nfc, string nfd)[] NormalizationCases =
        {
            ("dia", "Otro día", "Otro día"),
            ("cafe", "Café", "Café"),
        };

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

        // MediaFileCreator.CreateMediaFile on a SAF tree folder.
        [Test]
        public void MediaFilesKeepTheirName()
        {
            Log.Info(Tag, "provider: " + _scratchDir.Authority + " api " + (int)Android.OS.Build.VERSION.SdkInt);
            var parent = AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(Application.Context, _scratchDir);
            var mismatches = new List<string>();
            foreach (string name in MediaFileNames())
            {
                CheckMediaCreate(parent, name, mismatches);

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

        // Create one normalization form, then the other, in the same folder. Logs what the listing holds, what
        //   DocumentFile.FindFile finds for each form, and whether the second create collides into " (1)".
        [Test]
        public void NormalizationFormsInListing()
        {
            Log.Info(Tag, "provider: " + _scratchDir.Authority + " api " + (int)Android.OS.Build.VERSION.SdkInt);
            var resolver = Application.Context.ContentResolver;
            var mismatches = new List<string>();
            int n = 0;
            foreach (var (label, nfc, nfd) in NormalizationCases)
            {
                foreach (bool dir in new[] { true, false })
                {
                    foreach (bool nfcFirst in new[] { true, false })
                    {
                        string suffix = dir ? "" : ".flac";
                        string mime = dir ? DocumentsContract.Document.MimeTypeDir : "audio/flac";
                        string first = (nfcFirst ? nfc : nfd) + suffix;
                        string second = (nfcFirst ? nfd : nfc) + suffix;
                        string caseLabel = label + (dir ? " dir " : " file ") + (nfcFirst ? "nfc->nfd" : "nfd->nfc");

                        var sub = DocumentsContract.CreateDocument(resolver, _scratchDir, DocumentsContract.Document.MimeTypeDir, "norm" + n++);
                        var firstUri = DocumentsContract.CreateDocument(resolver, sub, mime, first);
                        var listed1 = ListChildNames(sub);
                        var subDoc = AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(Application.Context, sub);
                        string findNfc = subDoc.FindFile(nfc + suffix)?.Name;
                        string findNfd = subDoc.FindFile(nfd + suffix)?.Name;
                        var secondUri = DocumentsContract.CreateDocument(resolver, sub, mime, second);
                        string secondName = secondUri == null ? "<null>" : QueryDisplayName(secondUri);
                        var listed2 = ListChildNames(sub);

                        Log.Info(Tag, "  " + caseLabel + ": created '" + EscapeCodePoints(first) + "' (returned '" + EscapeCodePoints(firstUri == null ? "<null>" : QueryDisplayName(firstUri)) + "')");
                        Log.Info(Tag, "      listing: " + string.Join(", ", listed1.Select(s => "'" + EscapeCodePoints(s) + "'")));
                        Log.Info(Tag, "      FindFile(nfc) -> " + (findNfc == null ? "null" : "'" + EscapeCodePoints(findNfc) + "'") + "  FindFile(nfd) -> " + (findNfd == null ? "null" : "'" + EscapeCodePoints(findNfd) + "'"));
                        Log.Info(Tag, "      then created '" + EscapeCodePoints(second) + "' -> '" + EscapeCodePoints(secondName) + "'; listing: " + string.Join(", ", listed2.Select(s => "'" + EscapeCodePoints(s) + "'")));

                        if (listed1.Count != 1 || listed1[0] != first)
                        {
                            mismatches.Add(caseLabel + ": stored '" + string.Join(", ", listed1.Select(EscapeCodePoints)) + "' for requested '" + EscapeCodePoints(first) + "'");
                        }
                    }
                }
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        // MediaFileCreator.CreateMediaFile on a file:// folder (temp dir, legacy storage), i.e. a RawDocumentFile parent.
        [Test]
        public void RawFilesKeepTheirName()
        {
            var root = new Java.IO.File(Application.Context.GetExternalFilesDir(null), "parity_" + Guid.NewGuid().ToString("N"));
            root.Mkdirs();
            try
            {
                var parent = AndroidX.DocumentFile.Provider.DocumentFile.FromFile(root);
                Log.Info(Tag, "raw parent: " + parent.Uri + " api " + (int)Android.OS.Build.VERSION.SdkInt);
                var mismatches = new List<string>();
                foreach (string name in MediaFileNames())
                {
                    CheckMediaCreate(parent, name, mismatches);
                }
                Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
            }
            finally
            {
                System.IO.Directory.Delete(root.AbsolutePath, true);
            }
        }

        // Checks both the returned DocumentFile's name and the parent's listing, then deletes what was created.
        private static void CheckMediaCreate(AndroidX.DocumentFile.Provider.DocumentFile parent, string name, List<string> mismatches)
        {
            string label = name + " [" + (MediaMimeTypes.GetMimeTypeForCreate(name) ?? "null") + "]";
            string actual;
            string listed;
            try
            {
                var created = MediaFileCreator.CreateMediaFile(parent, name);
                actual = created == null ? "<null>" : created.Name;
                listed = string.Join(", ", parent.ListFiles().Select(f => f.Name));
                created?.Delete();
            }
            catch (Exception e)
            {
                actual = "<threw " + e.GetType().Name + ": " + e.Message + ">";
                listed = "?";
            }
            bool ok = actual == name && listed == name;
            Log.Info(Tag, (ok ? "  same " : "  DIFF ") + label + ": returned '" + Escape(actual) + "' listing '" + Escape(listed) + "'");
            if (!ok)
            {
                mismatches.Add(label + ": returned '" + Escape(actual) + "' listing '" + Escape(listed) + "'");
            }
        }

        // java.io on app-internal storage (no casefold) and app-external (emulated / FUSE).
        [Test]
        public void RawNormalizationForms()
        {
            var mismatches = new List<string>();
            foreach (var baseDir in new[] { Application.Context.FilesDir, Application.Context.GetExternalFilesDir(null) })
            {
                var root = new Java.IO.File(baseDir, "parity_" + Guid.NewGuid().ToString("N"));
                root.Mkdirs();
                try
                {
                    foreach (var (label, nfc, nfd) in NormalizationCases)
                    {
                        foreach (bool nfcFirst in new[] { true, false })
                        {
                            string first = nfcFirst ? nfc : nfd;
                            string second = nfcFirst ? nfd : nfc;
                            string caseLabel = label + " " + (nfcFirst ? "nfc->nfd" : "nfd->nfc") + " in " + baseDir.AbsolutePath;
                            var sub = new Java.IO.File(root, label + (nfcFirst ? "_a" : "_b"));
                            sub.Mkdirs();

                            bool made1 = new Java.IO.File(sub, first).Mkdir();
                            var listed1 = sub.List()?.ToList() ?? new List<string>();
                            bool otherExists = new Java.IO.File(sub, second).Exists();
                            bool made2 = new Java.IO.File(sub, second).Mkdir();
                            var listed2 = sub.List()?.ToList() ?? new List<string>();

                            Log.Info(Tag, "  " + caseLabel + ": mkdir '" + EscapeCodePoints(first) + "' " + made1 + "; listing: " + string.Join(", ", listed1.Select(s => "'" + EscapeCodePoints(s) + "'")));
                            Log.Info(Tag, "      exists('" + EscapeCodePoints(second) + "') " + otherExists + "; mkdir it " + made2 + "; listing: " + string.Join(", ", listed2.Select(s => "'" + EscapeCodePoints(s) + "'")));

                            if (listed1.Count != 1 || listed1[0] != first)
                            {
                                mismatches.Add(caseLabel + ": stored '" + string.Join(", ", listed1.Select(EscapeCodePoints)) + "' for requested '" + EscapeCodePoints(first) + "'");
                            }
                        }
                    }
                }
                finally
                {
                    System.IO.Directory.Delete(root.AbsolutePath, true);
                }
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        private static List<string> ListChildNames(Android.Net.Uri dir)
        {
            var names = new List<string>();
            var children = DocumentsContract.BuildChildDocumentsUriUsingTree(dir, DocumentsContract.GetDocumentId(dir));
            using var cursor = Application.Context.ContentResolver.Query(children, new[] { DocumentsContract.Document.ColumnDisplayName }, null, null, null);
            while (cursor != null && cursor.MoveToNext())
            {
                names.Add(cursor.GetString(0));
            }
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static string EscapeCodePoints(string s)
        {
            return string.Concat(s.Select(c => c < 0x20 || c >= 0x7f ? "\\u" + ((int)c).ToString("X4") : c.ToString()));
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
