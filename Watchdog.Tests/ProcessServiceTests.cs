using Moq;
using Serilog;
using Watchdog.Interfaces;
using Watchdog.Services;

namespace Watchdog.Tests;

public class ProcessServiceTests
{
    #region IsProcessRunning Tests

    [Fact]
    public void IsProcessRunning_ShouldReturnTrue_WhenProcessExists()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        launcher.Setup(p => p.GetProcessesByName("MyApp")).Returns(new[] { new System.Diagnostics.Process() });

        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MyApp")).Returns(logger.Object);

        var service = new ProcessService(launcher.Object, loggerService.Object);

        // Act
        var result = service.IsProcessRunning("MyApp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsProcessRunning_ShouldReturnFalse_WhenNoProcessesFound()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        launcher.Setup(p => p.GetProcessesByName("MyApp")).Returns(Array.Empty<System.Diagnostics.Process>());

        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MyApp")).Returns(logger.Object);

        var service = new ProcessService(launcher.Object, loggerService.Object);

        // Act
        var result = service.IsProcessRunning("MyApp");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsProcessRunning_ShouldReturnFalse_AndLogError_OnException()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        launcher.Setup(p => p.GetProcessesByName("MyApp")).Throws(new Exception("Test exception"));

        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MyApp")).Returns(logger.Object);

        var service = new ProcessService(launcher.Object, loggerService.Object);

        // Act
        var result = service.IsProcessRunning("MyApp");

        // Assert
        Assert.False(result);
        logger.Verify(l => l.Error(It.IsAny<Exception>(), "{ProcessName} checking error", "MyApp"), Times.Once);
    }

    #endregion

    #region StartProcess Tests

    [Fact]
    public void StartProcess_ShouldReturnTrue_WhenProcessStartsSuccessfully()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        launcher.Setup(p => p.Start("app.exe"));

        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MyApp")).Returns(logger.Object);

        var service = new ProcessService(launcher.Object, loggerService.Object);

        // Act
        var result = service.StartProcess("app.exe", "MyApp");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StartProcess_ShouldReturnFalse_AndLogError_OnException()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        launcher.Setup(p => p.Start("bad.exe")).Throws(new Exception("Test start exception"));

        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("BadApp")).Returns(logger.Object);

        var service = new ProcessService(launcher.Object, loggerService.Object);

        // Act
        var result = service.StartProcess("bad.exe", "BadApp");

        // Assert
        Assert.False(result);
        logger.Verify(l => l.Error(It.IsAny<Exception>(), "{ProcessName} starting error", "BadApp"), Times.Once);
    }

    #endregion
}