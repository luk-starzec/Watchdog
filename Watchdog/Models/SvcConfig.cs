namespace Watchdog.Models;

public record SvcConfig : BaseConfig
{
    public required string ServiceName { get; set; }
}
