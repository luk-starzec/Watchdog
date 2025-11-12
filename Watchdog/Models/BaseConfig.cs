namespace Watchdog.Models;

public record BaseConfig
{
    public int CheckIntervalSeconds { get; set; } = 10;
    public int MaxRestartsPerWindow { get; set; } = 3;
    public int RestartWindowSeconds { get; set; } = 300;
    public int RestartAttempts { get; set; } = 3;
}
