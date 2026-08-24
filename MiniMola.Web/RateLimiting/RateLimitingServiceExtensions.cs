using System.Diagnostics;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MiniMola.Web.RateLimiting;

public static class RateLimitingServiceExtensions
{
    public const string AquariumWritePolicy =
        "aquarium-write";

    public const string GameSubmitPolicy =
        "game-submit";

    public static IServiceCollection
        AddMiniMolaRateLimiting(
            this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                AquariumWritePolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder =
                                QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        }));

            options.AddPolicy(
                GameSubmitPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetPartitionKey(httpContext),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder =
                                QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        }));

            options.OnRejected =
                async (rejectionContext, cancellationToken) =>
                {
                    var httpContext =
                        rejectionContext.HttpContext;

                    var response =
                        httpContext.Response;

                    response.StatusCode =
                        StatusCodes.Status429TooManyRequests;

                    response.ContentType =
                        "application/problem+json";

                    if (rejectionContext.Lease.TryGetMetadata(
                            MetadataName.RetryAfter,
                            out var retryAfter))
                    {
                        var retryAfterSeconds =
                            Math.Max(
                                1,
                                (int)Math.Ceiling(
                                    retryAfter.TotalSeconds));

                        response.Headers.RetryAfter =
                            retryAfterSeconds.ToString();
                    }

                    var problemDetails = new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status429TooManyRequests,

                        Title = "Çok fazla istek gönderildi.",

                        Detail =
                            "Bu işlem için istek sınırına "
                            + "ulaştın. Kısa bir süre sonra "
                            + "tekrar deneyebilirsin.",

                        Instance =
                            httpContext.Request.Path
                    };

                    problemDetails.Extensions["traceId"] =
                        Activity.Current?.Id
                        ?? httpContext.TraceIdentifier;

                    await response.WriteAsJsonAsync(
                        problemDetails,
                        cancellationToken);
                };
        });

        return services;
    }

    private static string GetPartitionKey(
        HttpContext httpContext)
    {
        var identityUserId =
            httpContext.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(identityUserId))
        {
            return $"user:{identityUserId}";
        }

        var ipAddress =
            httpContext.Connection.RemoteIpAddress?
                .ToString()
            ?? "unknown";

        return $"ip:{ipAddress}";
    }
}