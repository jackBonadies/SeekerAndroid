using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;

namespace UnitTestCommon
{
    public static class TarBz2Utility
    {
        /// <summary>
        /// Extracts every regular file inside <paramref name="tarBz2Path"/> to text files
        /// in <paramref name="outputDirectory"/>. Returns the paths written.
        /// </summary>
        public static List<string> ExtractInnerFilesToText(string tarBz2Path, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var writtenFiles = new List<string>();
            string baseName = Path.GetFileName(tarBz2Path).Replace(".tar.bz2", string.Empty);

            using var fileStream = File.OpenRead(tarBz2Path);
            using var bzip2Stream = new Bzip2.BZip2InputStream(fileStream, false);
            using var tarStream = new MemoryStream();
            bzip2Stream.CopyTo(tarStream);
            tarStream.Seek(0, SeekOrigin.Begin);

            using var tarReader = new TarReader(tarStream);
            TarEntry entry;
            while ((entry = tarReader.GetNextEntry()) != null)
            {
                if (entry.EntryType != TarEntryType.RegularFile && entry.EntryType != TarEntryType.V7RegularFile)
                {
                    continue;
                }
                string flattenedEntryName = entry.Name.Replace('/', '_').Replace('\\', '_').TrimStart('.', '_');
                string outputPath = Path.Combine(outputDirectory, $"{baseName}__{flattenedEntryName}.txt");
                using (var outputFile = File.Create(outputPath))
                {
                    entry.DataStream?.CopyTo(outputFile);
                }
                writtenFiles.Add(outputPath);
            }
            return writtenFiles;
        }
    }

    [TestFixture]
    public class TarBz2ExtractionUtility
    {
        [Test, Explicit("Utility: extracts all Nicotine tar.bz2 test data to text files for inspection")]
        public void ExtractAllNicotineTestData()
        {
            string sourceProjectDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
            string nicotineDir = Path.Combine(sourceProjectDir, "TestData", "Nicotine");
            Assert.That(Directory.Exists(nicotineDir), Is.True, $"Could not locate source TestData dir at {nicotineDir}");

            string outputDir = Path.Combine(nicotineDir, "extracted");
            foreach (string archive in Directory.GetFiles(nicotineDir, "*.tar.bz2"))
            {
                var written = TarBz2Utility.ExtractInnerFilesToText(archive, outputDir);
                TestContext.Out.WriteLine($"{Path.GetFileName(archive)}:");
                foreach (string path in written)
                {
                    TestContext.Out.WriteLine($"    -> {path}");
                }
            }
        }
    }
}
