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
- 🔄 Automatic restart with configurable retry limits and wait times
- 🧠 Lock-free restart history tracking with time-window constraints
- 🪵 Structured logging via Serilog
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
| └── RestartHistoryService (thread-safe restart limits)    |
| +-------------------------------------------------------+ |

[↑ Back to top](#table-of-contents)

---

## Configuration

`appsettings.json` example (uses standard .NET TimeSpan format `HH:mm:ss`):

```json
{
  "WatchdogConfig": {
    "Applications": [
      {
        "ProcessName": "notepad",
        "ExePath": "C:\\Windows\\System32\\notepad.exe",
        "CheckInterval": "00:00:10",
        "MaxRestartsPerWindow": 3,
        "RestartWindow": "00:05:00",
        "RestartRetryWait": "00:00:10"
      }
    ],
    "WindowsServices": [
      {
        "ServiceName": "DemoWindowsService",
        "CheckInterval": "00:00:10",
        "MaxRestartsPerWindow": 3,
        "RestartWindow": "00:05:00",
        "RestartRetryWait": "00:00:10",
        "RestartAttempts": 3
      }
    ]
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/watchdog.log", "rollingInterval": "Day" } }
    ]
  }
}
```
[↑ Back to top](#table-of-contents)

---

## How It Works

### Application Monitoring

1. Every `CheckInterval`, Watchdog checks if the process `ProcessName` is running.  
2. If it is not, it attempts to restart the process up to `RestartAttempts` times.
3. Between each failed attempt, it waits for `RestartRetryWait`.
4. Restart attempts are tracked by a thread-safe `RestartHistoryService`.  
5. If the number of restarts within `RestartWindow` exceeds `MaxRestartsPerWindow`, further restarts are temporarily blocked to prevent infinite loops.

### Windows Service Monitoring

1. At the start of monitoring (or after a configuration reload), the service's **Start Mode** is verified. Only services set to *Automatic* or *Auto* are monitored.
2. The current service status is read using `ServiceControllerService`.  
3. If the service is not running, Watchdog attempts to start it.  
4. It uses the same retry logic (`RestartAttempts`, `RestartRetryWait`) and window constraints as for applications.

### Default Values (if not specified in JSON)
- `CheckInterval`: `00:00:10` (10s)
- `MaxRestartsPerWindow`: `3`
- `RestartWindow`: `00:05:00` (5 min)
- `RestartRetryWait`: `00:00:10` (10s)
- `RestartAttempts`: `3`

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
3. Logs will be available at:
   ```bash
   C:\Path\To\publish\logs\watchdog.log
   ```

[↑ Back to top](#table-of-contents)

---

## Local Debugging

To debug Watchdog without installing it as a Windows Service:

1. Run it directly from Visual Studio or CLI (`dotnet run`).
2. Optionally use `DemoWindowsService` to test monitoring.
3. Observe Watchdog logs — it should detect stopped processes or services and restart them automatically.

### Testing with DemoWindowsService
The project includes a `DemoWindowsService` specifically designed to simulate failures:
- **Simulate Crash:** Create a file named `FailedAttempts.txt` in the service's executable directory and write a number (e.g., `3`). The service will throw an exception on startup that many times before finally starting correctly.
- **Simulate Timeout:** Set `"TimeoutSimulation": true` in the `appsettings.json` of `DemoWindowsService`. The service will hang for 40 seconds during startup, allowing you to test how Watchdog handles services that fail to reach the 'Running' state in time.

[↑ Back to top](#table-of-contents)

---

## Example Use Cases

- Automatically restart crashed background applications.
- Monitor and maintain stability of internal Windows Services.
- Enforce uptime policies for legacy software components.
- Integrate into internal observability or resilience tooling.

[↑ Back to top](#table-of-contents)
