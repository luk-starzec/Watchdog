namespace DemoWindowsService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _logFile;
    private readonly string _failedAttemptsFile;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _logFile = Path.Combine(AppContext.BaseDirectory, "DemoWindowsService.log");
        _failedAttemptsFile = Path.Combine(AppContext.BaseDirectory, "FailedAttempts.txt");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            int remainingFailedAttempts = 0;

            if (File.Exists(_failedAttemptsFile))
            {
                var text = File.ReadAllText(_failedAttemptsFile);
                _ = int.TryParse(text, out remainingFailedAttempts);
            }

            remainingFailedAttempts = Math.Max(0, remainingFailedAttempts - 1);
            File.WriteAllText(_failedAttemptsFile, remainingFailedAttempts.ToString());

            if (remainingFailedAttempts > 0)
            {
                //var message = $"Simulated start failure, remaining failed attempts: {remainingFailedAttempts}";
                //LogToFile(message);

                //throw new Exception(message);

                var message = $"Simulated start failure by delay, remaining failed attempts: {remainingFailedAttempts}";
                LogToFile(message);

                await Task.Delay(TimeSpan.FromSeconds(40), stoppingToken);
                return;
            }

            _logger.LogInformation("DemoWindowsService started.");
            LogToFile("Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Working at: {time}", DateTimeOffset.Now);
                LogToFile($"Working... {DateTime.Now:HH:mm:ss}");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // normal service stop
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DemoWindowsService failed during startup");
            throw;
        }
        finally
        {
            LogToFile("Service stopping...");
            _logger.LogInformation("DemoWindowsService stopping.");
        }
    }
    private void LogToFile(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(_logFile, line + Environment.NewLine);
    }
}