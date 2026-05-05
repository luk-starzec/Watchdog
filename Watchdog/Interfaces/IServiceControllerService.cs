using System.ServiceProcess;

namespace Watchdog.Interfaces;

internal interface IServiceControllerService
{
    string GetStartMode(string serviceName);
    ServiceControllerStatus GetStatus(string serviceName);
    bool StartService(string serviceName, TimeSpan? timeout = null);
}