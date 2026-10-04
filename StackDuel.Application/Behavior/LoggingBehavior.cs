using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace StackDuel.Application.Behaviors;

/// <summary>
/// Logs every command/query that flows through MediatR: when it started, how long it took,
/// and whether it succeeded or failed. This exists so the Application layer (where the actual
/// business logic lives) produces log output without every handler needing its own ILogger.
///
/// Uses Result's own success/failure state (Ardalis.Result) rather than exceptions, since
/// handlers in this codebase return failures as Result.Error/Invalid/NotFound rather than throwing.
/// </summary>
internal sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        string requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        LogRequestStarted(requestName);

        try
        {
            TResponse response = await next(cancellationToken);
            stopwatch.Stop();

            if (response is IResult result)
            {
                if (result.Status == ResultStatus.Ok)
                {
                    LogRequestSucceeded(requestName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    LogRequestFailed(
                        requestName,
                        stopwatch.ElapsedMilliseconds,
                        result.Status.ToString(),
                        string.Join("; ", result.Errors)
                    );
                }
            }
            else
            {
                LogRequestSucceeded(requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Rethrown as-is — this only adds visibility, it never changes handler behavior.
            // The global exception handler still owns turning this into an HTTP response.
            LogRequestThrew(requestName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} started")]
    private partial void LogRequestStarted(string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{RequestName} succeeded in {ElapsedMs}ms")]
    private partial void LogRequestSucceeded(string requestName, long elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "{RequestName} failed in {ElapsedMs}ms with status {Status}: {Errors}"
    )]
    private partial void LogRequestFailed(string requestName, long elapsedMs, string status, string errors);

    [LoggerMessage(Level = LogLevel.Error, Message = "{RequestName} threw after {ElapsedMs}ms")]
    private partial void LogRequestThrew(string requestName, long elapsedMs, Exception exception);
}