using System;
using Seeker.Helpers;

namespace Seeker.Services
{
    public class AndroidTransferListUpdater : ITransferListUpdater
    {
        public void NotifyItemChanged(int position)
        {
            if (StaticHacks.TransfersFrag != null)
            {
                StaticHacks.TransfersFrag.recyclerTransferAdapter?.NotifyItemChanged(position);
            }
        }

        public void RefreshListView(Action specificRefreshAction = null)
        {
            if (StaticHacks.TransfersFrag != null)
            {
                StaticHacks.TransfersFrag.refreshListView(specificRefreshAction);
            }
        }
    }
}
