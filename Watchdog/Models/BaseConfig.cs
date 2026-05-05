using System.ComponentModel.DataAnnotations;

namespace Watchdog.Models;

public record BaseConfig
{
    [Required]
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    [Range(0, 100)]
    public int MaxRestartsPerWindow { get; set; } = 3;

    [Required]
    public TimeSpan RestartWindow { get; set; } = TimeSpan.FromSeconds(300);

    [Required]
    public TimeSpan RestartRetryWait { get; set; } = TimeSpan.FromSeconds(10);

    [Range(1, 10)]
    public int RestartAttempts { get; set; } = 3;
}
