using System.Security.Claims;
using Azure.Extensions.AspNetCore.DataProtection.Blobs;
using Azure.Identity;
using Azure.Storage.Blobs;
using ContosoInsurance.Data;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Common.Storage;
using ContosoInsurance.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using User = ContosoInsurance.Data.Models.User;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddContosoLogging(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

var storageAccountName = builder.Configuration["AZURE_STORAGE_ACCOUNT_NAME"];
BlobServiceClient? blobServiceClient = null;
if (!string.IsNullOrWhiteSpace(storageAccountName))
{
    blobServiceClient = new BlobServiceClient(
        new Uri($"https://{storageAccountName}.blob.core.windows.net"),
        new DefaultAzureCredential());
    builder.Services.AddSingleton(blobServiceClient);
}

// Persist Data Protection keys to Azure Blob Storage so auth cookies survive
// container restarts and remain valid across multiple replicas.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("ContosoInsurance");
if (blobServiceClient != null)
{
    var keyContainer = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
    var keyBlob = keyContainer.GetBlobClient("keys.xml");
    dataProtection.PersistKeysToAzureBlobStorage(keyBlob);
}

builder.Services.AddDbContextFactory<ContosoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContosoDb")));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContosoDbContext>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddClaimDocumentStore(builder.Configuration);

// Azure Container Apps terminates TLS at its ingress proxy and forwards requests to the
// app over plain HTTP; without this, the app thinks every request is HTTP and generates
// absolute http:// URLs (Blazor script/WebSocket, favicon), which browsers block as mixed content.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var scoringEndpoint = builder.Configuration["AppSettings:ClaimScoringEndpoint"] ?? "http://localhost:8080";
builder.Services.AddHttpClient("scoring", client =>
{
    client.BaseAddress = new Uri(EnsureTrailingSlash(scoringEndpoint));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Ensure the blob container for Data Protection keys exists before the app starts.
if (blobServiceClient != null)
{
    var keyContainer = blobServiceClient.GetBlobContainerClient("dataprotection-keys");
    await keyContainer.CreateIfNotExistsAsync();
}

app.UseForwardedHeaders();

app.Logger.LogInformation("ContosoInsurance.Web starting");
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("ContosoInsurance.Web stopping"));

if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ContosoDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await DbInitializer.InitializeAsync(
        db,
        passwordHasher,
        app.Logger,
        seedSampleData: app.Configuration.GetValue("Database:SeedSampleData", true));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();

app.MapPost("/auth/login", async Task<IResult> (
        HttpContext httpContext,
        IDbContextFactory<ContosoDbContext> dbFactory,
        IPasswordHasher<User> passwordHasher,
        ILogger<Program> logger,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken) =>
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var username = form["username"].ToString().Trim();
        var password = form["password"].ToString();
        var returnUrl = form["returnUrl"].ToString();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users
            .SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);

        var verificationResult = PasswordVerificationResult.Failed;
        if (user is not null && !string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }

        if (user is null || verificationResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {Username}", username);
            return Results.Redirect(BuildLoginErrorUrl(returnUrl));
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username)
        };

        if (!string.IsNullOrWhiteSpace(user.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });

        logger.LogInformation("Signed in {Username}", user.Username);
        return Results.Redirect(GetLocalReturnUrl(returnUrl));
    })
    .AllowAnonymous();

app.MapHealthChecks("/health")
    .AllowAnonymous();

app.MapGet("/auth/logout", async Task<IResult> (HttpContext httpContext) =>
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    })
    .RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string EnsureTrailingSlash(string endpoint)
{
    return endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";
}

static string BuildLoginErrorUrl(string? returnUrl)
{
    var url = "/login?error=1";
    if (!string.IsNullOrWhiteSpace(returnUrl))
    {
        url += "&returnUrl=" + Uri.EscapeDataString(returnUrl);
    }

    return url;
}

static string GetLocalReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl) || Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
    {
        return "/";
    }

    if (!returnUrl.StartsWith("/", StringComparison.Ordinal) ||
        returnUrl.StartsWith("//", StringComparison.Ordinal) ||
        returnUrl.StartsWith("/\\", StringComparison.Ordinal))
    {
        return "/";
    }

    return returnUrl;
}
