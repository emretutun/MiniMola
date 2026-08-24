using System.Diagnostics;

namespace MiniMola.Web.Middleware;

public sealed class RequestLogScopeMiddleware(
    RequestDelegate next,
    ILogger<RequestLogScopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId =
            Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        // Başarılı veya hatalı bütün cevaplarda takip numarası bulunur.
        context.Response.Headers["X-Trace-Id"] =
            traceId;

        using var logScope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["TraceId"] = traceId,
                ["RequestMethod"] =
                    context.Request.Method,
                ["RequestPath"] =
                    context.Request.Path.Value ?? "/"
            });

        await next(context);
    }
}