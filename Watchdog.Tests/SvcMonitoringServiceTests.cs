using Moq;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Watchdog.Interfaces;
using Watchdog.Models;
using Watchdog.Services;

namespace Watchdog.Tests;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class SvcMonitoringServiceTests
{
    [Fact]
    public async Task MonitorServiceAsync_ShouldTryRestart_WhenServiceStopped_AndCanRestart()
    {
        // Arrange
        var svc = new SvcConfig
        {
            ServiceName = "TestSvc",
            CheckIntervalSeconds = 1,
            MaxRestartsPerWindow = 5,
            RestartWindowSeconds = 60,
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var serviceControllerMock = new Mock<IServiceControllerService>();

        restartHistoryMock
            .Setup(r => r.CanRestart(svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindowSeconds))
            .Returns(true);

        serviceControllerMock
            .Setup(s => s.GetStartMode(svc.ServiceName))
            .Returns("Automatic");

        serviceControllerMock
            .Setup(s => s.GetStatus(svc.ServiceName))
            .Returns(ServiceControllerStatus.Stopped);

        serviceControllerMock
            .Setup(s => s.StartService(svc.ServiceName, It.IsAny<int>()))
            .Returns(true);

        var monitoring = new SvcMonitoringService(restartHistoryMock.Object, null, serviceControllerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorServiceAsync(svc, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // expected result of the test completion
        }

        // Assert
        loggerMock.Verify(l => l.Warning(
            It.Is<string>(s => s.Contains("not running - starting")),
            svc.ServiceName
        ), Times.AtLeastOnce);

        loggerMock.Verify(l => l.Information(
            It.Is<string>(s => s.Contains("started after")),
            svc.ServiceName,
            It.IsAny<int>()
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorServiceAsync_ShouldLogRestartLimitExceeded_WhenCannotRestart()
    {
        // Arrange
        var svc = new SvcConfig
        {
            ServiceName = "TestSvc",
            CheckIntervalSeconds = 1,
            MaxRestartsPerWindow = 1,
            RestartWindowSeconds = 60,
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var serviceControllerMock = new Mock<IServiceControllerService>();

        serviceControllerMock
            .Setup(s => s.GetStartMode(svc.ServiceName))
            .Returns("Automatic");

        restartHistoryMock
            .Setup(r => r.CanRestart(svc.ServiceName, svc.MaxRestartsPerWindow, svc.RestartWindowSeconds))
            .Returns(false);

        var monitoring = new SvcMonitoringService(restartHistoryMock.Object, null, serviceControllerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorServiceAsync(svc, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Task.Delay w MonitorServiceAsync throws TaskCanceledException, ignore
        }

        // Assert
        loggerMock.Verify(l => l.Error(
            It.Is<string>(s => s.Contains("restart limit exceeded")),
            svc.ServiceName,
            svc.MaxRestartsPerWindow,
            svc.RestartWindowSeconds
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorServiceAsync_ShouldSkip_WhenStartModeIsNotAutomatic()
    {
        // Arrange
        var svc = new SvcConfig
        {
            ServiceName = "TestSvc",
            CheckIntervalSeconds = 1,
            MaxRestartsPerWindow = 5,
            RestartWindowSeconds = 60,
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var serviceControllerMock = new Mock<IServiceControllerService>();

        serviceControllerMock.Setup(s => s.GetStartMode(svc.ServiceName)).Returns("Manual");

        var monitoring = new SvcMonitoringService(restartHistoryMock.Object, null, serviceControllerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorServiceAsync(svc, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException) { }

        // Assert
        loggerMock.Verify(l => l.Warning(
            It.Is<string>(s => s.Contains("skipped (mode")),
            svc.ServiceName,
            "Manual"
        ), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MonitorServiceAsync_ShouldLogWorking_WhenServiceRunning()
    {
        // Arrange
        var svc = new SvcConfig
        {
            ServiceName = "TestSvc",
            CheckIntervalSeconds = 1,
            MaxRestartsPerWindow = 5,
            RestartWindowSeconds = 60,
            RestartAttempts = 1
        };

        var loggerMock = new Mock<ILogger>();
        var restartHistoryMock = new Mock<IRestartHistoryService>();
        var serviceControllerMock = new Mock<IServiceControllerService>();

        serviceControllerMock.Setup(s => s.GetStartMode(svc.ServiceName)).Returns("Automatic");
        serviceControllerMock.Setup(s => s.GetStatus(svc.ServiceName)).Returns(ServiceControllerStatus.Running);

        var monitoring = new SvcMonitoringService(restartHistoryMock.Object, null, serviceControllerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        try
        {
            await monitoring.MonitorServiceAsync(svc, loggerMock.Object, cts.Token);
        }
        catch (TaskCanceledException) { }

        // Assert
        loggerMock.Verify(l => l.Debug(
            It.Is<string>(s => s.Contains("is working correctly")),
            svc.ServiceName
        ), Times.AtLeastOnce);
    }
}
