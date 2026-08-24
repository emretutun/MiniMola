using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Common.Exceptions;

namespace MiniMola.Web.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Bu handler yalnızca Web API isteklerini JSON olarak işler.
        // Normal MVC sayfaları kendi hata sayfasını kullanmaya devam eder.
        if (!httpContext.Request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        // Kullanıcı sayfayı kapattıysa veya isteği iptal ettiyse
        // yeni bir hata cevabı üretmeye çalışmayız.
        if (exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                "İstek kullanıcı tarafından iptal edildi. Path: {Path}",
                httpContext.Request.Path);

            return true;
        }

        var descriptor = GetDescriptor(exception);

        LogException(
            exception,
            descriptor.StatusCode,
            httpContext);

        httpContext.Response.StatusCode =
            descriptor.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = descriptor.StatusCode,
            Title = descriptor.Title,
            Detail = GetSafeDetail(
                exception,
                descriptor.StatusCode),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] =
                exception.GetType().Name;
        }

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        };

        var written =
            await problemDetailsService.TryWriteAsync(context);

        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);
        }

        return true;
    }

    private string GetSafeDetail(
        Exception exception,
        int statusCode)
    {
        if (statusCode < 500
            || environment.IsDevelopment())
        {
            return exception.Message;
        }

        return "İşlem sırasında beklenmeyen bir hata oluştu.";
    }

    private void LogException(
        Exception exception,
        int statusCode,
        HttpContext httpContext)
    {
        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "İstek işlenirken hata oluştu. "
                + "Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);

            return;
        }

        logger.LogWarning(
            exception,
            "Kontrollü istek hatası. "
            + "StatusCode: {StatusCode}, Method: {Method}, "
            + "Path: {Path}, TraceId: {TraceId}",
            statusCode,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);
    }

    private static ExceptionDescriptor GetDescriptor(
        Exception exception)
    {
        return exception switch
        {
            NotFoundException => new(
                StatusCodes.Status404NotFound,
                "Kayıt bulunamadı"),

            ConflictException => new(
                StatusCodes.Status409Conflict,
                "İşlem gerçekleştirilemedi"),

            ExternalServiceException => new(
                StatusCodes.Status503ServiceUnavailable,
                "Dış servis kullanılamıyor"),

            ArgumentException => new(
                StatusCodes.Status400BadRequest,
                "Geçersiz istek"),

            UnauthorizedAccessException => new(
                StatusCodes.Status403Forbidden,
                "Bu işlem için yetkin bulunmuyor"),

            HttpRequestException => new(
                StatusCodes.Status503ServiceUnavailable,
                "Dış servis kullanılamıyor"),

            TaskCanceledException => new(
                StatusCodes.Status504GatewayTimeout,
                "Dış servis zaman aşımına uğradı"),

            _ => new(
                StatusCodes.Status500InternalServerError,
                "Beklenmeyen bir hata oluştu")
        };
    }

    private readonly record struct ExceptionDescriptor(
        int StatusCode,
        string Title);
}