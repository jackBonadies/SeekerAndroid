using System.Collections.Generic;

namespace Seeker
{
    // UI owned, for transfers adapter
    public sealed class TransferListSnapshot
    {
        public static TransferListSnapshot Empty => new TransferListSnapshot(new List<ITransferItem>());

        private readonly List<ITransferItem> items;
        private Dictionary<ITransferItem, int>? positions;

        public TransferListSnapshot(List<ITransferItem> items)
        {
            this.items = items;
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
            // TODO revist this logic...
            if (positions == null)
            {
                positions = new Dictionary<ITransferItem, int>(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    positions[items[i]] = i;
                }
            }
            return positions.TryGetValue(item, out int pos) ? pos : -1;
        }

        public void RemoveAt(int position)
        {
            items.RemoveAt(position);
            // rebuilt lazily on the next lookup
            positions = null;
        }
    }
}
