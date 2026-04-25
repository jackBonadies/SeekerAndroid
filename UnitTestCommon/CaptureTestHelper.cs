#if DEBUG
using NUnit.Framework;
using Seeker.Debug;
using Soulseek;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using static Seeker.Debug.SearchCaptureStore;
using IoDirectory = System.IO.Directory;
using IoFile = System.IO.File;

namespace UnitTestCommon
{
    // Configures SearchCaptureStore for desktop test use.
    //
    // Passphrase: set SEEKER_DEBUG_CAPTURE_KEY env var (same value used for on-device builds).
    //
    // Read path (default): TestData/Captures/ inside the test output directory.
    //   Encrypted files committed there are copied to output by MSBuild and loaded here.
    //
    // Write path: set SEEKER_TEST_CAPTURE_PATH env var to point directly at the source
    //   tree's UnitTestCommon/TestData/Captures/ folder. That way new captures are
    //   saved there and can be committed without a manual copy step.
    //   If unset, saves also go to the default read path (copy back manually to commit).
    internal static class CaptureTestHelper
    {
        private static bool _configured;

        public static void Configure(String testDataPath)
        {
            if (_configured) return;
            _configured = true;

            var passphrase = Environment.GetEnvironmentVariable("SeekerEncryptKey") ?? "";
            SearchCaptureStore.Configure(testDataPath, passphrase);
        }

        public static void SkipIfNotConfigured()
        {
            if (!SearchCaptureStore.IsConfigured)
            {
                Assert.Ignore("SeekerEncryptKey not set — skipping capture-based test.");
            }
        }

        public static CapturePayload TryLoad(string filename)
        {
            return SearchCaptureStore.LoadRaw(filename);
        }

        public static void Save(string query, IReadOnlyCollection<SearchResponse> responses)
        {
            SearchCaptureStore.Save(query, responses);
        }

        public static void AssertVerified(string testName, IEnumerable<string> actual)
        {
            var verifiedDir = Path.Combine(
                Environment.GetEnvironmentVariable("SEEKER_TEST_CAPTURE_PATH")
                    ?? Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Captures"),
                "Verified");
            IoDirectory.CreateDirectory(verifiedDir);
            var path = Path.Combine(verifiedDir, testName + ".enc");

            var actualList = new List<string>(actual);
            byte[] actualJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(actualList));

            var helper = SearchCaptureStore.FileHelper!;
            if (!IoFile.Exists(path))
            {
                helper.WriteToFile(path, actualJson);
                Assert.Fail($"No verified answer for \"{testName}\" — recorded {actualList.Count} entries. " +
                            $"Copy {path} to TestData/Captures/Verified/ in source, commit, then re-run.");
            }

            byte[] storedJson = helper.Decrypt(IoFile.ReadAllBytes(path));
            var expected = JsonSerializer.Deserialize<List<string>>(Encoding.UTF8.GetString(storedJson))!;
            Assert.AreEqual(expected, actualList);
        }
    }
}
#endif
