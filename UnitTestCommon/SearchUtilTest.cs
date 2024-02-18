using NUnit.Framework;
using NUnit.Framework.Constraints;
using SlskHelp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestCommon
{
    public class SearchUtilTest
    {
        [Test]
        public void FilterExcludedSearchPhrasesTest()
        {
            SearchUtil.ExcludedSearchPhrases = new List<string>() { "Homer Simpson", "Bart Simpson", "Lisa" };
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("homers diary"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("homer_simpson diary"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("Bart and Homer"));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(@"homer folder//simpsons family//bart and homer file.txt"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile("homer Simpsons diary"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile("the homer Simpson full"));
            Assert.IsTrue(SearchUtil.ShouldExcludeFile(@"Full//LISAS diary"));

            SearchUtil.ExcludedSearchPhrases = new List<string>();
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(""));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("test"));

            SearchUtil.ExcludedSearchPhrases = null;
            Assert.IsFalse(SearchUtil.ShouldExcludeFile(""));
            Assert.IsFalse(SearchUtil.ShouldExcludeFile("test"));
        }
    }
}
