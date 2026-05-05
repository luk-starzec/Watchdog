using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Watchdog.Interfaces;

namespace Watchdog.Services;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
internal class ServiceControllerService : IServiceControllerService
{
    private readonly IServiceControllerFactory _controllerFactory;
    private readonly IManagementObjectSearcherFactory _searcherFactory;
    private readonly ILoggerService _loggerService;

    public ServiceControllerService(IServiceControllerFactory controllerFactory, IManagementObjectSearcherFactory searcherFactory, ILoggerService loggerService)
    {
        _controllerFactory = controllerFactory;
        _searcherFactory = searcherFactory;
        _loggerService = loggerService;
    }

    public string GetStartMode(string serviceName)
    {
        try
        {
            var searcher = _searcherFactory.Create($"SELECT StartMode FROM Win32_Service WHERE Name = '{serviceName}'");

            foreach (var obj in searcher.Get())
            {
                return obj["StartMode"]?.ToString() ?? "Unknown";
            }
        }
        catch (Exception ex)
        {
            var logger = _loggerService.GetLogger(serviceName);
            logger.Error(ex, "{ServiceName} getting start mode error", serviceName);
        }

        return "Unknown";
    }

    public ServiceControllerStatus GetStatus(string serviceName)
    {
        try
        {
            using var controller = _controllerFactory.Create(serviceName);

            return controller.Status;
        }
        catch (Exception ex)
        {
            var logger = _loggerService.GetLogger(serviceName);
            logger.Error(ex, "{ServiceName} getting status error", serviceName);
            return ServiceControllerStatus.Stopped;
        }
    }

    public bool StartService(string serviceName, TimeSpan? timeout = null)
    {
        var logger = _loggerService.GetLogger(serviceName);

        try
        {
            using var controller = _controllerFactory.Create(serviceName);

            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, timeout ?? TimeSpan.FromSeconds(30));

            if (controller.Status != ServiceControllerStatus.Running)
            {
                logger.Error("{ServiceName} starting timeout", serviceName);
                return false;
            }

            return true;
        }
        catch (Exception startEx)
        {
            logger.Error(startEx, "{ServiceName} starting error", serviceName);
        }
        return false;
    }
}