namespace Watchdog.Interfaces;

public interface IRestartHistoryService
{
    bool CanRestart(string name, int maxRestarts, TimeSpan window);
    void RegisterRestart(string name);
}