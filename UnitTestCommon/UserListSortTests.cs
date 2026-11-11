using NUnit.Framework;
using Seeker;
using System.Collections.Generic;
using System.Linq;
using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;

namespace UnitTestCommon
{
    [TestFixture]
    public class UserListSortTests
    {
        private static List<string> SortNames(bool descending, params string[] names)
        {
            var items = names.Select(n => new UserListItem(n)).ToList();
            items.Sort(new UserListAlphabeticalComparer(descending));
            return items.Select(i => i.Username).ToList();
        }

        [Test]
        public void Alphabetical_AscendingByDefault()
        {
            CollectionAssert.AreEqual(new[] { "alice", "bob", "carol" }, SortNames(false, "bob", "carol", "alice"));
        }

        [Test]
        public void Alphabetical_Descending()
        {
            CollectionAssert.AreEqual(new[] { "carol", "bob", "alice" }, SortNames(true, "bob", "carol", "alice"));
        }
    }
}
