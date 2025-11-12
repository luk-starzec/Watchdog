namespace Watchdog.Models;

public record AppConfig : BaseConfig
{
    public required string ProcessName { get; set; }
    public required string ExePath { get; set; }
}
