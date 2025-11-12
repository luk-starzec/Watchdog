# Watchdog

**Watchdog** is a lightweight Windows monitoring service that automatically supervises both running applications and Windows Services.  
It restarts processes or services when they stop unexpectedly and enforces restart limits within a configurable time window.

# Table of Contents

- [Features](#features)
- [Architecture Overview](#architecture-overview)
- [Configuration](#configuration)
- [How It Works](#how-it-works)
- [Installation as a Windows Service](#installation-as-a-windows-service)
- [Local Debugging](#local-debugging)
- [Example Use Cases](#example-use-cases)

---

## Features

- 🔍 Monitors running applications (EXE processes)
- ⚙️ Monitors Windows Services via ServiceController
- 🔄 Automatic restart with configurable retry limits
- 🧠 Restart history tracking with time-window constraints
- 🪵 Structured logging via Serilog (or any ILogger)
- 🔁 Automatic configuration reload on change
- 💨 Minimal CPU and memory footprint

[↑ Back to top](#table-of-contents)

---

## Architecture Overview

| Watchdog                                                  |
| --------------------------------------------------------- |
| Worker (BackgroundService)                                |
| ├── AppMonitoringService (process monitoring)             |
| ├── SvcMonitoringService (service monitoring)             |
| ├── ProcessService (process control)                      |
| ├── ServiceControllerService (service control)            |
| └── RestartHistoryService (restart limits)                |
| +-------------------------------------------------------+ |

[↑ Back to top](#table-of-contents)

---

## Configuration

`appsettings.json` example:

```json
{
  "WatchdogConfig": {
    "Applications": [
      {
        "ProcessName": "DemoApp",
        "ExePath": "C:\\Tools\\DemoApp.exe",
        "CheckIntervalSeconds": 10,
        "MaxRestartsPerWindow": 3,
        "RestartWindowSeconds": 300,
        "RestartAttempts": 2
      }
    ],
    "WindowsServices": [
      {
        "ServiceName": "DemoWindowsService",
        "CheckIntervalSeconds": 10,
        "MaxRestartsPerWindow": 3,
        "RestartWindowSeconds": 300,
        "RestartAttempts": 2
      }
    ]
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "Logs/watchdog.log", "rollingInterval": "Day" } }
    ]
  },
  "DynamicLoggerBase": {
    "Serilog": {
      "MinimumLevel": "Debug",
      "WriteTo": [
        { "Name": "Console" }
      ]
    }
  }
}
```
[↑ Back to top](#table-of-contents)

---

## How It Works

### Application Monitoring

1. Every `CheckIntervalSeconds`, Watchdog checks if the process `ProcessName` is running.  
2. If it is not, it attempts to restart the process using `ProcessService`.  
3. Restart attempts are tracked by `RestartHistoryService`.  
4. If the number of restarts within `RestartWindowSeconds` exceeds `MaxRestartsPerWindow`, further restarts are temporarily blocked.

### Windows Service Monitoring

1. The current service status is read using `ServiceControllerService`.  
2. If the service is not running and is configured as *Automatic*, Watchdog attempts to start it.  
3. Restart limits are applied the same way as for applications.

[↑ Back to top](#table-of-contents)

---

## Installation as a Windows Service

1. Build and publish the application:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```
2. Create a Windows Service:
   ```bash
   sc create Watchdog binPath= "C:\Path\To\publish\Watchdog.exe"
   sc start Watchdog
   ```
3. Logs will be available at::
   ```bash
   C:\Path\To\publish\Logs\watchdog.log
   ```

[↑ Back to top](#table-of-contents)

---

## Local Debugging

To debug Watchdog without installing it as a Windows Service:

1. Run it directly from Visual Studio or CLI (`dotnet run`).
2. Optionally create a dummy service for testing (see `DemoWindowsService`).
3. Observe Watchdog logs — it should detect stopped processes or services and restart them automatically.

[↑ Back to top](#table-of-contents)

---

## Example Use Cases

- Automatically restart crashed background applications.
- Monitor and maintain stability of internal Windows Services.
- Enforce uptime policies for legacy software components.
- Integrate into internal observability or resilience tooling.

[↑ Back to top](#table-of-contents)
