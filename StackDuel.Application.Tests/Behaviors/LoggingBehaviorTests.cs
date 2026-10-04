using Ardalis.Result;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using StackDuel.Application.Behaviors;

namespace StackDuel.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record FakeMessage : IMessage;

    private readonly LoggingBehavior<FakeMessage, Result> _sut = new(
        NullLogger<LoggingBehavior<FakeMessage, Result>>.Instance
    );

    [Fact]
    public async Task Handle_NextSucceeds_ReturnsNextResponse()
    {
        var expected = Result.Success();

        var response = await _sut.Handle(new FakeMessage(), (_, _) => ValueTask.FromResult(expected), CancellationToken.None);

        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Handle_NextReturnsNonSuccessResult_ReturnsThatResult()
    {
        var expected = Result.Error("boom");

        var response = await _sut.Handle(new FakeMessage(), (_, _) => ValueTask.FromResult(expected), CancellationToken.None);

        Assert.Equal(expected, response);
    }

    [Fact]
    public async Task Handle_NextThrows_PropagatesException()
    {
        var exception = new InvalidOperationException("failure");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(new FakeMessage(), (_, _) => throw exception, CancellationToken.None).AsTask()
        );
    }

    [Fact]
    public async Task Handle_PassesMessageAndCancellationTokenToNext()
    {
        var message = new FakeMessage();
        using var cts = new CancellationTokenSource();
        FakeMessage? receivedMessage = null;
        CancellationToken receivedToken = default;

        await _sut.Handle(
            message,
            (m, ct) =>
            {
                receivedMessage = m;
                receivedToken = ct;
                return ValueTask.FromResult(Result.Success());
            },
            cts.Token
        );

        Assert.Same(message, receivedMessage);
        Assert.Equal(cts.Token, receivedToken);
    }
}