namespace Watchdog.Interfaces;

internal interface IProcessService
{
    bool IsProcessRunning(string processName);
    bool StartProcess(string exePath, string processName);
}
