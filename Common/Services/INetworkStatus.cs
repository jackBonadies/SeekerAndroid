namespace Seeker.Services
{
    public interface INetworkStatus
    {
        bool DoWeHaveInternet();
        bool HasHandoffOccuredRecently();
    }
}
