using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Provider;
using Android.Util;
using Common;
using Seeker;
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
                CheckOne(label + " (dir)", name, DocumentsContract.Document.MimeTypeDir, name, mismatches);
                CheckOne(label + " (file)", name + ".mp3", "audio/mpeg", name + ".mp3", mismatches);
            }
            Assert.That(mismatches, Is.Empty, string.Join("\n", mismatches));
        }

        private void CheckOne(string label, string requested, string mimeType, string original, List<string> mismatches)
        {
            var resolver = Application.Context.ContentResolver;
            string expected = SimpleHelpers.ToValidFileName(original);
            string actual;
            Android.Net.Uri created = null;
            try
            {
                created = DocumentsContract.CreateDocument(resolver, _scratchDir, mimeType, requested);
                actual = created == null ? "<null>" : QueryDisplayName(created);
            }
            catch (Exception e)
            {
                actual = "<threw " + e.GetType().Name + ": " + e.Message + ">";
            }
            finally
            {
                if (created != null)
                {
                    DocumentsContract.DeleteDocument(resolver, created);
                }
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
