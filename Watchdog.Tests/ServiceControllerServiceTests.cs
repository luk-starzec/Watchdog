using Moq;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using Watchdog.Interfaces;
using Watchdog.Services;

namespace Watchdog.Tests;

[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class ServiceControllerServiceTests
{
    #region GetStatus Tests

    [Fact]
    public void GetStatus_ShouldReturnRunning_WhenServiceIsRunning()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var controller = new Mock<IServiceControllerWrapper>();
        controller.Setup(c => c.Status).Returns(ServiceControllerStatus.Running);

        var factory = new Mock<IServiceControllerFactory>();
        factory.Setup(f => f.Create("MySvc")).Returns(controller.Object);

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();

        var service = new ServiceControllerService(factory.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var status = service.GetStatus("MySvc");

        // Assert
        Assert.Equal(ServiceControllerStatus.Running, status);
    }

    [Fact]
    public void GetStatus_ShouldReturnStopped_AndLogError_OnException()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var factory = new Mock<IServiceControllerFactory>();
        factory.Setup(f => f.Create("MySvc")).Throws(new Exception("Access denied"));

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();

        var service = new ServiceControllerService(factory.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var status = service.GetStatus("MySvc");

        // Assert
        Assert.Equal(ServiceControllerStatus.Stopped, status);
        logger.Verify(l => l.Error(It.IsAny<Exception>(), "{ServiceName} getting status error", "MySvc"), Times.Once);
    }

    #endregion

    #region StartService Tests

    [Fact]
    public void StartService_ShouldReturnTrue_WhenServiceStartsSuccessfully()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();

        var controller = new Mock<IServiceControllerWrapper>();
        controller.Setup(c => c.Start());
        controller.Setup(c => c.WaitForStatus(ServiceControllerStatus.Running, It.IsAny<TimeSpan>()));
        controller.Setup(c => c.Status).Returns(ServiceControllerStatus.Running);

        var factory = new Mock<IServiceControllerFactory>();
        factory.Setup(f => f.Create("MySvc")).Returns(controller.Object);

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();

        var service = new ServiceControllerService(factory.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.StartService("MySvc", 5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StartService_ShouldReturnFalse_AndLogTimeout_WhenServiceDoesNotReachRunning()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var controller = new Mock<IServiceControllerWrapper>();
        controller.Setup(c => c.Start());
        controller.Setup(c => c.WaitForStatus(ServiceControllerStatus.Running, It.IsAny<TimeSpan>()));
        controller.Setup(c => c.Status).Returns(ServiceControllerStatus.Stopped);

        var factory = new Mock<IServiceControllerFactory>();
        factory.Setup(f => f.Create("MySvc")).Returns(controller.Object);

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();

        var service = new ServiceControllerService(factory.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.StartService("MySvc", 5);

        // Assert
        Assert.False(result);
        logger.Verify(l => l.Error("{ServiceName} starting timeout", "MySvc"), Times.Once);
    }

    [Fact]
    public void StartService_ShouldReturnFalse_AndLogError_OnException()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var factory = new Mock<IServiceControllerFactory>();
        factory.Setup(f => f.Create("MySvc")).Throws(new Exception("Access denied"));

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();

        var service = new ServiceControllerService(factory.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.StartService("MySvc", 5);

        // Assert
        Assert.False(result);
        logger.Verify(l => l.Error(It.IsAny<Exception>(), "{ServiceName} starting error", "MySvc"), Times.Once);
    }

    #endregion

    #region GetStartMode Tests

    [Fact]
    public void GetStartMode_ShouldReturnAutomatic_WhenServiceIsAutomatic()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var managementObject = new Mock<IManagementObject>();
        managementObject.Setup(m => m["StartMode"]).Returns("Automatic");

        var searcher = new Mock<IManagementObjectSearcher>();
        searcher.Setup(s => s.Get()).Returns(new List<IManagementObject> { managementObject.Object });

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();
        searcherFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(searcher.Object);

        var factoryController = new Mock<IServiceControllerFactory>();

        var service = new ServiceControllerService(factoryController.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.GetStartMode("MySvc");

        // Assert
        Assert.Equal("Automatic", result);
    }

    [Fact]
    public void GetStartMode_ShouldReturnUnknown_WhenNoObjectsReturned()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var searcher = new Mock<IManagementObjectSearcher>();
        searcher.Setup(s => s.Get()).Returns(new List<IManagementObject>());

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();
        searcherFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(searcher.Object);

        var factoryController = new Mock<IServiceControllerFactory>();
        var service = new ServiceControllerService(factoryController.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.GetStartMode("MySvc");

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void GetStartMode_ShouldReturnUnknown_AndLogError_OnException()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();
        searcherFactory.Setup(f => f.Create(It.IsAny<string>())).Throws(new Exception("WMI error"));

        var factoryController = new Mock<IServiceControllerFactory>();
        var service = new ServiceControllerService(factoryController.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.GetStartMode("MySvc");

        // Assert
        Assert.Equal("Unknown", result);
        logger.Verify(l => l.Error(It.IsAny<Exception>(), "{ServiceName} getting start mode error", "MySvc"), Times.Once);
    }

    [Fact]
    public void GetStartMode_ShouldReturnUnknown_WhenStartModeIsNull()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var loggerService = new Mock<ILoggerService>();
        loggerService.Setup(l => l.GetLogger("MySvc")).Returns(logger.Object);

        var managementObject = new Mock<IManagementObject>();
        managementObject.Setup(m => m["StartMode"]).Returns((object?)null);

        var searcher = new Mock<IManagementObjectSearcher>();
        searcher.Setup(s => s.Get()).Returns(new List<IManagementObject> { managementObject.Object });

        var searcherFactory = new Mock<IManagementObjectSearcherFactory>();
        searcherFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(searcher.Object);

        var factoryController = new Mock<IServiceControllerFactory>();
        var service = new ServiceControllerService(factoryController.Object, searcherFactory.Object, loggerService.Object);

        // Act
        var result = service.GetStartMode("MySvc");

        // Assert
        Assert.Equal("Unknown", result);
    }

    #endregion
}
