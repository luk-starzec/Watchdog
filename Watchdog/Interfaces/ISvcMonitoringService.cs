using Serilog;
using Watchdog.Models;

namespace Watchdog.Interfaces;

internal interface ISvcMonitoringService
{
    Task MonitorServiceAsync(SvcConfig svc, ILogger logger, CancellationToken token);
}