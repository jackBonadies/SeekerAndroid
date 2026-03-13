using Seeker.Helpers;

namespace Seeker.Services
{
    public class AndroidNetworkStatus : INetworkStatus
    {
        public bool DoWeHaveInternet()
        {
            return ConnectionReceiver.DoWeHaveInternet();
        }

        public bool HasHandoffOccuredRecently()
        {
            return NetworkHandoffDetector.HasHandoffOccuredRecently();
        }
    }
}
