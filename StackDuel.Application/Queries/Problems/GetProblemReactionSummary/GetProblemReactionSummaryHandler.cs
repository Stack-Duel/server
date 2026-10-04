using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Problems.GetProblemReactionSummary;

internal sealed class GetProblemReactionSummaryHandler(
    IProblemReadRepository problemReadRepository,
    IProblemReactionReadRepository problemReactionReadRepository
) : IQueryHandler<GetProblemReactionSummaryQuery, ProblemReactionSummaryDto>
{
    public async Task<Result<ProblemReactionSummaryDto>> Handle(
        GetProblemReactionSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        bool problemExists = await problemReadRepository.ExistsAsync(request.ProblemId, cancellationToken);
        if (!problemExists)
            return Result.NotFound($"Problem '{request.ProblemId}' was not found.");

        var summary = await problemReactionReadRepository.GetSummaryAsync(
            request.ProblemId,
            request.UserId,
            cancellationToken
        );

        return Result.Success(summary);
    }
}