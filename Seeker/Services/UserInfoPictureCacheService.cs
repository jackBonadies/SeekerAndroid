/*
 * Copyright 2021 Seeker
 *
 * This file is part of Seeker
 *
 * Seeker is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Seeker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Seeker. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using Seeker.Helpers;

namespace Seeker.Services
{
    /// <summary>
    /// On-disk cache for peer profile pictures so that a Bundle save (which travels through
    /// Binder, ~1 MB cap) can store a filename instead of multi-megabyte byte arrays.
    /// Eviction is size-bounded LRU (and done on write), mirroring how Glide / DiskLruCache bound their caches.
    /// </summary>
    public class UserInfoPictureCacheService
    {
        public static UserInfoPictureCacheService Instance { get; set; }

        private const string CacheDirName = "userinfo_pictures";
        private const string FileExtension = ".bin";
        private const long BudgetBytes = 30L * 1024 * 1024;

        public string GetFilenameFor(string username)
        {
            return SanitizeUsernameForFilename(username) + FileExtension;
        }

        public string TryWrite(string username, byte[] bytes)
        {
            try
            {
                string filename = GetFilenameFor(username);
                var dir = GetCacheDir(createIfMissing: true);
                var file = new Java.IO.File(dir, filename);
                System.IO.File.WriteAllBytes(file.AbsolutePath, bytes);
                TrimToBudget();
                return filename;
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to write user info picture cache: " + ex.Message);
                return null;
            }
        }

        public byte[] TryRead(string filename)
        {
            try
            {
                var dir = GetCacheDir(createIfMissing: false);
                if (dir == null)
                {
                    return null;
                }
                var file = new Java.IO.File(dir, filename);
                if (!file.Exists())
                {
                    return null;
                }
                return System.IO.File.ReadAllBytes(file.AbsolutePath);
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to read user info picture cache: " + ex.Message);
                return null;
            }
        }

        public void Delete(string filename)
        {
            try
            {
                var dir = GetCacheDir(createIfMissing: false);
                if (dir == null)
                {
                    return;
                }
                var file = new Java.IO.File(dir, filename);
                if (file.Exists())
                {
                    file.Delete();
                }
            }
            catch (Exception)
            {
            }
        }

        // Sorts entries oldest-first by mtime, evicts until under budget. Never evicts
        // the newest entry: a single picture larger than the budget would otherwise be
        // deleted moments after being written.
        public void TrimToBudget()
        {
            try
            {
                var dir = GetCacheDir(createIfMissing: false);
                if (dir == null)
                {
                    return;
                }
                var files = dir.ListFiles();
                if (files == null || files.Length == 0)
                {
                    return;
                }
                long total = 0;
                foreach (var f in files)
                {
                    total += f.Length();
                }
                if (total <= BudgetBytes)
                {
                    return;
                }
                Array.Sort(files, (a, b) => a.LastModified().CompareTo(b.LastModified()));
                for (int i = 0; i < files.Length - 1; i++)
                {
                    if (total <= BudgetBytes)
                    {
                        break;
                    }
                    var f = files[i];
                    long size = f.Length();
                    if (f.Delete())
                    {
                        total -= size;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to trim user info picture cache: " + ex.Message);
            }
        }

        private static Java.IO.File GetCacheDir(bool createIfMissing)
        {
            var ctx = SeekerApplication.ApplicationContext;
            if (ctx == null || ctx.CacheDir == null)
            {
                return null;
            }
            var dir = new Java.IO.File(ctx.CacheDir, CacheDirName);
            if (!dir.Exists())
            {
                if (!createIfMissing)
                {
                    return null;
                }
                dir.Mkdirs();
            }
            return dir;
        }

        private static string SanitizeUsernameForFilename(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return "_";
            }
            var sb = new System.Text.StringBuilder(username.Length);
            foreach (char c in username)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
    }
}
