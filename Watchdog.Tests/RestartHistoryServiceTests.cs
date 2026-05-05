using Watchdog.Services;

namespace Watchdog.Tests;

public class RestartHistoryServiceTests
{
    [Fact]
    public void CanRestart_ShouldReturnTrue_WhenNoHistory()
    {
        // Arrange
        var service = new RestartHistoryService();
        var window = TimeSpan.FromMinutes(1);

        // Act
        var result = service.CanRestart("Test", 3, window);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanRestart_ShouldReturnFalse_WhenLimitReached()
    {
        // Arrange
        var service = new RestartHistoryService();
        var window = TimeSpan.FromMinutes(1);
        service.RegisterRestart("Test");
        service.RegisterRestart("Test");
        service.RegisterRestart("Test");

        // Act
        var result = service.CanRestart("Test", 3, window);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanRestart_ShouldReturnTrue_WhenLimitNotReached()
    {
        // Arrange
        var service = new RestartHistoryService();
        var window = TimeSpan.FromMinutes(1);
        service.RegisterRestart("Test");
        service.RegisterRestart("Test");

        // Act
        var result = service.CanRestart("Test", 3, window);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanRestart_ShouldReturnTrue_WhenHistoryExpired()
    {
        // Arrange
        var service = new RestartHistoryService();
        var window = TimeSpan.FromMilliseconds(100);
        service.RegisterRestart("Test");
        service.RegisterRestart("Test");
        service.RegisterRestart("Test");

        // Act
        await Task.Delay(200);
        var result = service.CanRestart("Test", 3, window);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegisterRestart_ShouldWorkForDifferentNames()
    {
        // Arrange
        var service = new RestartHistoryService();
        var window = TimeSpan.FromMinutes(1);
        service.RegisterRestart("App1");
        service.RegisterRestart("App2");

        // Act & Assert
        Assert.True(service.CanRestart("App1", 2, window));
        Assert.True(service.CanRestart("App2", 2, window));
        
        service.RegisterRestart("App1");
        Assert.False(service.CanRestart("App1", 2, window));
        Assert.True(service.CanRestart("App2", 2, window));
    }
}
