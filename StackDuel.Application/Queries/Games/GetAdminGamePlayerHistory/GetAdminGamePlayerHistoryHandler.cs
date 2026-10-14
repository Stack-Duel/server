using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Problems;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Queries.Games.GetAdminGamePlayerHistory;

internal sealed class GetAdminGamePlayerHistoryHandler(
    IGameReadRepository gameReadRepository,
    ISubmissionReadRepository submissionReadRepository,
    IProblemReadRepository problemReadRepository
) : IQueryHandler<GetAdminGamePlayerHistoryQuery, IReadOnlyList<AdminGamePlayerHistoryDto>>
{
    public async Task<Result<IReadOnlyList<AdminGamePlayerHistoryDto>>> Handle(
        GetAdminGamePlayerHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
        if (game is null)
            return Result<IReadOnlyList<AdminGamePlayerHistoryDto>>.NotFound();

        IReadOnlyList<GameSubmissionEventDto> submissions = await submissionReadRepository.GetSubmissionsForGameAsync(
            request.GameId,
            cancellationToken
        );
        ILookup<Guid, GameSubmissionEventDto> submissionsByUser = submissions.ToLookup(s => s.UserId);

        Dictionary<Guid, Guid> problemIdByRoundId = game.Rounds.ToDictionary(r => r.Id, r => r.ProblemId);

        // Titles for a problem skipped with zero attempts never appear in the submissions join
        // above, so they are resolved separately here.
        // NOTE: skips now DO carry their own timestamp — GameRoundResult.CompletedAt is required —
        // so the fabricated ordering below could read the real one instead. Left alone because that
        // would change the timestamps this admin DTO reports.
        HashSet<Guid> skippedProblemIds =
        [
            .. game.Participants.SelectMany(p => SkippedProblemIdsFor(p, problemIdByRoundId)),
        ];
        Dictionary<Guid, (string Title, string Slug)> skippedProblemTitles =
            skippedProblemIds.Count == 0
                ? []
                : (await problemReadRepository.FindByIdsAsync(skippedProblemIds, cancellationToken)).ToDictionary(
                    p => p.Id,
                    p => (p.Title, p.Slug)
                );

        var histories = game
            .Participants.Select(participant =>
                BuildHistory(
                    participant,
                    submissionsByUser[participant.UserId].ToList(),
                    skippedProblemTitles,
                    problemIdByRoundId
                )
            )
            .ToList();

        return Result<IReadOnlyList<AdminGamePlayerHistoryDto>>.Success(histories);
    }

    private static IEnumerable<Guid> SkippedProblemIdsFor(
        GameParticipant participant,
        IReadOnlyDictionary<Guid, Guid> problemIdByRoundId
    ) =>
        participant
            .RoundResults.Where(result => result.Outcome == GameRoundOutcome.Skipped)
            .Where(result => problemIdByRoundId.ContainsKey(result.RoundId))
            .Select(result => problemIdByRoundId[result.RoundId]);

    private static AdminGamePlayerHistoryDto BuildHistory(
        GameParticipant participant,
        IReadOnlyList<GameSubmissionEventDto> userSubmissions,
        IReadOnlyDictionary<Guid, (string Title, string Slug)> skippedProblemTitles,
        IReadOnlyDictionary<Guid, Guid> problemIdByRoundId
    )
    {
        List<AdminGameHistoryEventDto> events =
        [
            .. userSubmissions.Select(s => new AdminGameHistoryEventDto(
                s.Status == SubmissionStatus.Accepted
                    ? AdminGameHistoryEventType.Accepted
                    : AdminGameHistoryEventType.WrongAnswer,
                s.ProblemId,
                s.ProblemTitle,
                s.ProblemSlug,
                s.CreatedAt,
                s.Id,
                s.LanguageName
            )),
        ];

        foreach (Guid skippedProblemId in SkippedProblemIdsFor(participant, problemIdByRoundId))
        {
            DateTime? lastAttemptAt = userSubmissions
                .Where(s => s.ProblemId == skippedProblemId)
                .Select(s => (DateTime?)s.CreatedAt)
                .Max();

            (string title, string slug) = skippedProblemTitles.TryGetValue(skippedProblemId, out var lookup)
                ? lookup
                : ("Unknown problem", "");

            events.Add(
                new AdminGameHistoryEventDto(
                    AdminGameHistoryEventType.Skipped,
                    skippedProblemId,
                    title,
                    slug,
                    lastAttemptAt,
                    null,
                    null
                )
            );
        }

        events = [.. events.OrderBy(e => e.OccurredAt ?? DateTime.MaxValue)];

        return new AdminGamePlayerHistoryDto(participant.UserId, events);
    }
}