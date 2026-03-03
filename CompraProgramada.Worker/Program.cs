using CompraProgramada.Worker;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();