using System.ServiceProcess;

namespace Watchdog.Interfaces;

internal interface IServiceControllerFactory
{
    IServiceControllerWrapper Create(string serviceName);
}

internal interface IServiceControllerWrapper : IDisposable
{
    ServiceControllerStatus Status { get; }
    void Start();
    void WaitForStatus(ServiceControllerStatus status, TimeSpan timeout);
}
