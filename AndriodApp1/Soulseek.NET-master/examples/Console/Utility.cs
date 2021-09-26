using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Console
{
    public static class Utility
    {
        public static string ToSafeFilename(this string text)
        {
            return string.Join("-", text.Split(Path.GetInvalidFileNameChars()));
        }

        public static double Similarity(this string s, string t)
        {
            return (1.0 - ((double)s.LevenshteinDistance(t) / (double)Math.Max(s.Length, t.Length)));
        }

        public static double SimilarityCaseInsensitive(this string s, string t)
        {
            return (1.0 - ((double)s.LevenshteinDistanceCaseInsensitive(t) / (double)Math.Max(s.Length, t.Length)));
        }

        public static int LevenshteinDistanceCaseInsensitive(this string s, string t)
        {
            return s.ToLower().LevenshteinDistance(t.ToLower());
        }

        public static int LevenshteinDistance(this string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        public static string ToKB(this double size)
        {
            return $"{size / 1000:N2}KB";
        }

        public static string ToKB(this int size)
        {
            return ((long)size).ToKB();
        }

        public static string ToKB(this long size)
        {
            return $"{size / (double)1000:N2}KB";
        }

        public static string ToMB(this double size)
        {
            return $"{size / 1000000:N2}MB";
        }

        public static string ToMB(this long size)
        {
            return $"{size / (double)1000000:N2}MB";
        }

        public static string ToMB(this int size)
        {
            return ((long)size).ToMB();
        }

        public static string ToLocalOSPath(this string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }

    public class SemiNumericComparer : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            if (IsNumeric(s1) && IsNumeric(s2))
            {
                if (Convert.ToInt32(s1) > Convert.ToInt32(s2)) return 1;
                if (Convert.ToInt32(s1) < Convert.ToInt32(s2)) return -1;
                if (Convert.ToInt32(s1) == Convert.ToInt32(s2)) return 0;
            }

            if (IsNumeric(s1) && !IsNumeric(s2))
                return -1;

            if (!IsNumeric(s1) && IsNumeric(s2))
                return 1;

            return string.Compare(s1, s2, true);
        }

        public static bool IsNumeric(object value)
        {
            try
            {
                int i = Convert.ToInt32(value.ToString());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
