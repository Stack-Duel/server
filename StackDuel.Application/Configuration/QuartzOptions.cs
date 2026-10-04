namespace StackDuel.Application.Configuration;

using StackDuel.Application.Settings;

public sealed class JobScheduleOptions : IOption
{
    public static string SectionName => "Quartz";

    public GameExpirySweepJobOptions GameExpirySweepJob { get; init; } = new();

    public SubmissionCleanupJobOptions SubmissionCleanupJob { get; init; } = new();
    public SubmissionJobProcessorJobOptions SubmissionJobProcessorJob { get; init; } = new();
    public TestCaseGenerationJobProcessorJobOptions TestCaseGenerationJobProcessorJob { get; init; } = new();
    public TestCaseRefreshJobOptions TestCaseRefreshJob { get; init; } = new();
    public ProblemValidationJobProcessorJobOptions ProblemValidationJobProcessorJob { get; init; } = new();
    public AvatarCleanupJobOptions AvatarCleanupJob { get; init; } = new();
    public DailyChallengeAssignmentJobOptions DailyChallengeAssignmentJob { get; init; } = new();
}

public sealed class AvatarCleanupJobOptions
{
    public string CronExpression { get; init; } = "0 0 4 * * ?";
}

public sealed class DailyChallengeAssignmentJobOptions
{
    public string CronExpression { get; init; } = "0 0 0 * * ?";
}

public sealed class SubmissionCleanupJobOptions
{
    /// <summary>
    /// Quartz cron expression for the scheduled run. Defaults to the top of every hour.
    /// </summary>
    public string CronExpression { get; init; } = "0 0 * * * ?";
}

public sealed class SubmissionJobProcessorJobOptions
{
    /// <summary>
    /// Quartz cron expression. Defaults to every 30 seconds as a fallback for missed queue messages.
    /// </summary>
    public string CronExpression { get; init; } = "0/30 * * * * ?";
}

public sealed class TestCaseGenerationJobProcessorJobOptions
{
    /// <summary>
    /// Quartz cron expression. Pending jobs are normally picked up immediately via a published
    /// TestCaseGenerationJobContinuationMessage (see RabbitMqConsumerService/AzureServiceBusConsumerService);
    /// this sweep only exists as a backstop for a message that was lost or never published.
    /// Defaults to every 2 minutes.
    /// </summary>
    public string CronExpression { get; init; } = "0 */2 * * * ?";
}

public sealed class TestCaseRefreshJobOptions
{
    /// <summary>
    /// Quartz cron expression for the weekly re-roll of every published problem's generated
    /// hidden test case pool. Defaults to Friday 11:30pm (server local time, no explicit time
    /// zone is configured on the trigger) — late enough to be off-hours, and deliberately
    /// ahead of the weekend rather than Sunday night, so a bad regeneration surfaces with two
    /// days to fix it before Monday traffic instead of a few hours.
    /// </summary>
    public string CronExpression { get; init; } = "0 30 23 ? * FRI";
}

public sealed class ProblemValidationJobProcessorJobOptions
{
    /// <summary>
    /// Quartz cron expression. Pending/AwaitingGeneration jobs are normally advanced immediately
    /// via published ProblemValidationJobContinuationMessage/TestCaseGenerationJobCompletedMessage
    /// messages; this sweep only exists as a backstop for a message that was lost or never published.
    /// Defaults to every 2 minutes.
    /// </summary>
    public string CronExpression { get; init; } = "0 */2 * * * ?";
}

public sealed class GameExpirySweepJobOptions
{
    /// <summary>
    /// Quartz cron expression for the game-expiry sweep backstop. This only exists to catch
    /// games whose GameTimeExpiredMessage was lost or never scheduled — the normal path is
    /// timely on its own, so this is intentionally infrequent. Defaults to every 10 minutes.
    /// </summary>
    public string CronExpression { get; init; } = "0 */10 * * * ?";
}