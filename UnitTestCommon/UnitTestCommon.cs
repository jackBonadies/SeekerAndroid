using NUnit.Framework;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Soulseek;
using VerifyNUnit;

namespace UnitTestCommon
{
    public class BrowseResponseTestData
    {
        public List<DirectoryTestData> Directories { get; set; }
        public List<DirectoryTestData> LockedDirectories { get; set; }
    }

    public class DirectoryTestData
    {
        public string Name { get; set; }
        public int FileCount { get; set; }
        public string FirstFilename { get; set; }
    }

    public class Tests
    {
        [TestCase("username1")]
        [TestCase("username2")]
        [TestCase("username3")]
        [TestCase("username4")]
        [TestCase("username5")]
        [TestCase("username6")]
        [TestCase("username7")]
        public async Task TestParsingBrowseResponseTestData(string username)
        {
            string testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BrowseResponse");
            string jsonPath = Path.Combine(testDataDir, $"{username}.json");
            string json = System.IO.File.ReadAllText(jsonPath);
            var testData = JsonSerializer.Deserialize<BrowseResponseTestData>(json);

            var directories = testData.Directories.Select(ToDirectory).ToList();
            var lockedDirectories = testData.LockedDirectories.Select(ToDirectory).ToList();
            var browseResponse = new BrowseResponse(directories, lockedDirectories);

            Common.TreeNode<Soulseek.Directory> tree = Common.Algorithms.CreateTreeCore(browseResponse, false, null, null, username, false);
            string result = PrintTreeToString(tree);

            await Verifier.Verify(result).UseParameters(username);
        }

        private static Soulseek.Directory ToDirectory(DirectoryTestData d)
        {
            var files = new List<Soulseek.File>();
            for (int i = 0; i < d.FileCount; i++)
            {
                string filename = (i == 0 && d.FirstFilename != null)
                    ? d.FirstFilename
                    : $"file_{i:D3}.mp3";
                files.Add(new Soulseek.File(1, filename, 1000L, "mp3"));
            }
            return new Soulseek.Directory(d.Name, files);
        }

        private static string PrintTreeToString(Common.TreeNode<Soulseek.Directory> tree)
        {
            var sb = new StringBuilder();
            PrintTree(tree, sb);
            return sb.ToString();
        }

        private static void PrintTree(Common.TreeNode<Soulseek.Directory> tree, StringBuilder sb)
        {
            sb.AppendLine(tree.Data.Name);
            sb.AppendLine(tree.Data.Files.Count.ToString());
            if (tree.Data.Files.Count > 0)
            {
                sb.AppendLine(tree.Data.Files.First().Filename);
            }
            foreach (Common.TreeNode<Soulseek.Directory> child in tree.Children)
            {
                PrintTree(child, sb);
            }
        }
    }
}
