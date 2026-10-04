using Ardalis.Result;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.DailyChallenges.Dtos;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Queries.DailyChallenges.GetUpcomingDailyChallenges;

internal sealed class GetUpcomingDailyChallengesHandler(
    IDailyChallengeRepository dailyChallengeRepository,
    IProblemReadRepository problemReadRepository
) : IQueryHandler<GetUpcomingDailyChallengesQuery, IReadOnlyList<UpcomingDailyChallengeDto>>
{
    public async Task<Result<IReadOnlyList<UpcomingDailyChallengeDto>>> Handle(
        GetUpcomingDailyChallengesQuery request,
        CancellationToken cancellationToken
    )
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyList<DailyChallenge> upcoming = await dailyChallengeRepository.GetUpcomingAsync(
            today,
            cancellationToken
        );

        if (upcoming.Count == 0)
            return Result.Success<IReadOnlyList<UpcomingDailyChallengeDto>>([]);

        Guid[] problemIds = [.. upcoming.Select(c => c.ProblemId).Distinct()];
        IReadOnlyList<AdminProblemListRowDto> rows = await problemReadRepository.FindByIdsAsync(
            problemIds,
            cancellationToken
        );
        Dictionary<Guid, AdminProblemListRowDto> rowsById = rows.ToDictionary(r => r.Id);

        IReadOnlyList<UpcomingDailyChallengeDto> results =
        [
            .. upcoming.Select(challenge =>
            {
                rowsById.TryGetValue(challenge.ProblemId, out AdminProblemListRowDto? row);
                return new UpcomingDailyChallengeDto(
                    challenge.ChallengeDate,
                    challenge.ProblemId,
                    row?.Slug ?? "",
                    row?.Title ?? "(problem no longer exists)",
                    ToDifficultyTier(row?.DifficultyValue ?? 0),
                    row?.Status ?? ProblemStatus.Archived
                );
            }),
        ];

        return Result.Success(results);
    }

    private static DifficultyTier ToDifficultyTier(int difficultyValue) =>
        difficultyValue <= Difficulty.BeginnerMax ? DifficultyTier.Beginner
        : difficultyValue <= Difficulty.EasyMax ? DifficultyTier.Easy
        : difficultyValue <= Difficulty.IntermediateMax ? DifficultyTier.Intermediate
        : difficultyValue <= Difficulty.AdvancedMax ? DifficultyTier.Advanced
        : DifficultyTier.Expert;
}