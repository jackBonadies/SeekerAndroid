using System;

namespace Seeker.Services
{
    public interface ITransferListUpdater
    {
        void NotifyItemChanged(int position);
        void RefreshListView(Action specificRefreshAction = null);
    }
}
