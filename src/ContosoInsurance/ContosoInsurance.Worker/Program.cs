using System;
using ContosoInsurance.Data;
using ContosoInsurance.Worker;
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

builder.Services.AddDbContext<ContosoDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<ClaimsRepository>();
builder.Services.AddHostedService<ClaimsExporterService>();

var host = builder.Build();

await host.RunAsync();
