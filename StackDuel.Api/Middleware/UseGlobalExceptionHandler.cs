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
                var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var ex = feature?.Error;

                context.Response.ContentType = "application/json";

                var exceptionToLog = ex ?? new Exception("Unknown error");
                AppLog.UnhandledExceptionWithPath(logger, context.Request.Method, context.Request.Path, exceptionToLog);
                telemetryClient?.TrackException(exceptionToLog);

                if (ex is FluentValidation.ValidationException validationException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(
                            new { message = "Validation failed", errors = ToErrorDictionary(validationException) }
                        )
                    );
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { message = DescribeFailure(exceptionToLog, environment) })
                );
            });
        });

        return app;
    }

    private static Dictionary<string, string[]> ToErrorDictionary(FluentValidation.ValidationException exception) =>
        exception
            .Errors.GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

    private static string DescribeFailure(Exception exception, IHostEnvironment environment) =>
        environment.IsProduction()
            ? "An unexpected error occurred."
            : $"{exception.GetType().Name}: {exception.Message}";
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