using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class ProcessService : IProcessService
{
    private readonly IProcessLauncher _processLauncher;
    private readonly ILoggerService _loggerService;

    public ProcessService(IProcessLauncher processLauncher, ILoggerService loggerService)
    {
        _processLauncher = processLauncher;
        _loggerService = loggerService;
    }

    public virtual bool IsProcessRunning(string processName)
    {
        try
        {
            return _processLauncher.GetProcessesByName(processName).Length > 0;
        }
        catch (Exception ex)
        {
            var logger = _loggerService.GetLogger(processName);
            logger.Error(ex, "{ProcessName} checking error", processName);

            return false;
        }
    }

    public virtual bool StartProcess(string exePath, string processName)
    {
        try
        {
            _processLauncher.Start(exePath);
            return true;
        }
        catch (Exception ex)
        {
            var logger = _loggerService.GetLogger(processName);
            logger.Error(ex, "{ProcessName} starting error", processName);

            return false;
        }
    }
}
