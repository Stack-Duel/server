using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace StackDuel.Api.Middleware;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var logger = context
                    .RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                var telemetryClient = context.RequestServices.GetService<TelemetryClient>();

                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var ex = feature?.Error;

                context.Response.ContentType = "application/json";

                if (ex is FluentValidation.ValidationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    AppLog.UnhandledExceptionWithPath(logger, context.Request.Method, context.Request.Path, ex);
                    telemetryClient?.TrackException(ex);

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Validation failed" }));
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var exceptionToLog = ex ?? new Exception("Unknown error");
                AppLog.UnhandledExceptionWithPath(logger, context.Request.Method, context.Request.Path, exceptionToLog);
                telemetryClient?.TrackException(exceptionToLog);

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { message = "An unexpected error occurred." })
                );
            });
        });

        return app;
    }
}

public static partial class AppLog
{
    [LoggerMessage(
        EventId = LoggingEventIds.Exceptions.UnhandledException,
        Level = LogLevel.Error,
        Message = "Unhandled exception."
    )]
    public static partial void UnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LoggingEventIds.Exceptions.UnhandledExceptionWithPath,
        Level = LogLevel.Error,
        Message = "Unhandled exception for {Method} {Path}."
    )]
    public static partial void UnhandledExceptionWithPath(
        ILogger logger,
        string method,
        string path,
        Exception exception
    );
}