using NUnit.Framework;
using Seeker;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VerifyNUnit;

namespace UnitTestCommon
{
    [TestFixture]
    public class ImportHelperTests
    {
        // 2021-12-06              — python 3.9 (PAX tar) windows backup, basic userlist only
        // 2026-06-10              — python 3.9 (PAX tar), wishlist/history-heavy
        // basic3_7                — python 3.7 (old GNU tar, no pax header)
        // emptyTest               — empty userlist/banlist/ignorelist, ipblocklist only
        // pythontarfile_old_2_7   — python 2.7 tarfile, old config format ([columns] has a
        //                           'userlist' key that must NOT be picked up — section check)
        // special_character_usernames — usernames containing @ " ' [ ] { \\ and spaces
        [TestCase("2021-12-06.tar.bz2")]
        [TestCase("2026-06-10.tar.bz2")]
        [TestCase("basic3_7.tar.bz2")]
        [TestCase("emptyTest.tar.bz2")]
        [TestCase("pythontarfile_old_2_7.tar.bz2")]
        [TestCase("special_character_usernames.tar.bz2")]
        public async Task ImportNicotineConfigBackup(string fileName)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Nicotine", fileName);
            using var fileStream = File.OpenRead(path);
            ImportedData result = ImportHelper.ImportFile(path, fileStream);
            await Verifier.Verify(result).UseTextForParameters(fileName.Split('.')[0]);
        }

        // seeker_data_empty_usernotes — export with an empty <UserNotes /> element
        // seeker_data_with_usernotes  — note containing comma, quotes, and escaped html;
        //                               duplicate userlist entry
        [TestCase("seeker_data_empty_usernotes.xml")]
        [TestCase("seeker_data_with_usernotes.xml")]
        public async Task ImportSeekerConfig(string fileName)
        {
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "SeekerSettings", fileName);
            using var fileStream = File.OpenRead(path);
            ImportedData result = ImportHelper.ImportFile(path, fileStream);
            await Verifier.Verify(result).UseTextForParameters(fileName.Split('.')[0]);
        }


#if DEBUG
        private void encryptFile(string inputPath, string outputPath)
        {
            var normalBytes = System.IO.File.ReadAllBytes(inputPath);
            var encryptedFileHelper = new Seeker.Debug.EncryptedFileHelper();
            encryptedFileHelper.WriteToFile(outputPath, normalBytes);
        }

        private static string PrintImportedData(ImportedData data)
        {
            var sb = new StringBuilder();
            AppendList(sb, "UserList", data.UserList);
            AppendList(sb, "IgnoredBanned", data.IgnoredBanned);
            AppendList(sb, "Wishlist", data.Wishlist);
            sb.AppendLine("UserNotes:");
            if (data.UserNotes != null)
            {
                foreach (var note in data.UserNotes)
                {
                    sb.AppendLine($"  {note.Item1}: {note.Item2}");
                }
            }
            return sb.ToString();
        }

        private static void AppendList(StringBuilder sb, string header, List<string> items)
        {
            sb.AppendLine(header + ":");
            if (items != null)
            {
                foreach (var item in items)
                {
                    sb.AppendLine("  " + item);
                }
            }
        }

        [TestCase("linux_empty_wish")]
        [TestCase("test")]
        [TestCase("test2")]
        public void ImportQTConfig(string name)
        {
            string testDataDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "QT");
            string path = Path.Combine(testDataDir, $"{name}_Encrypted.scd1");

            var encryptedFileHelper = new Seeker.Debug.EncryptedFileHelper();
            var data = encryptedFileHelper.Decrypt(System.IO.File.ReadAllBytes(path));

            using var memoryStream = new MemoryStream(data);
            ImportedData result = ImportHelper.ImportFile(path, memoryStream);
            string resultText = PrintImportedData(result);

            string answerPath = Path.Combine(testDataDir, $"{name}_Answer.bin");

            // uncomment to write answer
            //System.IO.File.WriteAllBytes(answerPath, encryptedFileHelper.Encrypt(Encoding.UTF8.GetBytes(resultText)));

            var answerBytes = encryptedFileHelper.Decrypt(System.IO.File.ReadAllBytes(answerPath));
            string answerPlainText = Encoding.UTF8.GetString(answerBytes);

            Assert.AreEqual(resultText, answerPlainText);
        }

        // One-shot generator: drop the plaintext .scd1 files into UnitTestCommon/TestData/QT
        // (source dir, not bin), run this, then remove the plaintext files again.
        // Writes {name}_Encrypted.scd1 and {name}_Answer.bin next to them.
        [Test]
        [Explicit]
        public void EncryptQTTestData()
        {
            string sourceDir = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory, "..", "..", "..", "TestData", "QT"));
            var encryptedFileHelper = new Seeker.Debug.EncryptedFileHelper();
            foreach (string inputPath in System.IO.Directory.GetFiles(sourceDir, "*.scd1"))
            {
                string name = Path.GetFileNameWithoutExtension(inputPath);
                if (name.EndsWith("_Encrypted"))
                {
                    continue;
                }

                encryptFile(inputPath, Path.Combine(sourceDir, $"{name}_Encrypted.scd1"));

                using var fileStream = File.OpenRead(inputPath);
                ImportedData result = ImportHelper.ImportFile(inputPath, fileStream);
                string resultText = PrintImportedData(result);
                System.IO.File.WriteAllBytes(
                    Path.Combine(sourceDir, $"{name}_Answer.bin"),
                    encryptedFileHelper.Encrypt(Encoding.UTF8.GetBytes(resultText)));
            }
        }
#endif
    }
}
