using Ardalis.Result;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact;

internal sealed class GetStudioProblemDeletionImpactHandler(IProblemReadRepository problemReadRepository)
    : IQueryHandler<GetStudioProblemDeletionImpactQuery, ProblemDeletionImpactDto>
{
    public async Task<Result<ProblemDeletionImpactDto>> Handle(
        GetStudioProblemDeletionImpactQuery request,
        CancellationToken cancellationToken
    )
    {
        Guid? createdById = await problemReadRepository.GetCreatedByIdAsync(request.ProblemId, cancellationToken);

        if (createdById is null)
            return Result<ProblemDeletionImpactDto>.NotFound();

        if (createdById != request.RequestedByUserId)
            return Result<ProblemDeletionImpactDto>.NotFound();

        return Result.Success(
            await problemReadRepository.FindDeletionImpactAsync(request.ProblemId, cancellationToken)
        );
    }
}