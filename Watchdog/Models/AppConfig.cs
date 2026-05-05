using System.ComponentModel.DataAnnotations;

namespace Watchdog.Models;

public record AppConfig : BaseConfig
{
    [Required]
    public required string ProcessName { get; set; }

    [Required]
    public required string ExePath { get; set; }
}