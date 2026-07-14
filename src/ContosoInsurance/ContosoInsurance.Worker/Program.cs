using ContosoInsurance.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<ExportOptions>()
    .BindConfiguration(ExportOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHostedService<ClaimsExporterService>();

var host = builder.Build();

await host.RunAsync();
