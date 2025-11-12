using Serilog;
using Watchdog.Models;

namespace Watchdog.Interfaces;

internal interface IAppMonitoringService
{
    Task MonitorAppAsync(AppConfig app, ILogger logger, CancellationToken token);
}