using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;

internal sealed class GetStudioProblemByIdHandler(
    IProblemReadRepository problemReadRepository,
    IProblemReactionReadRepository problemReactionReadRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetStudioProblemByIdQuery, StudioProblemDetailsDto>
{
    public async Task<Result<StudioProblemDetailsDto>> Handle(
        GetStudioProblemByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        StudioProblemDetailsDto? details = await problemReadRepository.FindStudioProblemDetailsAsync(
            request.ProblemId,
            cancellationToken
        );

        if (details is null)
            return Result<StudioProblemDetailsDto>.NotFound();

        ProblemReactionSummaryDto reactionSummary = await problemReactionReadRepository.GetSummaryAsync(
            request.ProblemId,
            userId: null,
            cancellationToken
        );

        SubmissionStatsDto submissionStats = await submissionReadRepository.GetSubmissionStatsForProblemAsync(
            request.ProblemId,
            cancellationToken
        );

        int reactionCount = reactionSummary.Counts.Sum(c => c.Count);
        double solveRatePercent =
            submissionStats.TotalSubmissions == 0
                ? 0
                : Math.Round(
                    (double)submissionStats.AcceptedSubmissions / submissionStats.TotalSubmissions * 100,
                    1
                );

        return Result<StudioProblemDetailsDto>.Success(
            details with
            {
                ReactionCount = reactionCount,
                AttemptCount = submissionStats.TotalSubmissions,
                SolveRatePercent = solveRatePercent,
            }
        );
    }
}
