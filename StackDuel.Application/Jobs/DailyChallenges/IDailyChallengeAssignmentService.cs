namespace StackDuel.Application.Jobs.DailyChallenges;

public interface IDailyChallengeAssignmentService
{
    Task AssignUpcomingChallengesAsync(CancellationToken cancellationToken = default);
}