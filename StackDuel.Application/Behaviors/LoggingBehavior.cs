using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace StackDuel.Application.Behaviors;

internal sealed partial class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        string messageName = typeof(TMessage).Name;
        var stopwatch = Stopwatch.StartNew();

        LogRequestStarted(messageName);

        try
        {
            TResponse response = await next(message, cancellationToken);
            stopwatch.Stop();

            if (response is IResult result)
            {
                if (result.Status == ResultStatus.Ok)
                    LogRequestSucceeded(messageName, stopwatch.ElapsedMilliseconds);
                else
                    LogRequestFailed(
                        messageName,
                        stopwatch.ElapsedMilliseconds,
                        result.Status.ToString(),
                        string.Join("; ", result.Errors)
                    );
            }
            else
            {
                LogRequestSucceeded(messageName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogRequestThrew(messageName, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{MessageName} started")]
    private partial void LogRequestStarted(string messageName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{MessageName} succeeded in {ElapsedMs}ms")]
    private partial void LogRequestSucceeded(string messageName, long elapsedMs);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "{MessageName} failed in {ElapsedMs}ms with status {Status}: {Errors}"
    )]
    private partial void LogRequestFailed(string messageName, long elapsedMs, string status, string errors);

    [LoggerMessage(Level = LogLevel.Error, Message = "{MessageName} threw after {ElapsedMs}ms")]
    private partial void LogRequestThrew(string messageName, long elapsedMs, Exception exception);
}