using System.Collections.Generic;

namespace Seeker
{
    // UI owned, for transfers adapter
    public sealed class TransferListSnapshot
    {
        private readonly List<ITransferItem> items;
        // transferItem -> key we used to sort it. null unless the sort key is mutable (TransferSort.IsMutable)
        private readonly Dictionary<ITransferItem, TransferBandKey> sortKeys;
        private readonly TransferSortKey sortKey;

        public TransferListSnapshot(List<ITransferItem> items) : this(items, null, TransferSortKey.DateAdded)
        {
        }

        public TransferListSnapshot(List<ITransferItem> items, Dictionary<ITransferItem, TransferBandKey> sortKeys, TransferSortKey sortKey)
        {
            this.items = items;
            this.sortKeys = sortKeys;
            this.sortKey = sortKey;
        }

        // true if the row would now sort somewhere else i.e. the list needs a rebuild
        public bool HasSortKeyChanged(ITransferItem item)
        {
            if (item == null || sortKeys == null || !sortKeys.TryGetValue(item, out TransferBandKey sortedBy))
            {
                return false;
            }
            return !sortedBy.Equals(TransferSort.MutableKeyOf(item, sortKey));
        }

        public int Count => items.Count;

        public IReadOnlyList<ITransferItem> Items => items;

        public ITransferItem GetItem(int position)
        {
            if (position < 0 || position >= items.Count)
            {
                return null;
            }
            return items[position];
        }

        public int IndexOf(ITransferItem item)
        {
            if (item == null)
            {
                return -1;
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public void RemoveAt(int position)
        {
            sortKeys?.Remove(items[position]);
            items.RemoveAt(position);
        }
    }
}
