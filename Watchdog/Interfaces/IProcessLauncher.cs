using System.Diagnostics;

namespace Watchdog.Interfaces;

internal interface IProcessLauncher
{
    Process[] GetProcessesByName(string name);
    void Start(string exePath);
}
