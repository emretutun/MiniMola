using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using MiniMola.Infrastructure;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Infrastructure.Spotify;
using MiniMola.Web.ErrorHandling;
using MiniMola.Web.Middleware;
using FluentValidation;
using MiniMola.Application.Aquariums;
using MiniMola.Application.Aquariums.Validators;
using MiniMola.Web.RateLimiting;
using Hangfire;
using Hangfire.SqlServer;
using MiniMola.Application.WordGames;
using MiniMola.Application.WorkSchedules;
using MiniMola.Application.WorkSchedules.Validators;
using MiniMola.Application.Markets;
using Microsoft.AspNetCore.Identity.UI.Services;
using MiniMola.Infrastructure.Email;
using MiniMola.Web.Services;



var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantısı
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Infrastructure servisleri: EF Core ve SQL Server
builder.Services.AddInfrastructure(connectionString);

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(
        SmtpOptions.SectionName));

builder.Services.AddTransient<
    IEmailSender,
    IdentityEmailSender>();

builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(
            CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            connectionString,
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout =
                    TimeSpan.FromMinutes(5),

                SlidingInvisibilityTimeout =
                    TimeSpan.FromMinutes(5),

                QueuePollInterval =
                    TimeSpan.FromSeconds(15),

                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                PrepareSchemaIfNecessary = true
            }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.ServerName =
        $"MiniMola-{Environment.MachineName}";
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Şimdilik e-posta doğrulama sistemi kurmadığımız için false.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Spotify yapılandırması
var spotifyClientId =
    builder.Configuration["Spotify:ClientId"]
    ?? throw new InvalidOperationException(
        "Spotify Client ID, User Secrets içinde bulunamadı.");

var spotifyClientSecret =
    builder.Configuration["Spotify:ClientSecret"]
    ?? throw new InvalidOperationException(
        "Spotify Client Secret, User Secrets içinde bulunamadı.");

builder.Services.Configure<SpotifyOptions>(
    builder.Configuration.GetSection("Spotify"));

builder.Services
    .AddAuthentication()
    .AddOAuth("Spotify", options =>
    {
        options.SignInScheme =
            IdentityConstants.ExternalScheme;

        options.ClientId = spotifyClientId;
        options.ClientSecret = spotifyClientSecret;

        options.CallbackPath = "/signin-spotify";

        options.AuthorizationEndpoint =
            "https://accounts.spotify.com/authorize";

        options.TokenEndpoint =
            "https://accounts.spotify.com/api/token";

        options.UserInformationEndpoint =
            "https://api.spotify.com/v1/me";

        options.SaveTokens = true;
        options.UsePkce = true;

        options.Scope.Clear();
        options.Scope.Add("user-read-private");
        options.Scope.Add("user-read-playback-state");
        options.Scope.Add("user-read-currently-playing");
        options.Scope.Add("playlist-read-private");
        options.Scope.Add("user-read-playback-position");
        options.Scope.Add("streaming");
        options.Scope.Add("user-read-email");
        options.Scope.Add("user-modify-playback-state");

        options.ClaimActions.MapJsonKey(
            ClaimTypes.NameIdentifier,
            "account_id");

        options.ClaimActions.MapJsonKey(
            ClaimTypes.Name,
            "display_name");

        options.ClaimActions.MapJsonKey(
            "urn:spotify:user-id",
            "id");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        context.Options.UserInformationEndpoint);

                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        context.AccessToken
                        ?? throw new InvalidOperationException(
                            "Spotify erişim anahtarı alınamadı."));

                using var response =
                    await context.Backchannel.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        context.HttpContext.RequestAborted);

                response.EnsureSuccessStatusCode();

                var json =
                    await response.Content.ReadAsStringAsync(
                        context.HttpContext.RequestAborted);

                using var userDocument =
                    JsonDocument.Parse(json);

                context.RunClaimActions(
                    userDocument.RootElement);
            }
        };
    });

// Veri koruma
builder.Services.AddDataProtection();

// CSRF koruması
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// MVC ve API
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<
    IValidator<UpdateFishNicknameRequest>,
    UpdateFishNicknameRequestValidator>();
// Standart API hata cevapları
builder.Services.AddProblemDetails();

// Global API exception handler
builder.Services
    .AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddMiniMolaRateLimiting();

builder.Services.AddScoped<
    IValidator<UpdateWorkScheduleRequest>,
    UpdateWorkScheduleRequestValidator>();

var app = builder.Build();

var recurringJobManager =
    app.Services.GetRequiredService<
        IRecurringJobManager>();

var backgroundJobClient =
    app.Services.GetRequiredService<
        IBackgroundJobClient>();

recurringJobManager.AddOrUpdate<
    IDailyWordGameService>(
        "prepare-daily-word-puzzle",
        service =>
            service.EnsureTodayPuzzleAsync(
                CancellationToken.None),
        "5 0 * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local
        });

recurringJobManager.AddOrUpdate<
    IMarketPriceRefreshService>(
        "refresh-market-prices",
        service =>
            service.RefreshTrackedAssetsAsync(
                CancellationToken.None),
        "*/5 * * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

recurringJobManager.AddOrUpdate<
    IMarketAssetCatalogSyncService>(
        "sync-market-asset-catalogs",
        service =>
            service.SyncCatalogsAsync(
                CancellationToken.None),
        "20 3 * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

recurringJobManager.AddOrUpdate<
    IFundEstimateService>(
        "evaluate-fund-estimates",
        service =>
            service.EvaluatePendingAsync(
                CancellationToken.None),
        "15 * * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

// The service caches successful checks for six hours and retries failures after 30 minutes.
recurringJobManager.AddOrUpdate<IFundPortfolioService>(
    "discover-fund-portfolio-reports",
    service => service.RefreshReportsAsync(CancellationToken.None),
    "*/30 * * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

// UTC 15:00-16:59; the service narrows execution to 18:45-19:30 Turkey time.
recurringJobManager.AddOrUpdate<IFundEstimateService>(
    "capture-fund-closing-estimates",
    service => service.CaptureClosingEstimatesAsync(CancellationToken.None),
    "*/10 15-16 * * 1-5",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

backgroundJobClient.Enqueue<
    IMarketAssetCatalogSyncService>(
        service =>
            service.SyncCatalogsAsync(
                CancellationToken.None));

app.UseMiddleware<RequestLogScopeMiddleware>();

// Yalnızca /api istekleri için JSON hata yönetimi
app.UseWhen(
    context =>
        context.Request.Path.StartsWithSegments("/api"),
    apiApplication =>
    {
        apiApplication.UseExceptionHandler();
        apiApplication.UseStatusCodePages();
    });

// HTTP istek hattı
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    // Normal MVC sayfaları için kullanıcı dostu hata sayfası
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();

// Authentication'dan sonra çalışmalı;
// böylece sayaç kullanıcı kimliğine göre tutulabilir.
app.UseRateLimiter();

app.UseMiddleware<UserProfileProvisioningMiddleware>();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();


app.Run();
