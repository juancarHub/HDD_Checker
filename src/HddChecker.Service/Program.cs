using HddChecker.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ST_Holmes";
});

builder.Services
    .AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
    .Validate(options => options.IntervalMinutes is >= 1 and <= 10_080,
        "Monitoring:IntervalMinutes debe estar entre 1 y 10080.")
    .Validate(options => options.TimeoutSeconds is >= 1 and <= 300,
        "Monitoring:TimeoutSeconds debe estar entre 1 y 300.")
    .ValidateOnStart();

builder.Services.AddHostedService<MonitoringWorker>();

await builder.Build().RunAsync();
