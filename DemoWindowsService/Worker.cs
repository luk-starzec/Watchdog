namespace DemoWindowsService;

public class Worker : BackgroundService
{
    private readonly string _logFile;

    public Worker()
    {
        _logFile = Path.Combine(AppContext.BaseDirectory, "DemoWindowsService.log");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogToFile("Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            LogToFile($"Working... {DateTime.Now:HH:mm:ss}");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private void LogToFile(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(_logFile, line + Environment.NewLine);
    }
}