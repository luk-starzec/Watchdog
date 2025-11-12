using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Watchdog;
using Watchdog.Interfaces;
using Watchdog.Services;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

bool runAsConsole = configuration.GetValue<bool>("RunAsConsole")
                    || args.Contains("--console", StringComparer.OrdinalIgnoreCase);

#if DEBUG
runAsConsole = true;
#endif

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting Watchdog Service (mode: {Mode})", runAsConsole ? "Console" : "WindowsService");

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((_, config) =>
        {
            config.AddConfiguration(configuration);
        })
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.AddHostedService<Worker>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IAppMonitoringService, AppMonitoringService>();
            services.AddSingleton<ISvcMonitoringService, SvcMonitoringService>();
            services.AddTransient<IRestartHistoryService, RestartHistoryService>();
            services.AddTransient<IProcessService, ProcessService>();
            services.AddTransient<IProcessLauncher, ProcessLauncher>();
            services.AddTransient<IServiceControllerService, ServiceControllerService>();
            services.AddTransient<IServiceControllerFactory, ServiceControllerFactory>();
            services.AddTransient<IManagementObjectSearcherFactory, ManagementObjectSearcherFactory>();
        });

    if (!runAsConsole)
    {
        builder.UseWindowsService(options => options.ServiceName = "Watchdog Service");
    }

    var host = builder.Build();

    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fatal error while starting Watchdog Service");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
