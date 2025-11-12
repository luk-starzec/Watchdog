using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class ServiceControllerFactory : IServiceControllerFactory
{
    public IServiceControllerWrapper Create(string serviceName) => new ServiceControllerWrapper(serviceName);
}

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class ServiceControllerWrapper : IServiceControllerWrapper
{
    private readonly ServiceController _controller;

    public ServiceControllerWrapper(string serviceName)
    {
        _controller = new ServiceController(serviceName);
    }

    public ServiceControllerStatus Status => _controller.Status;

    public void Start()
    {
        _controller.Start();
    }

    public void WaitForStatus(ServiceControllerStatus status, TimeSpan timeout)
    {
        _controller.WaitForStatus(status, timeout);
    }

    public void Dispose()
    {
        _controller.Dispose();
    }
}
