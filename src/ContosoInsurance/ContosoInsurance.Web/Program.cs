using System.Security.Claims;
using ContosoInsurance.Data;
using ContosoInsurance.Common.Logging;
using ContosoInsurance.Web.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using User = ContosoInsurance.Data.Models.User;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddContosoLogging(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAntiforgery();

builder.Services.AddDbContextFactory<ContosoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ContosoDb")));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContosoDbContext>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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

app.Logger.LogInformation("ContosoInsurance.Web starting");
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("ContosoInsurance.Web stopping"));

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
