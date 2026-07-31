using System.Security.Claims;
using MiniMola.Application.Abstractions;

namespace MiniMola.Web.Middleware;

public sealed class UserProfileProvisioningMiddleware(
    RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserProfileService userProfileService)
    {
        var isAuthenticated =
            context.User.Identity?.IsAuthenticated == true;

        var isStaticAsset = System.IO.Path.HasExtension(
            context.Request.Path.Value);

        if (isAuthenticated && !isStaticAsset)
        {
            var identityUserId = context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(identityUserId))
            {
                var userName =
                    context.User.Identity?.Name
                    ?? "MiniMola Kullanıcısı";

                var atIndex = userName.IndexOf('@');

                var displayName = atIndex > 0
                    ? userName[..atIndex]
                    : userName;

                await userProfileService.EnsureUserProfileAsync(
                    identityUserId,
                    displayName);
            }
        }

        await next(context);
    }
}