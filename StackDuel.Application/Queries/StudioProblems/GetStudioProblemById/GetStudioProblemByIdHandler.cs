using Ardalis.Result;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.ProblemValidation;
using StackDuel.Application.ProblemValidation.Dtos;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;

internal sealed class GetStudioProblemByIdHandler(
    IProblemReadRepository problemReadRepository,
    IProblemReactionReadRepository problemReactionReadRepository,
    ISubmissionReadRepository submissionReadRepository,
    ILanguageReadRepository languageReadRepository,
    IProblemValidationJobReadRepository problemValidationJobReadRepository
) : IQueryHandler<GetStudioProblemByIdQuery, StudioProblemDetailsDto>
{
    private static readonly HashSet<ProblemStatus> SubmittableStatuses = new HashSet<ProblemStatus>
    {
        ProblemStatus.Draft,
        ProblemStatus.Failed,
    };

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

        StudioProblemPublishReadinessDto? publishReadiness = await BuildPublishReadinessAsync(
            request.ProblemId,
            details.Status,
            details.HasUnpublishedChanges,
            details.IsRevalidating,
            cancellationToken
        );

        int reactionCount = reactionSummary.Counts.Sum(c => c.Count);
        double solveRatePercent =
            submissionStats.TotalSubmissions == 0
                ? 0
                : Math.Round((double)submissionStats.AcceptedSubmissions / submissionStats.TotalSubmissions * 100, 1);

        return Result<StudioProblemDetailsDto>.Success(
            details with
            {
                ReactionCount = reactionCount,
                AttemptCount = submissionStats.TotalSubmissions,
                SolveRatePercent = solveRatePercent,
                DifficultyTier = new Difficulty(details.DifficultyValue).Tier,
                PublishReadiness = publishReadiness,
            }
        );
    }

    private async Task<StudioProblemPublishReadinessDto?> BuildPublishReadinessAsync(
        Guid problemId,
        ProblemStatus status,
        bool hasUnpublishedChanges,
        bool isRevalidating,
        CancellationToken cancellationToken
    )
    {
        ProblemPublishReadinessProjection? projection = await problemReadRepository.FindPublishReadinessAsync(
            problemId,
            cancellationToken
        );

        if (projection is null)
            return null;

        ProblemPublishReadiness readiness = new(
            projection.SetupCount,
            projection.LanguageVersionIdsMissingReferenceSolution.Count,
            projection.SampleTestCaseCount,
            projection.HasGenerationParameters
        );

        IReadOnlyList<string> languagesMissingReferenceSolution = await ResolveLanguageNamesAsync(
            projection.LanguageVersionIdsMissingReferenceSolution,
            cancellationToken
        );

        ProblemValidationAttemptSummaryDto attempts = await problemValidationJobReadRepository.GetAttemptSummaryAsync(
            problemId,
            cancellationToken
        );

        return new StudioProblemPublishReadinessDto(
            SubmittableStatuses.Contains(status)
                || (status == ProblemStatus.Published && hasUnpublishedChanges && !isRevalidating),
            readiness.IsReady,
            readiness.RequirementsMet,
            ProblemPublishReadiness.RequirementCount,
            readiness.SetupCount,
            languagesMissingReferenceSolution,
            readiness.SampleTestCaseCount,
            readiness.HasGenerationParameters,
            attempts.AttemptCount,
            attempts.LastSubmittedAt,
            attempts.LastCompletedAt,
            attempts.LastStatus,
            attempts.LastFailureReason,
            attempts.Steps,
            attempts.ProcessingAttempts,
            attempts.LastError,
            hasUnpublishedChanges,
            isRevalidating
        );
    }

    private async Task<IReadOnlyList<string>> ResolveLanguageNamesAsync(
        IReadOnlyList<Guid> languageVersionIds,
        CancellationToken cancellationToken
    )
    {
        if (languageVersionIds.Count == 0)
            return [];

        var languages = await languageReadRepository.FindLanguagesByVersionId(languageVersionIds, cancellationToken);

        Dictionary<Guid, string> namesByVersionId = languages
            .SelectMany(language => language.Versions.Select(version => (version.Id, Name: (string)language.Name)))
            .ToDictionary(entry => entry.Id, entry => entry.Name);

        return
        [
            .. languageVersionIds
                .Select(versionId => namesByVersionId.TryGetValue(versionId, out string? name) ? name : "Unknown")
                .Distinct()
                .Order(),
        ];
    }
}
