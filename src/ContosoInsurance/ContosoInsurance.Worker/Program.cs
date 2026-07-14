using ContosoInsurance.Common.Logging;
using ContosoInsurance.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<ExportOptions>()
    .BindConfiguration(ExportOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHostedService<ClaimsExporterService>();

var host = builder.Build();

// Wire the legacy AppLogger facade to the Generic Host logging pipeline so that
// code in ContosoInsurance.Data (e.g. ClaimsRepository) that still calls the
// static AppLogger methods routes through ILogger and appears in console output.
#pragma warning disable CS0618 // AppLogger is intentionally kept for incremental migration
AppLogger.Configure(host.Services.GetRequiredService<ILoggerFactory>());
#pragma warning restore CS0618

await host.RunAsync();
