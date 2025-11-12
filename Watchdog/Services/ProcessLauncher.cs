using System.Diagnostics;
using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class ProcessLauncher : IProcessLauncher
{
    public Process[] GetProcessesByName(string name)
    {
        return Process.GetProcessesByName(name);
    }

    public void Start(string exePath)
    {
        Process.Start(exePath);
    }
}
