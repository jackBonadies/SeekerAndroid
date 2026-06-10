using NUnit.Framework;
using Seeker;
using System.IO;
using System.Threading.Tasks;
using VerifyNUnit;

namespace UnitTestCommon
{
    [TestFixture]
    public class ImportHelperTests
    {
        // 2021-12-06              — python 3.9 (PAX tar) windows backup, basic userlist only
        // 2026-06-10              — python 3.9 (PAX tar), wishlist/history-heavy, bare .bz2 extension
        // basic3_7                — python 3.7 (old GNU tar, no pax header)
        // emptyTest               — empty userlist/banlist/ignorelist, ipblocklist only
        // pythontarfile_old_2_7   — python 2.7 tarfile, old config format ([columns] has a
        //                           'userlist' key that must NOT be picked up — section check)
        // special_character_usernames — usernames containing @ " ' [ ] { \\ and spaces
        [TestCase("2021-12-06.tar.bz2")]
        [TestCase("2026-06-10.bz2")]
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
    }
}
