using DemoWindowsService;

Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DemoWindowsService";
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();