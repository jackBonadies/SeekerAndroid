using NUnit.Framework;
using Soulseek;
using Seeker;
using Common.Search;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestCommon
{
    public class SearchSelectionTests
    {
        private static SearchResponse MakeResponse(
            string username,
            string fileName = "song.mp3",
            List<File> files = null,
            List<File> lockedFiles = null)
        {
            return new SearchResponse(
                username,
                token: 1,
                hasFreeUploadSlot: true,
                uploadSpeed: 100,
                queueLength: 0,
                fileList: files ?? new List<File>
                {
                    new File(100, $@"@{username}\Music\{fileName}", 1000, "mp3")
                },
                lockedFileList: lockedFiles ?? new List<File>());
        }

        [Test]
        public void Identity_DistinguishesPeerAndRepresentativeFile()
        {
            var a = MakeResponse("alice", "a.mp3");
            var b = MakeResponse("alice", "b.mp3");
            var c = MakeResponse("bob", "a.mp3");

            Assert.AreNotEqual(SearchResultIdentity.Get(a), SearchResultIdentity.Get(b));
            Assert.AreNotEqual(SearchResultIdentity.Get(a), SearchResultIdentity.Get(c));
            Assert.AreEqual(SearchResultIdentity.Get(a), SearchResultIdentity.Get(MakeResponse("alice", "a.mp3")));
        }

        [Test]
        public void Identity_FallsBackToLockedFile()
        {
            var locked = MakeResponse("alice",
                files: new List<File>(),
                lockedFiles: new List<File> { new File(100, @"@alice\Music\locked.mp3", 1000, "mp3") });

            Assert.That(SearchResultIdentity.Get(locked), Does.Contain("locked.mp3"));
        }

        [Test]
        public void Toggle_ReturnsNewStateAndIsStableAcrossReorder()
        {
            var selection = new SearchSelection();
            var a = MakeResponse("alice", "a.mp3");
            var b = MakeResponse("bob", "b.mp3");
            var c = MakeResponse("carol", "c.mp3");

            Assert.IsTrue(selection.Toggle(a));
            Assert.IsTrue(selection.IsSelected(a));
            Assert.AreEqual(1, selection.Count);

            // Sorting/filtering changes positions but must not change which peers are selected.
            var reordered = new List<SearchResponse> { c, b, a };
            Assert.IsTrue(selection.IsSelected(a));
            Assert.IsFalse(selection.IsSelected(reordered[0]));
            Assert.AreEqual(1, selection.Resolve(reordered).Count);
            Assert.AreSame(a, selection.Resolve(reordered)[0]);

            Assert.IsFalse(selection.Toggle(a));
            Assert.IsTrue(selection.IsEmpty);
        }

        [Test]
        public void SelectAll_SelectsOnlyVisibleAndResolveKeepsVisibleOrder()
        {
            var selection = new SearchSelection();
            var a = MakeResponse("alice", "a.mp3");
            var b = MakeResponse("bob", "b.mp3");
            var c = MakeResponse("carol", "c.mp3");

            selection.SelectAll(new[] { a, b, c });

            // Hide bob; the selection still remembers him but must not leak into a batch.
            var visible = new List<SearchResponse> { c, a };
            var resolved = selection.Resolve(visible);
            Assert.AreEqual(2, resolved.Count);
            Assert.AreSame(c, resolved[0]);
            Assert.AreSame(a, resolved[1]);
        }

        [Test]
        public void PruneTo_DropsIdentitiesThatAreNoLongerVisible()
        {
            var selection = new SearchSelection();
            var a = MakeResponse("alice", "a.mp3");
            var b = MakeResponse("bob", "b.mp3");

            selection.Select(a);
            selection.Select(b);
            selection.PruneTo(new[] { a });

            Assert.AreEqual(1, selection.Count);
            Assert.IsTrue(selection.IsSelected(a));
            Assert.IsFalse(selection.IsSelected(b));
        }

        [Test]
        public void Clear_RemovesEverySelection()
        {
            var selection = new SearchSelection();
            selection.Select(MakeResponse("alice"));
            selection.Select(MakeResponse("bob"));

            selection.Clear();

            Assert.IsTrue(selection.IsEmpty);
            Assert.AreEqual(0, selection.Count);
        }

        [Test]
        public void Resolve_MissingOrNullInputDoesNotThrow()
        {
            var selection = new SearchSelection();
            selection.Select(MakeResponse("alice"));

            Assert.AreEqual(0, selection.Resolve(null!).Count);
            selection.PruneTo(null!);
            Assert.IsTrue(selection.IsEmpty);
        }

        [Test]
        public void SelectAll_10000Results_ResolvesWithoutPositions()
        {
            var selection = new SearchSelection();
            var responses = new List<SearchResponse>(10000);
            for (int i = 0; i < 10000; i++)
            {
                responses.Add(MakeResponse("user" + i, $"track{i:D5}.mp3"));
            }

            selection.SelectAll(responses);
            var reversed = responses.AsEnumerable().Reverse().ToList();

            Assert.AreEqual(10000, selection.Count);
            var resolved = selection.Resolve(reversed);
            Assert.AreEqual(10000, resolved.Count);
            Assert.AreSame(reversed[0], resolved[0]);
            Assert.AreSame(reversed[9999], resolved[9999]);
        }
    }
}
