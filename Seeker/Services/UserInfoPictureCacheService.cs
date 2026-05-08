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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Seeker.Helpers;

namespace Seeker.Services
{
    /// <summary>
    /// Size-bounded on-disk cache for peer profile pictures, keyed by username. Lets a Bundle
    /// save (Binder, ~1 MB cap) carry a username string instead of multi-megabyte byte arrays
    /// — the picture survives process death on disk and is read back on restore. Eviction is
    /// FIFO-by-write (oldest mtime first), enforced on every write.
    ///
    /// Public surface is async-by-default so I/O never lands on the UI thread by accident.
    /// INVARIANT: the async methods MUST stay implemented as `Task.Run(() => SyncBody())` with
    /// no `await` inside — this keeps `.Result` from the UI thread deadlock-safe (no captured
    /// SynchronizationContext to marshal back to). If you ever need to `await` something here,
    /// you must `.ConfigureAwait(false)` it AND remove .Result from any UI-thread caller.
    /// </summary>
    public class UserInfoPictureCacheService
    {
        public static UserInfoPictureCacheService Instance { get; set; }

        private const string CacheDirName = "userinfo_pictures";
        private const string FileExtension = ".bin";
        private const long DiskBudgetBytes = 30L * 1024 * 1024;

        public Task PutAsync(string username, byte[] bytes)
        {
            if (string.IsNullOrEmpty(username) || bytes == null || bytes.Length == 0)
            {
                return Task.CompletedTask;
            }
            return Task.Run(() => TryWrite(username, bytes));
        }

        public Task<byte[]> GetAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return Task.FromResult<byte[]>(null);
            }
            return Task.Run(() => TryRead(GetFilenameFor(username)));
        }

        private static string GetFilenameFor(string username)
        {
            return SanitizeAndHashUsername(username) + FileExtension;
        }

        private static string TryWrite(string username, byte[] bytes)
        {
            try
            {
                string filename = GetFilenameFor(username);
                string dir = GetCacheDir(createIfMissing: true);
                if (dir == null)
                {
                    return null;
                }
                string path = Path.Combine(dir, filename);
                File.WriteAllBytes(path, bytes);
                TrimToBudget(filename);
                return filename;
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to write user info picture cache: " + ex.Message);
                return null;
            }
        }

        private static byte[] TryRead(string filename)
        {
            try
            {
                string dir = GetCacheDir(createIfMissing: false);
                if (dir == null)
                {
                    return null;
                }
                string path = Path.Combine(dir, filename);
                if (!File.Exists(path))
                {
                    return null;
                }
                return File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to read user info picture cache: " + ex.Message);
                return null;
            }
        }

        // Sorts entries oldest-first by mtime, evicts until under budget. The just-written
        // filename is preserved explicitly: filesystem mtime resolution can be 1s, so a sort
        // tie could otherwise leave the new file eligible for eviction the moment it lands.
        private static void TrimToBudget(string preserveFilename = null)
        {
            try
            {
                string dir = GetCacheDir(createIfMissing: false);
                if (dir == null)
                {
                    return;
                }
                string[] paths = Directory.GetFiles(dir);
                if (paths.Length == 0)
                {
                    return;
                }
                var infos = new FileInfo[paths.Length];
                long total = 0;
                for (int i = 0; i < paths.Length; i++)
                {
                    infos[i] = new FileInfo(paths[i]);
                    total += infos[i].Length;
                }
                if (total <= DiskBudgetBytes)
                {
                    return;
                }
                Array.Sort(infos, (a, b) => a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));
                for (int i = 0; i < infos.Length; i++)
                {
                    if (total <= DiskBudgetBytes)
                    {
                        break;
                    }
                    var info = infos[i];
                    if (preserveFilename != null && string.Equals(info.Name, preserveFilename, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    long size = info.Length;
                    try
                    {
                        info.Delete();
                        total -= size;
                    }
                    catch (Exception ex)
                    {
                        Logger.Firebase("Failed to evict user info picture cache entry: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Firebase("Failed to trim user info picture cache: " + ex.Message);
            }
        }

        private static string GetCacheDir(bool createIfMissing)
        {
            var ctx = SeekerApplication.ApplicationContext;
            if (ctx == null || ctx.CacheDir == null)
            {
                return null;
            }
            string dir = Path.Combine(ctx.CacheDir.AbsolutePath, CacheDirName);
            if (!Directory.Exists(dir))
            {
                if (!createIfMissing)
                {
                    return null;
                }
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        // Sanitized stem keeps cache files vaguely human-readable; the hash suffix is what
        // guarantees uniqueness across distinct usernames that would otherwise collapse to
        // the same sanitized form (e.g. "user@1" and "user 1" both -> "user_1").
        private static string SanitizeAndHashUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return "_";
            }
            var sb = new StringBuilder(username.Length + 9);
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
            sb.Append('_');
            sb.Append(ShortHash(username));
            return sb.ToString();
        }

        private static string ShortHash(string username)
        {
            using (var sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(username));
                var hex = new StringBuilder(8);
                for (int i = 0; i < 4; i++)
                {
                    hex.Append(hash[i].ToString("x2"));
                }
                return hex.ToString();
            }
        }
    }
}
