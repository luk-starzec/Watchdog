using Serilog;
using System.ServiceProcess;
using Watchdog.Interfaces;
using Watchdog.Models;

namespace Watchdog.Services;

internal class SvcMonitoringService : ISvcMonitoringService
{
    private readonly IRestartHistoryService _restartHistoryService;
    private readonly IServiceControllerService _serviceControllerService;

    public SvcMonitoringService(IRestartHistoryService restartHistoryService, ILoggerService loggerService, IServiceControllerService serviceControllerService)
    {
        _restartHistoryService = restartHistoryService;
        _serviceControllerService = serviceControllerService;
    }

    public async Task MonitorServiceAsync(SvcConfig svc, ILogger logger, CancellationToken token)
    {
        var (shouldSkip, mode) = GetSkipDetails(svc.ServiceName);
        if (shouldSkip)
        {
            logger.Warning("{ServiceName} skipped (mode: {Mode}).", svc.ServiceName, mode);
            return;
        }

        while (!token.IsCancellationRequested)
        {
            try
            {
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
            catch (OperationCanceledException)
            {
                // Task is being cancelled, exit gracefully
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ServiceName} monitoring error", svc.ServiceName);
                logger.Error(ex, "{ServiceName} monitoring error", svc.ServiceName);
            }

            await Task.Delay(svc.CheckInterval, token);
        }

        logger.Information("{ServiceName} monitoring stopped", svc.ServiceName);
    }

    private (bool shouldSkip, string mode) GetSkipDetails(string serviceName)
    {
        try
        {
            var startMode = _serviceControllerService.GetStartMode(serviceName);
            return (startMode != "Automatic" && startMode != "Auto", startMode);
        }
        catch
        {
            return (true, "Error");
        }
    }

    private async Task TryRestartServiceAsync(SvcConfig svc, ILogger log, CancellationToken token)
    {
        if (!_restartHistoryService.CanRestart(svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindow))
        {
            log.Error("{ServiceName} restart limit exceeded ({MaxRestartsPerWindow}/{RestartWindow})", svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindow);
            return;
        }

        log.Warning("{ServiceName} not running - starting...", svc.ServiceName);

        var success = false;

        for (var attempt = 1; attempt <= svc.RestartAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();

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

            await Task.Delay(svc.RestartRetryWait, token);
        }

        _restartHistoryService.RegisterRestart(svc.ServiceName);

        if (!success)
        {
            log.Error("{ServiceName} failed to start after {Total} attempts", svc.ServiceName, svc.RestartAttempts);
        }
    }
}