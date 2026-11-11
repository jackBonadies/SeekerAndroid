using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Seeker
{
    public enum TransferSortKey
    {
        DateAdded = 0,
        FolderName = 1,
        User = 2,
    }

    public static class TransferSort
    {
        private static long lastSeq;

        public static long NextSeq()
        {
            return Interlocked.Increment(ref lastSeq);
        }

        public static long GetSeq(ITransferItem item)
        {
            if (item is FolderItem fi)
            {
                return fi.Seq;
            }
            return ((TransferItem)item).Seq;
        }

        private struct Entry
        {
            public string Primary;
            public string Secondary;
            public string Tertiary;
            public long Seq;
            public ITransferItem Item;
        }

        // the direction applies to the primary key only, ties are always ascending
        public static void Sort(List<ITransferItem> items, TransferSortKey key, bool descending)
        {
            if (items.Count < 2)
            {
                return;
            }
            var entries = new Entry[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                entries[i] = CreateEntry(items[i], key);
            }

            int direction = descending ? -1 : 1;
            if (key == TransferSortKey.DateAdded)
            {
                Array.Sort(entries, (a, b) => a.Seq.CompareTo(b.Seq) * direction);
            }
            else
            {
                // user facing so current culture
                CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;
                Array.Sort(entries, (a, b) =>
                {
                    int c = compareInfo.Compare(a.Primary, b.Primary, CompareOptions.IgnoreCase);
                    if (c != 0)
                    {
                        return c * direction;
                    }
                    c = compareInfo.Compare(a.Secondary, b.Secondary, CompareOptions.IgnoreCase);
                    if (c != 0)
                    {
                        return c;
                    }
                    c = compareInfo.Compare(a.Tertiary, b.Tertiary, CompareOptions.IgnoreCase);
                    if (c != 0)
                    {
                        return c;
                    }
                    return a.Seq.CompareTo(b.Seq);
                });
            }

            for (int i = 0; i < entries.Length; i++)
            {
                items[i] = entries[i].Item;
            }
        }

        private static Entry CreateEntry(ITransferItem item, TransferSortKey key)
        {
            var entry = new Entry { Item = item, Seq = GetSeq(item), Primary = string.Empty, Secondary = string.Empty, Tertiary = string.Empty };
            if (key == TransferSortKey.DateAdded)
            {
                return entry;
            }
            if (item is FolderItem fi)
            {
                if (key == TransferSortKey.FolderName)
                {
                    entry.Primary = fi.GetDisplayFolderName() ?? string.Empty;
                    entry.Secondary = fi.Username ?? string.Empty;
                }
                else
                {
                    entry.Primary = fi.Username ?? string.Empty;
                }
                return entry;
            }

            var ti = (TransferItem)item;
            if (key == TransferSortKey.FolderName)
            {
                entry.Primary = ti.Folder?.GetDisplayFolderName() ?? ti.FolderName ?? string.Empty;
                entry.Secondary = ti.Username ?? string.Empty;
                entry.Tertiary = ti.Filename ?? string.Empty;
            }
            else
            {
                entry.Primary = ti.Username ?? string.Empty;
            }
            return entry;
        }
    }
}
