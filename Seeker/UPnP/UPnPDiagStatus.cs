namespace Seeker.UPnP
{
    public enum UPnPDiagStatus
    {
        None = 0,
        UpnpDisabled = 1,
        WifiDisabled = 2,
        NoUpnpDevicesFound = 3,
        UpnpDeviceFoundButFailedToMap = 4,
        Success = 5,
        NoWifiConnection = 6, //wifi is enabled but not connected to any particular connection
        ErrorUnspecified = 10
    }//what about captive portal??


}