using Common.Browse;
using MessagePack;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using NUnit.Framework;
using Seeker;
using Soulseek;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

    public class BrowseTreeParseTests
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

            Common.TreeNode<Soulseek.Directory> tree = BrowseUtils.CreateTreeFromFlatList(browseResponse, false, null, null, username, false);
            string result = PrintTreeToString(tree);

            await Verifier.Verify(result).UseParameters(username);
        }

        #if DEBUG
        private void encryptFile(string inputPath, string outputPath)
        {
            var normalBytes = System.IO.File.ReadAllBytes(inputPath);
            var encryptedFileHelper = new Seeker.Debug.EncryptedFileHelper();
            encryptedFileHelper.WriteToFile(outputPath, normalBytes);
        }


        [TestCase("RealUser1")]
        [TestCase("RealUser2")]
        public async Task TestRealUserParsingBrowseResponseTestData(string username)
        {
            string testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BrowseResponse");
            string path = Path.Combine(testDataDir, $"{username}_Encrypted.mpk");

            var encryptedData = System.IO.File.ReadAllBytes(path);
            var encryptedFileHelper = new Seeker.Debug.EncryptedFileHelper();
            var data = encryptedFileHelper.Decrypt(encryptedData);

            var memoryStream = new MemoryStream(data);
            var browseResponse = MessagePackSerializer.Deserialize<BrowseResponse>(memoryStream, SerializationHelper.BrowseResponseOptions);


            Common.TreeNode<Soulseek.Directory> tree = BrowseUtils.CreateTreeFromFlatList(browseResponse, false, null, null, username, false);
            string result = PrintTreeToString(tree);
            string answerPath = Path.Combine(testDataDir, $"{username}_Answer.bin");

            // uncomment to write answer
            //var resultBytes = Encoding.UTF8.GetBytes(result);
            //var resultBytesEncrypted = encryptedFileHelper.Encrypt(resultBytes);
            //System.IO.File.WriteAllBytes(answerPath, resultBytesEncrypted);

            var bytes = System.IO.File.ReadAllBytes(answerPath);
            var answerBytes = encryptedFileHelper.Decrypt(bytes);
            var answerPlainText = Encoding.UTF8.GetString(answerBytes);

            Assert.AreEqual(result, answerPlainText);
        }
        #endif

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
            sb.AppendLine("Files: " + tree.Data.Files.Count.ToString());
            sb.AppendLine("Children: " + tree.Children.Count.ToString());
            if (tree.Data.Files.Count > 0)
            {
                sb.AppendLine(tree.Data.Files.First().Filename);
            }
            foreach (Common.TreeNode<Soulseek.Directory> child in tree.Children)
            {
                PrintTree(child, sb);
            }
        }

        private static string PrintTreeToString(Soulseek.BrowseResponse browseResponse)
        {
            var sb = new StringBuilder();
            foreach (var dir in browseResponse.Directories)
            {
                sb.AppendLine("Folder: " + dir.Name);
                if (dir.Files.Count == 0)
                {
                    sb.AppendLine("  IsEmpty");
                }
                else
                {
                    sb.AppendLine("  File: " + dir.Files.First().Filename);
                }
            }
            sb.AppendLine("Locked:");
            foreach (var dir in browseResponse.LockedDirectories)
            {
                sb.AppendLine("Folder: " + dir.Name);
                if (dir.Files.Count == 0)
                {
                    sb.AppendLine("  IsEmpty");
                }
                else
                {
                    sb.AppendLine("  File: " + dir.Files.First().Filename);
                }
            }
            return sb.ToString();
        }

        //[TestCase("browse_response_test")]
        public async Task TestParsingRealBrowseResponse(string username)
        {
            string testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "BrowseResponse");
            string jsonPath = Path.Combine(testDataDir, $"{username}.json");
            var stream = System.IO.File.OpenRead(@"C:\seeker_newest\UnitTestCommon\TestData\BrowseResponse\username.mpk");
            var browseResponse = MessagePackSerializer.Deserialize<BrowseResponse>(stream, SerializationHelper.BrowseResponseOptions);
            var input = PrintTreeToString(browseResponse);

            Common.TreeNode<Soulseek.Directory> tree = BrowseUtils.CreateTreeFromFlatList(browseResponse, false, null, null, username, false);
            string result = PrintTreeToString(tree);

            await Verifier.Verify(result).UseParameters(username);
        }

        private Soulseek.Directory createDirectory(string dirName)
        {
            return new Soulseek.Directory(dirName, new List<Soulseek.File>() { new Soulseek.File(1, "test", 123, "mp3") });
        }

        [Test]
        public void TestSortCase()
        {
            var list = new List<Soulseek.Directory>() { 
                createDirectory("B"),
                createDirectory("c"),
                createDirectory("a"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual(dirTree.Children.ElementAt(0).Data.Name, "a");
            Assert.AreEqual(dirTree.Children.ElementAt(1).Data.Name, "B");
            Assert.AreEqual(dirTree.Children.ElementAt(2).Data.Name, "c");
        }

        [Test]
        public async Task TestDifferentCaseFolderSort()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("B"),
                createDirectory("c"),
                createDirectory("B\\anotherSubFolderOfB"),
                createDirectory("b\\anothersubofb"),
                createDirectory("B\\subFolderOfB"),
                createDirectory("b\\subofb"),
                createDirectory("a"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            string result = PrintTreeToString(dirTree);
            await Verifier.Verify(result);
        }

        [Test]
        public void TestSingleDirectory()
        {
            var list = new List<Soulseek.Directory>() { createDirectory("Music") };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(0, dirTree.Children.Count);
            Assert.AreEqual(1, dirTree.Data.Files.Count);
        }

        [Test]
        public void TestSimpleParentChild()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(1, dirTree.Children.Count);
            Assert.AreEqual("Music\\Rock", dirTree.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestRootBackslashSkipped()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("\\"),
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(1, dirTree.Children.Count);
            Assert.AreEqual("Music\\Rock", dirTree.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestTrailingBackslashTrimmed()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music\\"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(1, dirTree.Children.Count);
        }

        [Test]
        public void TestDeepNesting()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
                createDirectory("Music\\Rock\\Classic"),
                createDirectory("Music\\Rock\\Classic\\70s"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            var rock = dirTree.Children.ElementAt(0);
            Assert.AreEqual("Music\\Rock", rock.Data.Name);
            var classic = rock.Children.ElementAt(0);
            Assert.AreEqual("Music\\Rock\\Classic", classic.Data.Name);
            Assert.AreEqual("Music\\Rock\\Classic\\70s", classic.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestGoUpMultipleLevels()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
                createDirectory("Music\\Rock\\Classic"),
                createDirectory("Music\\Rock\\Classic\\70s"),
                createDirectory("Music\\Jazz"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(2, dirTree.Children.Count);
            // Jazz sorts before Rock (case-insensitive)
            Assert.AreEqual("Music\\Jazz", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("Music\\Rock", dirTree.Children.ElementAt(1).Data.Name);
            // Rock still has its deep children
            Assert.AreEqual(1, dirTree.Children.ElementAt(1).Children.Count);
        }

        [Test]
        public void TestMultipleSiblings()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Classical"),
                createDirectory("Music\\Jazz"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(3, dirTree.Children.Count);
            Assert.AreEqual("Music\\Classical", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("Music\\Jazz", dirTree.Children.ElementAt(1).Data.Name);
            Assert.AreEqual("Music\\Rock", dirTree.Children.ElementAt(2).Data.Name);
        }

        [Test]
        public void TestMultipleRootsCreatesSyntheticRoot()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Downloads"),
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            // No common parent → synthetic empty root
            Assert.AreEqual("", dirTree.Data.Name);
            Assert.AreEqual(0, dirTree.Data.Files.Count);
            Assert.AreEqual(2, dirTree.Children.Count);
            Assert.AreEqual("Downloads", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("Music", dirTree.Children.ElementAt(1).Data.Name);
        }

        [Test]
        public void TestCommonParentSyntheticRoot()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("shared\\FolderA"),
                createDirectory("shared\\FolderA\\sub"),
                createDirectory("shared\\FolderB"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            // Common parent "shared" becomes synthetic root
            Assert.AreEqual("shared", dirTree.Data.Name);
            Assert.AreEqual(0, dirTree.Data.Files.Count);
            Assert.AreEqual(2, dirTree.Children.Count);
            Assert.AreEqual("shared\\FolderA", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("shared\\FolderB", dirTree.Children.ElementAt(1).Data.Name);
            // FolderA has its sub-child
            Assert.AreEqual(1, dirTree.Children.ElementAt(0).Children.Count);
        }

        [Test]
        public void TestPrefixIsNotParent_MuVsMusic()
        {
            var list = new List<Soulseek.Directory>() {
                createDirectory("Mu"),
                createDirectory("Mu\\songs"),
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            // "Music" is NOT a child of "Mu" → synthetic empty root
            Assert.AreEqual("", dirTree.Data.Name);
            Assert.That(dirTree.Children.Any(c => c.Data.Name == "Mu"));
            Assert.That(dirTree.Children.Any(c => c.Data.Name == "Music"));
            // Each has its own child
            var mu = dirTree.Children.First(c => c.Data.Name == "Mu");
            Assert.AreEqual(1, mu.Children.Count);
            Assert.AreEqual("Mu\\songs", mu.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestLockedDirectoriesIncluded()
        {
            var regular = new List<Soulseek.Directory>() { createDirectory("Music") };
            var locked = new List<Soulseek.Directory>() { createDirectory("Music\\Private") };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(regular, locked), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(1, dirTree.Children.Count);
            Assert.AreEqual("Music\\Private", dirTree.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestLockedDirectoriesExcluded()
        {
            var regular = new List<Soulseek.Directory>() { createDirectory("Music") };
            var locked = new List<Soulseek.Directory>() { createDirectory("Music\\Private") };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(regular, locked), false, null, null, "test", true);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(0, dirTree.Children.Count);
        }

        [Test]
        public void TestBackslashSortsBeforeOtherChars()
        {
            // "a\b" should sort before "aa" because \ gets priority
            var list = new List<Soulseek.Directory>() {
                createDirectory("a"),
                createDirectory("aa"),
                createDirectory("a\\b"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            // "a" and "aa" are separate roots, "a\b" is child of "a"
            Assert.AreEqual("", dirTree.Data.Name);
            Assert.AreEqual(2, dirTree.Children.Count);
            var aNode = dirTree.Children.ElementAt(0);
            Assert.AreEqual("a", aNode.Data.Name);
            Assert.AreEqual(1, aNode.Children.Count);
            Assert.AreEqual("a\\b", aNode.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("aa", dirTree.Children.ElementAt(1).Data.Name);
        }

        [Test]
        public void TestCaseSensitiveParentChild()
        {
            // IsChildDirString uses case-sensitive StartsWith
            // so "music\jazz" is NOT recognized as a child of "Music"
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Rock"),
                createDirectory("music\\jazz"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("", dirTree.Data.Name);
            Assert.AreEqual(2, dirTree.Children.Count);
            Assert.AreEqual("Music", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual(1, dirTree.Children.ElementAt(0).Children.Count);
            // "music\jazz" ends up as sibling under synthetic root, not child of "Music"
            Assert.AreEqual("music\\jazz", dirTree.Children.ElementAt(1).Data.Name);
        }

        [Test]
        public void TestEmptyDirectoryNoFiles()
        {
            var list = new List<Soulseek.Directory>() {
                new Soulseek.Directory("Music"),
                createDirectory("Music\\Rock"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            Assert.AreEqual(0, dirTree.Data.Files.Count);
            Assert.AreEqual(1, dirTree.Children.Count);
            Assert.AreEqual(1, dirTree.Children.ElementAt(0).Data.Files.Count);
        }

        [Test]
        public void TestSkippedIntermediateDirectories()
        {
            // Parent "Music" exists but intermediate "Music\Rock" is missing
            var list = new List<Soulseek.Directory>() {
                createDirectory("Music"),
                createDirectory("Music\\Rock\\Classic"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            Assert.AreEqual("Music", dirTree.Data.Name);
            // "Music\Rock\Classic" should still be a child of "Music" (IsChildDirString checks prefix, not direct parent)
            Assert.AreEqual(1, dirTree.Children.Count);
            Assert.AreEqual("Music\\Rock\\Classic", dirTree.Children.ElementAt(0).Data.Name);
        }

        [Test]
        public void TestSortStabilityUpperBeforeLower()
        {
            // When case-insensitive compare is equal, uppercase sorts before lowercase
            var list = new List<Soulseek.Directory>() {
                createDirectory("music"),
                createDirectory("Music"),
            };
            var dirTree = BrowseUtils.CreateTreeFromFlatList(new BrowseResponse(list), false, null, null, "test", false);
            // "Music" (uppercase M) should sort before "music" (lowercase m)
            Assert.AreEqual("", dirTree.Data.Name);
            Assert.AreEqual(2, dirTree.Children.Count);
            Assert.AreEqual("Music", dirTree.Children.ElementAt(0).Data.Name);
            Assert.AreEqual("music", dirTree.Children.ElementAt(1).Data.Name);
        }
    }
}

