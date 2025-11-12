using Serilog;
using System.ServiceProcess;
using Watchdog.Interfaces;
using Watchdog.Models;

namespace Watchdog.Services;

internal class SvcMonitoringService : ISvcMonitoringService
{
    internal int RestartRetryWaitInSeconds { get; set; } = 10;

    private readonly IRestartHistoryService _restartHistoryService;
    private readonly IServiceControllerService _serviceControllerService;

    public SvcMonitoringService(IRestartHistoryService restartHistoryService, ILoggerService loggerService, IServiceControllerService serviceControllerService)
    {
        _restartHistoryService = restartHistoryService;
        _serviceControllerService = serviceControllerService;
    }

    public async Task MonitorServiceAsync(SvcConfig svc, ILogger logger, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (ShouldSkipService(svc.ServiceName, logger))
                {
                    await DelayAsync(svc.CheckIntervalSeconds, token);
                    continue;
                }

                var status = _serviceControllerService.GetStatus(svc.ServiceName);

                if (status == ServiceControllerStatus.Running)
                {
                    logger.Debug("{ServiceName} is working correctly", svc.ServiceName);
                }
                else
                {
                    await TryRestartServiceAsync(svc, logger, token);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ServiceName} monitoring error", svc.ServiceName);
                logger.Error(ex, "{ServiceName} monitoring error", svc.ServiceName);
            }

            await DelayAsync(svc.CheckIntervalSeconds, token);
        }

        logger.Information("{ServiceName} monitoring stopped", svc.ServiceName);
    }

    private bool ShouldSkipService(string serviceName, ILogger logger)
    {
        var startMode = _serviceControllerService.GetStartMode(serviceName);

        if (startMode != "Automatic" && startMode != "Auto")
        {
            logger.Warning("{ServiceName} skipped (mode: {Mode}).", serviceName, startMode);
            return true;
        }

        return false;
    }


    private async Task DelayAsync(int delayInSeconds, CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), token);
    }

    private async Task TryRestartServiceAsync(SvcConfig svc, ILogger log, CancellationToken token)
    {
        if (!_restartHistoryService.CanRestart(svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindowSeconds))
        {
            log.Error("{ServiceName} restart limit exceeded ({MaxRestartsPerWindow}/{RestartWindowSeconds}sec)", svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindowSeconds);
            return;
        }

        log.Warning("{ServiceName} not running - starting...", svc.ServiceName);

        var success = false;

        for (var attempt = 1; attempt <= svc.RestartAttempts; attempt++)
        {
            success = _serviceControllerService.StartService(svc.ServiceName);

            if (success)
            {
                log.Information("{ServiceName} started after {Attempt} attempt", svc.ServiceName, attempt);

                success = true;
                break;
            }
            else
            {
                log.Warning("{ServiceName} failed to start after {Attempt}/{Total} attempts", svc.ServiceName, attempt, svc.RestartAttempts);
            }

            await Task.Delay(TimeSpan.FromSeconds(RestartRetryWaitInSeconds), token);
        }

        _restartHistoryService.RegisterRestart(svc.ServiceName);

        if (!success)
        {
            log.Error("{ServiceName} failed to start after {Total} attempts", svc.ServiceName, svc.RestartAttempts);
        }
    }
}
