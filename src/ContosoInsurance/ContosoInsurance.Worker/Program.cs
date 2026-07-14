using System;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Common.Storage;
using ContosoInsurance.Data;
using ContosoInsurance.Worker;
using ContosoInsurance.Worker.HealthChecks;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ContosoDb")
    ?? throw new InvalidOperationException("Connection string 'ContosoDb' is not configured.");

builder.Services
    .AddOptions<ExportOptions>()
    .BindConfiguration(ExportOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Logging.AddContosoLogging(builder.Configuration);
builder.Services.AddClaimDocumentStore(builder.Configuration);

builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

builder.Services.AddDbContext<ContosoDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<ClaimsRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContosoDbContext>(tags: new[] { "ready" })
    .AddCheck<BlobContainerHealthCheck>("blob-storage", tags: new[] { "ready" });

builder.Services.AddHostedService<ClaimsExporterService>();

var host = builder.Build();

await host.RunAsync();
