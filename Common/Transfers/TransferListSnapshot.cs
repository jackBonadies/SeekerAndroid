using System.Collections.Generic;

namespace Seeker
{
    // UI owned, for transfers adapter
    public sealed class TransferListSnapshot
    {
        private readonly List<ITransferItem> items;

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
            items.RemoveAt(position);
        }
    }
}
