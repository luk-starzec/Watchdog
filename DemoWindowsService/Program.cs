using DemoWindowsService;

Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DemoWindowsService";
    })
    .ConfigureServices((context, services) =>
    {
        var baseDir = AppContext.BaseDirectory;
        var logFile = Path.Combine(baseDir, "DemoWindowsService.log");
        var failedAttemptsFile = Path.Combine(AppContext.BaseDirectory, "FailedAttempts.txt");

        void Log(string msg)
        {
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
        }

        int remaining = 0;
        if (File.Exists(failedAttemptsFile))
        {
            _ = int.TryParse(File.ReadAllText(failedAttemptsFile), out remaining);
        }

        remaining = Math.Max(0, remaining);

        if (remaining > 0)
        {
            remaining--;
            File.WriteAllText(failedAttemptsFile, remaining.ToString());

            bool timeoutSimulation = context.Configuration.GetValue<bool>("TimeoutSimulation");
            if (timeoutSimulation)
            {
                Log($"Simulated startup timeout. Remaining: {remaining}");

                Task.Delay(TimeSpan.FromSeconds(40)).Wait();

                return;
            }

            var msg = $"Simulated startup failure. Remaining: {remaining}";
            Log(msg);

            throw new Exception(msg);
        }

        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
