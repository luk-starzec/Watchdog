using Serilog;
using Watchdog.Interfaces;
using Watchdog.Models;

namespace Watchdog.Services;

internal class AppMonitoringService : IAppMonitoringService
{
    private readonly IRestartHistoryService _restartHistoryService;
    private readonly IProcessService _processService;

    public AppMonitoringService(IRestartHistoryService restartHistoryService, IProcessService processService)
    {
        _restartHistoryService = restartHistoryService;
        _processService = processService;
    }

    public async Task MonitorAppAsync(AppConfig app, ILogger logger, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var isRunning = _processService.IsProcessRunning(app.ProcessName);

                if (isRunning)
                {
                    logger.Debug("{ProcessName} is working correctly", app.ProcessName);
                }
                else
                {
                    await TryRestartAppAsync(app, logger, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Task is being cancelled, exit gracefully
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ProcessName} monitoring error", app.ProcessName);
                logger.Error(ex, "{ProcessName} monitoring error", app.ProcessName);
            }

            await Task.Delay(app.CheckInterval, token);
        }

        logger.Information("{ProcessName} monitoring stopped", app.ProcessName);
    }

    private async Task TryRestartAppAsync(AppConfig app, ILogger logger, CancellationToken token)
    {
        if (!_restartHistoryService.CanRestart(app.ProcessName, app.MaxRestartsPerWindow, app.RestartWindow))
        {
            logger.Error("{ProcessName} restart limit exceeded ({MaxRestartsPerWindow}/{RestartWindow})", app.ProcessName, app.MaxRestartsPerWindow, app.RestartWindow);
            return;
        }

        logger.Warning("{ProcessName} not running - starting {ExePath}", app.ProcessName, app.ExePath);

        var success = false;

        for (var attempt = 1; attempt <= app.RestartAttempts; attempt++)
        {
            token.ThrowIfCancellationRequested();

            success = _processService.StartProcess(app.ExePath, app.ProcessName);

            if (success)
            {
                logger.Information("{ProcessName} started after {Attempt} attempt", app.ProcessName, attempt);
                success = true;
                break;
            }

            logger.Warning("{ProcessName} failed to start after {Attempt}/{Total} attempts", app.ProcessName, attempt, app.RestartAttempts);

            if (attempt < app.RestartAttempts)
            {
                await Task.Delay(app.RestartRetryWait, token);
            }
        }

        _restartHistoryService.RegisterRestart(app.ProcessName);

        if (!success)
        {
            logger.Error("{ProcessName} failed to start after {Total} attempts", app.ProcessName, app.RestartAttempts);
        }
    }
}