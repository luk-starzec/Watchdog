using Moq;
using Serilog;
using Watchdog.Interfaces;
using Watchdog.Models;
using Watchdog.Services;

namespace Watchdog.Tests;

public class AppMonitoringServiceTests
{
    [Fact]
    public async Task MonitorAppAsync_ShouldTryRestart_WhenProcessNotRunning_AndCanRestart()
    {
        // Arrange
        var app = new AppConfig
        {
            ProcessName = "TestApp",
            ExePath = "C:\\TestApp.exe",
            CheckInterval = TimeSpan.FromSeconds(1),
            MaxRestartsPerWindow = 5,
            RestartWindow = TimeSpan.FromSeconds(60),
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var processServiceMock = new Mock<IProcessService>();

        restartHistoryMock
            .Setup(r => r.CanRestart(app.ProcessName, app.MaxRestartsPerWindow, app.RestartWindow))
            .Returns(true);

        processServiceMock.Setup(p => p.IsProcessRunning(app.ProcessName)).Returns(false);
        processServiceMock.Setup(p => p.StartProcess(app.ExePath, app.ProcessName)).Returns(true);

        app.RestartRetryWait = TimeSpan.Zero;
        var monitoring = new AppMonitoringService(restartHistoryMock.Object, processServiceMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorAppAsync(app, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Task.Delay w MonitorServiceAsync throws TaskCanceledException, ignore
        }

        // Assert
        loggerMock.Verify(l => l.Warning(
            It.Is<string>(s => s.Contains("not running - starting")),
            app.ProcessName,
            app.ExePath
        ), Times.AtLeastOnce);

        loggerMock.Verify(l => l.Information(
            It.Is<string>(s => s.Contains("started after")),
            app.ProcessName,
            It.IsAny<int>()
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorAppAsync_ShouldLogRestartLimitExceeded_WhenCannotRestart()
    {
        // Arrange
        var app = new AppConfig
        {
            ProcessName = "TestApp",
            ExePath = "C:\\TestApp.exe",
            CheckInterval = TimeSpan.FromSeconds(1),
            MaxRestartsPerWindow = 5,
            RestartWindow = TimeSpan.FromSeconds(60),
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var processServiceMock = new Mock<IProcessService>();

        restartHistoryMock
            .Setup(r => r.CanRestart(app.ProcessName, app.MaxRestartsPerWindow, app.RestartWindow))
            .Returns(false);

        app.RestartRetryWait = TimeSpan.Zero;
        var monitoring = new AppMonitoringService(restartHistoryMock.Object, processServiceMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorAppAsync(app, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // expected effect
        }

        // Assert
        loggerMock.Verify(l => l.Error(
            It.Is<string>(s => s.Contains("restart limit exceeded")),
            app.ProcessName,
            app.MaxRestartsPerWindow,
            app.RestartWindow
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorAppAsync_ShouldLogWorking_WhenProcessRunning()
    {
        // Arrange
        var app = new AppConfig
        {
            ProcessName = "TestApp",
            ExePath = "C:\\TestApp.exe",
            CheckInterval = TimeSpan.FromSeconds(1),
            MaxRestartsPerWindow = 5,
            RestartWindow = TimeSpan.FromSeconds(60),
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var processServiceMock = new Mock<IProcessService>();

        processServiceMock.Setup(p => p.IsProcessRunning(app.ProcessName)).Returns(true);

        app.RestartRetryWait = TimeSpan.Zero;
        var monitoring = new AppMonitoringService(restartHistoryMock.Object, processServiceMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorAppAsync(app, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Task.Delay ended by token
        }

        // Assert
        loggerMock.Verify(l => l.Debug(
            It.Is<string>(s => s.Contains("is working correctly")),
            app.ProcessName
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorAppAsync_ShouldTryRestartMultipleTimes_WhenStartFails()
    {
        // Arrange
        var app = new AppConfig
        {
            ProcessName = "TestApp",
            ExePath = "C:\\TestApp.exe",
            CheckInterval = TimeSpan.FromSeconds(1),
            MaxRestartsPerWindow = 5,
            RestartWindow = TimeSpan.FromSeconds(60),
            RestartAttempts = 3
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var processServiceMock = new Mock<IProcessService>();

        restartHistoryMock
            .Setup(r => r.CanRestart(app.ProcessName, app.MaxRestartsPerWindow, app.RestartWindow))
            .Returns(true);

        processServiceMock.Setup(p => p.IsProcessRunning(app.ProcessName)).Returns(false);
        processServiceMock.SetupSequence(p => p.StartProcess(app.ExePath, app.ProcessName))
            .Returns(false)
            .Returns(false)
            .Returns(true);

        app.RestartRetryWait = TimeSpan.Zero;
        var monitoring = new AppMonitoringService(restartHistoryMock.Object, processServiceMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));

        // Act
        try
        {
            await monitoring.MonitorAppAsync(app, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException) { }

        // Assert
        processServiceMock.Verify(p => p.StartProcess(app.ExePath, app.ProcessName), Times.Exactly(3));
        loggerMock.Verify(l => l.Information(
            It.Is<string>(s => s.Contains("started after")),
            app.ProcessName,
            3
        ), Times.AtLeast(1));
    }
}
