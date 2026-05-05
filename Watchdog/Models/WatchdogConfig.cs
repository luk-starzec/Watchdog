namespace Watchdog.Models;

public record WatchdogConfig
{
    public List<AppConfig> Applications { get; set; } = [];
    public List<SvcConfig> WindowsServices { get; set; } = [];
}
