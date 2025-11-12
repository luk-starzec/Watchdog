namespace Watchdog.Interfaces;

internal interface IRestartHistoryService
{
    bool CanRestart(string name, int maxRestarts, int windowSeconds);
    void RegisterRestart(string name);
}
