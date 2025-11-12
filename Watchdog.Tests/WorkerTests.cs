using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using Watchdog.Interfaces;
using Watchdog.Models;

namespace Watchdog.Tests;

public class WorkerTests
{
    private readonly Mock<IAppMonitoringService> _mockAppMonitoringService;
    private readonly Mock<ISvcMonitoringService> _mockSvcMonitoringService;
    private readonly Mock<ILoggerService> _mockLoggerService;

    public WorkerTests()
    {
        _mockAppMonitoringService = new Mock<IAppMonitoringService>();
        _mockSvcMonitoringService = new Mock<ISvcMonitoringService>();
        _mockLoggerService = new Mock<ILoggerService>();
    }

    private IConfiguration BuildTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            ["WatchdogConfig:Applications:0:ProcessName"] = "TestApp",
            ["WatchdogConfig:Applications:0:ExePath"] = "C:\\TestApp.exe",
            ["WatchdogConfig:Applications:0:CheckIntervalSeconds"] = "1",
            ["WatchdogConfig:Applications:0:MaxRestartsPerWindow"] = "3",
            ["WatchdogConfig:Applications:0:RestartWindowSeconds"] = "60",
            ["WatchdogConfig:Applications:0:RestartAttempts"] = "2",

            ["WatchdogConfig:WindowsServices:0:ServiceName"] = "TestService",
            ["WatchdogConfig:WindowsServices:0:CheckIntervalSeconds"] = "1",
            ["WatchdogConfig:WindowsServices:0:MaxRestartsPerWindow"] = "3",
            ["WatchdogConfig:WindowsServices:0:RestartWindowSeconds"] = "60",
            ["WatchdogConfig:WindowsServices:0:RestartAttempts"] = "2",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task Worker_StartsMonitoringForAppsAndServices_AndPassesLoggerAndToken()
    {
        // Arrange
        var config = BuildTestConfiguration();

        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(config, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act: start worker in a separate task
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        // Wait a bit to let tasks start
        await Task.Delay(500);

        // Stop the worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert: Verify that logger service was configured
        _mockLoggerService.Verify(ls => ls.ConfigureLoggers(It.Is<string[]>(arr =>
            arr.Contains("TestApp") && arr.Contains("TestService"))), Times.AtLeastOnce);

        // Verify that monitoring services were called with correct logger
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.Is<AppConfig>(a => a.ProcessName == "TestApp"),
            mockLoggerApp.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockSvcMonitoringService.Verify(m => m.MonitorServiceAsync(
            It.Is<SvcConfig>(s => s.ServiceName == "TestService"),
            mockLoggerSvc.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_RestartsMonitoringOnConfigurationChange()
    {
        // Arrange
        var config = BuildTestConfiguration();

        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(config, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        // Simulate configuration reload
        (config as IConfigurationRoot)?.Reload();

        await Task.Delay(500);

        // Stop the worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert: Verify monitoring was started at least once after reload
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockSvcMonitoringService.Verify(m => m.MonitorServiceAsync(
            It.IsAny<SvcConfig>(),
            mockLoggerSvc.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_StopsMonitoring_WhenCancelled()
    {
        // Arrange
        var config = BuildTestConfiguration();
        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(config, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // Cancel and wait
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert: Verify monitoring tasks were called at least once
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockSvcMonitoringService.Verify(m => m.MonitorServiceAsync(
            It.IsAny<SvcConfig>(),
            mockLoggerSvc.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_RestartsMonitoring_OnMultipleConfigurationReloads()
    {
        // Arrange
        var config = BuildTestConfiguration();

        var mockLoggerApp1 = new Mock<ILogger>();
        var mockLoggerSvc1 = new Mock<ILogger>();
        var mockLoggerApp2 = new Mock<ILogger>();
        var mockLoggerSvc2 = new Mock<ILogger>();

        // first configuration
        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp1.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc1.Object);

        var worker = new Worker(config, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act: start worker
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // simulate configuration change: change mock loggers
        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp2.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc2.Object);

        // Trigger reload
        (config as IConfigurationRoot)?.Reload();
        await Task.Delay(500);

        // Stop worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert: monitoring was run for the old and new configurations
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp1.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp2.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockSvcMonitoringService.Verify(m => m.MonitorServiceAsync(
            It.IsAny<SvcConfig>(),
            mockLoggerSvc1.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _mockSvcMonitoringService.Verify(m => m.MonitorServiceAsync(
            It.IsAny<SvcConfig>(),
            mockLoggerSvc2.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // Assert: ConfigureLoggers was called multiple times
        _mockLoggerService.Verify(ls => ls.ConfigureLoggers(It.IsAny<string[]>()), Times.AtLeast(2));
    }
}