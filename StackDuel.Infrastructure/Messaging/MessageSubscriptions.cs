using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Announcements;
using StackDuel.Application.Commands.Games.CompleteExpiredGame;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Infrastructure.Jobs.ProblemValidation;
using StackDuel.Infrastructure.Jobs.Submissions;
using StackDuel.Infrastructure.Jobs.TestCaseGeneration;

namespace StackDuel.Infrastructure.Messaging;

internal static class MessageSubscriptions
{
    public static IReadOnlyList<MessageSubscription> All { get; } =
        [
            MessageSubscription.For<SubmissionCreatedMessage>(
                (serviceProvider, message, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<SubmissionJobProcessorService>()
                        .RunForSubmissionAsync(message.SubmissionId, cancellationToken)
            ),
            MessageSubscription.For<SubmissionJobContinuationMessage>(
                (serviceProvider, message, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<SubmissionJobProcessorService>()
                        .RunForSubmissionAsync(message.SubmissionId, cancellationToken)
            ),
            MessageSubscription.For<ProblemValidationJobContinuationMessage>(
                (serviceProvider, message, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<ProblemValidationJobProcessorService>()
                        .RunPendingForJobAsync(message.JobId, cancellationToken)
            ),
            MessageSubscription.For<TestCaseGenerationJobContinuationMessage>(
                (serviceProvider, message, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<TestCaseGenerationJobProcessorService>()
                        .RunForJobAsync(message.JobId, cancellationToken)
            ),
            MessageSubscription.For<TestCaseGenerationJobCompletedMessage>(
                (serviceProvider, _, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<ProblemValidationJobProcessorService>()
                        .RunAwaitingGenerationPassAsync(cancellationToken)
            ),
            MessageSubscription.For<GameCreatedMessage>(
                (serviceProvider, message, cancellationToken) =>
                    serviceProvider
                        .GetRequiredService<IGameAnnouncer>()
                        .AnnounceGameCreatedAsync(message.GameId, cancellationToken)
            ),
            MessageSubscription.For<GameTimeExpiredMessage>(
                HandleGameTimeExpiredAsync,
                usesDelayedExchange: true
            ),
        ];

    /// <summary>
    /// A game-expiry message can arrive before the game has actually expired, in which case the
    /// command reports NotYetExpired_Rescheduled and this re-publishes with RescheduleCount + 1
    /// rather than treating the outcome as terminal.
    /// </summary>
    private static async Task HandleGameTimeExpiredAsync(
        IServiceProvider serviceProvider,
        GameTimeExpiredMessage message,
        CancellationToken cancellationToken
    )
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CompleteExpiredGameCommand(message.GameId, message.ExpectedStartedAt, message.RescheduleCount),
            cancellationToken
        );

        if (
            result.IsSuccess
            && result.Value.Outcome == CompleteExpiredGameOutcome.NotYetExpired_Rescheduled
            && result.Value.RescheduleForUtc is { } rescheduleFor
        )
        {
            var publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            await publisher.PublishScheduledAsync(
                message with
                {
                    RescheduleCount = message.RescheduleCount + 1,
                },
                rescheduleFor,
                cancellationToken
            );
        }
    }
}