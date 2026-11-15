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
        State = 3,
        LastAction = 4,
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

        // keys that change while a row is listed, so the snapshot remembers what each row was sorted by
        public static bool IsMutable(TransferSortKey key)
        {
            return key == TransferSortKey.State || key == TransferSortKey.LastAction;
        }

        // the part of a row's band and stamp that the key sorts on
        public static TransferBandKey MutableKeyOf(ITransferItem item, TransferSortKey key)
        {
            TransferBandKey full = TransferBands.KeyOf(item);
            // i.e. if LastAction, ignore the "band" part (force to active)
            return key == TransferSortKey.LastAction ? new TransferBandKey(TransferBand.Active, full.Stamp) : full;
        }

        private struct Entry
        {
            public string Primary;
            public string Secondary;
            public string Tertiary;
            public long Seq;
            public TransferBandKey BandKey;
            public ITransferItem Item;
        }

        // the direction applies to the primary key only, ties are always ascending
        public static void Sort(List<ITransferItem> items, TransferSortKey key, bool descending)
        {
            Sort(items, key, descending, null);
        }

        // sortKeys: for a mutable key, receives the key each row was sorted by
        public static void Sort(List<ITransferItem> items, TransferSortKey key, bool descending, Dictionary<ITransferItem, TransferBandKey>? sortKeys)
        {
            var entries = new Entry[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                entries[i] = CreateEntry(items[i], key);
                if (IsMutable(key) && sortKeys != null)
                {
                    sortKeys[items[i]] = entries[i].BandKey;
                }
            }
            if (items.Count < 2)
            {
                return;
            }

            int direction = descending ? -1 : 1;
            if (key == TransferSortKey.DateAdded)
            {
                Array.Sort(entries, (a, b) => a.Seq.CompareTo(b.Seq) * direction);
            }
            else if (key == TransferSortKey.State)
            {
                // the most recently touched in a band is first whether ascending or descending
                Array.Sort(entries, (a, b) =>
                {
                    int c = a.BandKey.Band.CompareTo(b.BandKey.Band);
                    if (c != 0)
                    {
                        return c * direction;
                    }
                    c = b.BandKey.Stamp.CompareTo(a.BandKey.Stamp);
                    if (c != 0)
                    {
                        return c;
                    }
                    return a.Seq.CompareTo(b.Seq);
                });
            }
            else if (key == TransferSortKey.LastAction)
            {
                // a batch shares a stamp, so it stays in date added (track) order
                Array.Sort(entries, (a, b) =>
                {
                    int c = a.BandKey.Stamp.CompareTo(b.BandKey.Stamp);
                    if (c != 0)
                    {
                        return c * direction;
                    }
                    return a.Seq.CompareTo(b.Seq);
                });
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
            if (IsMutable(key))
            {
                entry.BandKey = MutableKeyOf(item, key);
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
