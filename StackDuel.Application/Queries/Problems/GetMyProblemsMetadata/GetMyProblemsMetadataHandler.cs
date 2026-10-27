using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Submissions;

namespace StackDuel.Application.Queries.Problems.GetMyProblemsMetadata;

internal sealed class GetMyProblemsMetadataHandler(
    ISubmissionReadRepository submissionReadRepository,
    IProblemReactionReadRepository problemReactionReadRepository,
    IProblemReadRepository problemReadRepository,
    UserContext userContext
) : IQueryHandler<GetMyProblemsMetadataQuery, MyProblemsMetadataDto>
{
    public async Task<Result<MyProblemsMetadataDto>> Handle(
        GetMyProblemsMetadataQuery request,
        CancellationToken cancellationToken
    )
    {
        Guid creatorId = userContext.User!.Id;

        var submissionStats = await submissionReadRepository.GetSubmissionStatsForCreatorAsync(
            creatorId,
            cancellationToken
        );

        var reactionBreakdown = await problemReactionReadRepository.GetReactionBreakdownForCreatorAsync(
            creatorId,
            cancellationToken
        );

        var statusBreakdown = await problemReadRepository.GetStatusBreakdownForCreatorAsync(
            creatorId,
            cancellationToken
        );
        var orderedStatusBreakdown = statusBreakdown
            .OrderBy(x => x.Status.ToString(), StringComparer.Ordinal)
            .ToList();

        double averageCompletionRate =
            submissionStats.TotalSubmissions == 0
                ? 0
                : (double)submissionStats.AcceptedSubmissions / submissionStats.TotalSubmissions;

        return Result.Success(
            new MyProblemsMetadataDto
            {
                TotalSubmissions = submissionStats.TotalSubmissions,
                TotalReactions = reactionBreakdown.Sum(x => x.Count),
                ReactionBreakdown = reactionBreakdown,
                StatusBreakdown = orderedStatusBreakdown,
                AverageCompletionRate = averageCompletionRate,
            }
        );
    }
}
