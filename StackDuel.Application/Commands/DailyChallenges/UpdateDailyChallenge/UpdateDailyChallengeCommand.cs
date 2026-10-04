using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.DailyChallenges.UpdateDailyChallenge;

internal sealed record UpdateDailyChallengeCommand(DateOnly Date, Guid ProblemId) : ICommand;