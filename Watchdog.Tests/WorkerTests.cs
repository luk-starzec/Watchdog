using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task StartAsync_ShouldInitializeMonitoring_WhenConfigIsProvided()
    {
        // Arrange
        var config = CreateTestConfig();
        var options = CreateOptions(config);

        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(options, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act: start worker
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // Stop the worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockLoggerService.Verify(ls => ls.ConfigureLoggers(It.Is<string[]>(arr =>
            arr.Contains("TestApp") && arr.Contains("TestService"))), Times.AtLeastOnce);

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
    public async Task StartAsync_ShouldRestartMonitoring_WhenConfigurationChanges()
    {
        // Arrange
        var config = CreateTestConfig();
        var optionsMock = new Mock<IOptionsMonitor<WatchdogConfig>>();
        optionsMock.Setup(m => m.CurrentValue).Returns(config);

        Action<WatchdogConfig, string?>? registeredCallback = null;
        optionsMock.Setup(m => m.OnChange(It.IsAny<Action<WatchdogConfig, string?>>()))
            .Callback<Action<WatchdogConfig, string?>>(a => registeredCallback = a)
            .Returns(new Mock<IDisposable>().Object);

        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(optionsMock.Object, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // Simulate configuration reload
        registeredCallback?.Invoke(config, "name");

        await Task.Delay(500);

        // Stop the worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp.Object,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StopAsync_ShouldCancelMonitoring_WhenWorkerIsStopped()
    {
        // Arrange
        var config = CreateTestConfig();
        var options = CreateOptions(config);
        var mockLoggerApp = new Mock<ILogger>();
        var mockLoggerSvc = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp.Object);
        _mockLoggerService.Setup(ls => ls.GetLogger("TestService")).Returns(mockLoggerSvc.Object);

        var worker = new Worker(options, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // Cancel and wait
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            mockLoggerApp.Object,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnChange_ShouldRestartMonitoringMultipleTimes_WhenConfigurationChangesRepeatedly()
    {
        // Arrange
        var config = CreateTestConfig();
        var optionsMock = new Mock<IOptionsMonitor<WatchdogConfig>>();
        optionsMock.Setup(m => m.CurrentValue).Returns(config);

        Action<WatchdogConfig, string?>? registeredCallback = null;
        optionsMock.Setup(m => m.OnChange(It.IsAny<Action<WatchdogConfig, string?>>()))
            .Callback<Action<WatchdogConfig, string?>>(a => registeredCallback = a)
            .Returns(new Mock<IDisposable>().Object);

        var mockLoggerApp1 = new Mock<ILogger>();
        var mockLoggerApp2 = new Mock<ILogger>();

        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp1.Object);

        var worker = new Worker(optionsMock.Object, _mockLoggerService.Object, _mockAppMonitoringService.Object, _mockSvcMonitoringService.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = Task.Run(() => worker.StartAsync(cts.Token));

        await Task.Delay(500);

        // First reload
        _mockLoggerService.Setup(ls => ls.GetLogger("TestApp")).Returns(mockLoggerApp2.Object);
        registeredCallback?.Invoke(config, "reload1");
        await Task.Delay(500);

        // Second reload
        registeredCallback?.Invoke(config, "reload2");
        await Task.Delay(500);

        // Stop worker
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _mockAppMonitoringService.Verify(m => m.MonitorAppAsync(
            It.IsAny<AppConfig>(),
            It.IsAny<ILogger>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3)); // Initial + 2 reloads
    }

    private static IOptionsMonitor<WatchdogConfig> CreateOptions(WatchdogConfig config)
    {
        var mock = new Mock<IOptionsMonitor<WatchdogConfig>>();
        mock.Setup(m => m.CurrentValue).Returns(config);
        return mock.Object;
    }

    private static WatchdogConfig CreateTestConfig()
    {
        return new WatchdogConfig
        {
            Applications =
            [
                new AppConfig
                {
                    ProcessName = "TestApp",
                    ExePath = "C:\\TestApp.exe",
                    CheckInterval = TimeSpan.FromSeconds(1),
                    MaxRestartsPerWindow = 3,
                    RestartWindow = TimeSpan.FromSeconds(60),
                    RestartAttempts = 2
                }
            ],
            WindowsServices =
            [
                new SvcConfig
                {
                    ServiceName = "TestService",
                    CheckInterval = TimeSpan.FromSeconds(1),
                    MaxRestartsPerWindow = 3,
                    RestartWindow = TimeSpan.FromSeconds(60),
                    RestartAttempts = 2
                }
            ]
        };
    }

}