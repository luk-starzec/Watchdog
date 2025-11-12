using Watchdog.Interfaces;

namespace Watchdog.Services;

internal class RestartHistoryService : IRestartHistoryService
{
    private readonly Dictionary<string, List<DateTime>> _restartHistory = [];

    public bool CanRestart(string name, int maxRestarts, int windowSeconds)
    {
        var now = DateTime.Now;

        if (!_restartHistory.ContainsKey(name))
            _restartHistory[name] = [];

        _restartHistory[name] = _restartHistory[name].Where(r => r > now.AddSeconds(-windowSeconds)).ToList();

        return _restartHistory[name].Count < maxRestarts;
    }

    public void RegisterRestart(string name)
    {
        var now = DateTime.Now;

        if (!_restartHistory.ContainsKey(name))
            _restartHistory[name] = [];

        _restartHistory[name].Add(now);
    }
}
