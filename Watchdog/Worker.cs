using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Watchdog.Interfaces;
using Watchdog.Models;

namespace Watchdog;

internal class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILoggerService _loggerService;
    private readonly IAppMonitoringService _appMonitoringService;
    private readonly ISvcMonitoringService _svcMonitoringService;

    private readonly Dictionary<string, CancellationTokenSource> _appTokens = [];
    private readonly Dictionary<string, CancellationTokenSource> _svcTokens = [];

    public Worker(IConfiguration config, ILoggerService loggerService, IAppMonitoringService appMonitoringService, ISvcMonitoringService svcMonitoringService)
    {
        _config = config;
        _loggerService = loggerService;
        _appMonitoringService = appMonitoringService;
        _svcMonitoringService = svcMonitoringService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        void RegisterReloadCallback()
        {
            _config.GetReloadToken().RegisterChangeCallback(_ =>
            {
                Log.Information("Configuration change detected — reloading...");

                RestartMonitoring(stoppingToken);
                RegisterReloadCallback();

            }, null);
        }

        RegisterReloadCallback();
        RestartMonitoring(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        { }

        Log.Information("Watchdog Service stopped");
    }

    private void RestartMonitoring(CancellationToken globalToken)
    {
        CancelAllMonitoring();

        var apps = _config.GetSection("WatchdogConfig:Applications").Get<List<AppConfig>>() ?? [];
        var services = _config.GetSection("WatchdogConfig:WindowsServices").Get<List<SvcConfig>>() ?? [];

        Log.Information("Loaded {AppCount} apps and {SvcCount} services for monitoring", apps.Count, services.Count);

        var allNames = apps.Select(a => a.ProcessName)
                           .Concat(services.Select(s => s.ServiceName))
                           .ToArray();
        _loggerService.ConfigureLoggers(allNames);

        Log.Information("Starting apps monitoring");
        foreach (var app in apps)
        {
            var logger = _loggerService.GetLogger(app.ProcessName);
            StartAppMonitoring(app, logger, globalToken);
        }

        Log.Information("Starting services monitoring");
        foreach (var svc in services)
        {
            var logger = _loggerService.GetLogger(svc.ServiceName);
            StartServiceMonitoring(svc, logger, globalToken);
        }

        Log.Information("Monitoring restarted: {AppCount} apps, {SvcCount} services", apps.Count, services.Count);
    }

    private void CancelAllMonitoring()
    {
        Log.Information("Canceling apps monitoring");

        foreach (var kvp in _appTokens)
        {
            Log.Information("Canceling {ProcessName} app monitoring", kvp.Key);
            kvp.Value.Cancel();
        }
        _appTokens.Clear();

        Log.Information("Canceling services monitoring");

        foreach (var kvp in _svcTokens)
        {
            Log.Information("Canceling {ServiceName} service monitoring", kvp.Key);
            kvp.Value.Cancel();
        }
        _svcTokens.Clear();
    }

    private void StartAppMonitoring(AppConfig app, ILogger logger, CancellationToken globalToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
        _appTokens[app.ProcessName] = cts;

        Log.Information("Starting {ProcessName} app monitoring", app.ProcessName);

        Task.Run(async () =>
        {
            try
            {
                await _appMonitoringService.MonitorAppAsync(app, logger, cts.Token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Monitoring task for {ProcessName} failed", app.ProcessName);
            }
        }, cts.Token);
    }

    private void StartServiceMonitoring(SvcConfig svc, ILogger logger, CancellationToken globalToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
        _svcTokens[svc.ServiceName] = cts;

        Log.Information("Starting {ServiceName} service monitoring", svc.ServiceName);

        Task.Run(async () =>
        {
            try
            {
                await _svcMonitoringService.MonitorServiceAsync(svc, logger, cts.Token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Monitoring task for {ServiceName} failed", svc.ServiceName);
            }
        }, cts.Token);
    }
}