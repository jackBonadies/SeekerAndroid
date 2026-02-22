using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace UnitTestCommon
{
    public class StringResourceTests
    {
        private static readonly string ResourcesDir = Path.GetFullPath(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Seeker", "Resources"));

        private static readonly string DefaultStringsPath = Path.Combine(ResourcesDir, "values", "strings.xml");

        private Dictionary<string, string> _defaultStrings;
        private List<(string dir, Dictionary<string, string> strings)> _translations;

        [OneTimeSetUp]
        public void Setup()
        {
            if (!File.Exists(DefaultStringsPath))
            {
                Assert.Ignore($"strings.xml not found at {DefaultStringsPath}. Skipping string resource tests.");
            }

            _defaultStrings = ParseStrings(DefaultStringsPath);

            _translations = Directory.GetDirectories(ResourcesDir, "values-*")
                .Where(d => !d.EndsWith("values-night"))
                .Select(d => Path.Combine(d, "strings.xml"))
                .Where(File.Exists)
                .Select(f => (dir: Path.GetFileName(Path.GetDirectoryName(f)), strings: ParseStrings(f)))
                .ToList();
        }

        private static Dictionary<string, string> ParseStrings(string path)
        {
            var doc = XDocument.Load(path);
            return doc.Root.Elements("string")
                .ToDictionary(
                    e => e.Attribute("name").Value,
                    e => e.Value);
        }

        private static int CountPlaceholders(string value)
        {
            // Match {0}, {1}, {2}, etc.
            return Regex.Matches(value, @"\{\d+\}").Count;
        }

        private static HashSet<string> GetPlaceholderSet(string value)
        {
            return Regex.Matches(value, @"\{\d+\}")
                .Select(m => m.Value)
                .ToHashSet();
        }

        private static int CountBackslashN(string value)
        {
            return Regex.Matches(value, @"\\n").Count;
        }

        [Test]
        public void DefaultStringsFile_Exists_And_Has_Strings()
        {
            Assert.IsTrue(_defaultStrings.Count > 0, "Default strings.xml should have entries");
            TestContext.WriteLine($"Default strings.xml has {_defaultStrings.Count} strings");
        }

        [Test]
        public void AllTranslations_Have_Same_String_Names()
        {
            var errors = new List<string>();

            foreach (var (dir, strings) in _translations)
            {
                var missing = _defaultStrings.Keys.Except(strings.Keys).ToList();
                var extra = strings.Keys.Except(_defaultStrings.Keys).ToList();

                foreach (var m in missing)
                {
                    errors.Add($"{dir}: missing string '{m}'");
                }
                foreach (var e in extra)
                {
                    errors.Add($"{dir}: extra string '{e}' not in default");
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} name mismatches:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void AllTranslations_Have_Matching_Placeholder_Counts()
        {
            var errors = new List<string>();

            foreach (var (dir, strings) in _translations)
            {
                foreach (var (name, enValue) in _defaultStrings)
                {
                    if (!strings.TryGetValue(name, out var transValue)) continue;

                    var enPlaceholders = GetPlaceholderSet(enValue);
                    var transPlaceholders = GetPlaceholderSet(transValue);

                    if (!enPlaceholders.SetEquals(transPlaceholders))
                    {
                        errors.Add($"{dir}/{name}: EN has {{{string.Join(",", enPlaceholders.OrderBy(p => p))}}}, " +
                                   $"translation has {{{string.Join(",", transPlaceholders.OrderBy(p => p))}}}");
                    }
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} placeholder mismatches:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void AllTranslations_Have_Matching_BackslashN_Counts()
        {
            var errors = new List<string>();

            foreach (var (dir, strings) in _translations)
            {
                foreach (var (name, enValue) in _defaultStrings)
                {
                    if (!strings.TryGetValue(name, out var transValue)) continue;

                    var enCount = CountBackslashN(enValue);
                    var transCount = CountBackslashN(transValue);

                    if (enCount != transCount)
                    {
                        errors.Add($"{dir}/{name}: EN has {enCount} \\n, translation has {transCount} \\n");
                    }
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} \\n mismatches:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void NoTranslations_Have_Cyrillic_BackslashN()
        {
            // Catches cases where \n was transliterated to \н (Cyrillic н)
            var errors = new List<string>();

            foreach (var (dir, strings) in _translations)
            {
                foreach (var (name, value) in strings)
                {
                    if (value.Contains("\\н"))
                    {
                        errors.Add($"{dir}/{name}: contains Cyrillic \\н instead of \\n");
                    }
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} Cyrillic \\н found:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void NoTranslations_Have_Empty_Values()
        {
            var errors = new List<string>();

            foreach (var (dir, strings) in _translations)
            {
                foreach (var (name, value) in strings)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        errors.Add($"{dir}/{name}: empty or whitespace-only value");
                    }
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} empty translations:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void NoTranslations_Have_Unescaped_Apostrophes()
        {
            // In Android XML, apostrophes must be escaped as \' or the string must be quoted
            var errors = new List<string>();
            // Match a ' that is NOT preceded by \
            var unescapedApostrophe = new Regex(@"(?<!\\)'");

            foreach (var (dir, strings) in _translations)
            {
                // Re-read the raw XML to check escaping (XDocument would already parse escapes)
                var rawContent = File.ReadAllText(Path.Combine(ResourcesDir, dir, "strings.xml"));
                var rawMatches = Regex.Matches(rawContent, @"<string name=""([^""]+)"">([^<]*(?:<(?!/string>)[^<]*)*)</string>");

                foreach (Match m in rawMatches)
                {
                    var name = m.Groups[1].Value;
                    var rawValue = m.Groups[2].Value;

                    // Skip if the entire value is quoted
                    var trimmed = rawValue.Trim();
                    if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) continue;

                    // Skip CDATA sections — apostrophes don't need escaping inside CDATA
                    if (rawValue.Contains("<![CDATA[")) continue;

                    if (unescapedApostrophe.IsMatch(rawValue))
                    {
                        errors.Add($"{dir}/{name}: contains unescaped apostrophe");
                    }
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} unescaped apostrophes:\n" + string.Join("\n", errors.Take(50)));
            }
        }

        [Test]
        public void AllTranslations_Have_Valid_Xml()
        {
            var errors = new List<string>();

            foreach (var dir in Directory.GetDirectories(ResourcesDir, "values-*"))
            {
                if (dir.EndsWith("values-night")) continue;
                var path = Path.Combine(dir, "strings.xml");
                if (!File.Exists(path)) continue;

                try
                {
                    XDocument.Load(path);
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(dir)}: invalid XML - {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                Assert.Fail($"{errors.Count} invalid XML files:\n" + string.Join("\n", errors));
            }
        }
    }
}
