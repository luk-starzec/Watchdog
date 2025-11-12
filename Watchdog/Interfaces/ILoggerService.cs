using Serilog;

namespace Watchdog.Interfaces;

internal interface ILoggerService
{
    void ConfigureLoggers(string[] names);
    ILogger GetLogger(string name);
}
