using Microsoft.Extensions.Configuration;
using Serilog;
using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class LoggerService : ILoggerService
{
    private const string ConfigSection = "DynamicLoggerBase";

    private readonly IConfiguration _configuration;

    private Dictionary<string, ILogger> _loggers = [];

    public LoggerService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureLoggers(string[] names)
    {
        _loggers = names.ToDictionary(k => k, ConfigureLogger);
    }

    public ILogger GetLogger(string name)
    {
        return _loggers.TryGetValue(name, out var value) ? value : Log.Logger;
    }

    private ILogger ConfigureLogger(string name)
    {
        var dynamicLoggerSection = _configuration.GetSection(ConfigSection);
        var logsPath = dynamicLoggerSection.GetValue("LogsPath", "logs");

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(dynamicLoggerSection)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: $"{logsPath}/{name}.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            );

        var logger = loggerConfig.CreateLogger();

        return logger;
    }
}
