using Microsoft.AspNetCore.Identity;
using MiniMola.Infrastructure;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Web.Middleware;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using MiniMola.Infrastructure.Spotify;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı bağlantısı
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Infrastructure servisleri: EF Core ve SQL Server
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        // Şimdilik e-posta doğrulama sistemi kurmadığımız için false.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
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
        options.SignInScheme = IdentityConstants.ExternalScheme;

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
                using var request = new HttpRequestMessage(
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

                using var userDocument = JsonDocument.Parse(json);

                context.RunClaimActions(userDocument.RootElement);
            }
        };
    });

builder.Services.AddDataProtection();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// HTTP istek hattı
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<UserProfileProvisioningMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

app.Run();