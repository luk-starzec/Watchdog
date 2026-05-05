using System.Collections.Concurrent;
using System.Collections.Immutable;
using Watchdog.Interfaces;

namespace Watchdog.Services;

/// <summary>
/// Service tracking the history of restarts to enforce limits within a sliding time window.
/// </summary>
internal class RestartHistoryService : IRestartHistoryService
{
    private readonly ConcurrentDictionary<string, ImmutableList<DateTime>> _restartHistory = new();

    public bool CanRestart(string name, int maxRestarts, TimeSpan window)
    {
        var now = DateTime.Now;
        var threshold = now.Subtract(window);

        // We use AddOrUpdate here even though it's a "check" operation (read-only intent) because:
        // 1. Lazy Cleanup: We need to remove expired entries (older than the window) to get an accurate count.
        // 2. Atomicity: AddOrUpdate ensures that even if multiple threads check/update the same key, we don't lose any data and the cleanup is performed safely.
        var currentHistory = _restartHistory.AddOrUpdate(
            name,
            _ => [],
            (_, existing) => existing.RemoveAll(r => r <= threshold)
        );

        return currentHistory.Count < maxRestarts;
    }

    public void RegisterRestart(string name)
    {
        var now = DateTime.Now;

        _restartHistory.AddOrUpdate(
            name,
            _ => [now],
            (_, existing) => existing.Add(now)
        );
    }
}
