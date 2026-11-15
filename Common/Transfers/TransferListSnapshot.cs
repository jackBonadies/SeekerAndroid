using System.Collections.Generic;

namespace Seeker
{
    // UI owned, for transfers adapter
    public sealed class TransferListSnapshot
    {
        private readonly List<ITransferItem> items;
        // transferItem -> key we used to sort it
        private readonly Dictionary<ITransferItem, TransferBandKey> bandKeys;

        public TransferListSnapshot(List<ITransferItem> items) : this(items, null)
        {
        }

        public TransferListSnapshot(List<ITransferItem> items, Dictionary<ITransferItem, TransferBandKey> bandKeys)
        {
            this.items = items;
            this.bandKeys = bandKeys;
        }

        // true if the row would now sort somewhere else i.e. the list needs a rebuild
        public bool HasBandChanged(ITransferItem item)
        {
            if (item == null || bandKeys == null || !bandKeys.TryGetValue(item, out TransferBandKey sortedBy))
            {
                return false;
            }
            return !sortedBy.Equals(TransferBands.KeyOf(item));
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
            bandKeys?.Remove(items[position]);
            items.RemoveAt(position);
        }
    }
}
