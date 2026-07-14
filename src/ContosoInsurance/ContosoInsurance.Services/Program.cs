using System.Threading;
using ContosoInsurance.Data;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddContosoLogging(builder.Configuration);

builder.Services.AddDbContext<ContosoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContosoDb")));
builder.Services.AddScoped<ClaimScoringService>();

var app = builder.Build();

app.MapPost("/claims/{id:int}/score", async (
    int id,
    ClaimScoringService scoringService,
    CancellationToken cancellationToken) =>
{
    var result = await scoringService.ScoreClaimAsync(id, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/model-version", (ClaimScoringService scoringService) =>
    Results.Ok(new ModelVersionResult(scoringService.GetModelVersion())));

app.Run();

internal sealed record ModelVersionResult(string ModelVersion);
