using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.ProblemValidation;
using StackDuel.Application.ProblemValidation.Dtos;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.ProblemValidation.Enums;

namespace StackDuel.Application.Tests.Queries.StudioProblems.GetStudioProblemById;

public class GetStudioProblemByIdHandlerTests
{
    private readonly Mock<IProblemReadRepository> _problemReadRepository = new();
    private readonly Mock<IProblemReactionReadRepository> _problemReactionReadRepository = new();
    private readonly Mock<ISubmissionReadRepository> _submissionReadRepository = new();
    private readonly Mock<ILanguageReadRepository> _languageReadRepository = new();
    private readonly Mock<IProblemValidationJobReadRepository> _problemValidationJobReadRepository = new();
    private readonly GetStudioProblemByIdHandler _handler;

    public GetStudioProblemByIdHandlerTests()
    {
        _problemValidationJobReadRepository
            .Setup(x => x.GetAttemptSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProblemValidationAttemptSummaryDto.NeverSubmitted);

        _handler = new GetStudioProblemByIdHandler(
            _problemReadRepository.Object,
            _problemReactionReadRepository.Object,
            _submissionReadRepository.Object,
            _languageReadRepository.Object,
            _problemValidationJobReadRepository.Object
        );
    }

    private static StudioProblemDetailsDto CreateCoreDetails(Guid problemId) =>
        new(
            problemId,
            "two-sum",
            "Two Sum",
            DifficultyValue: 501,
            ProblemStatus.Published,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            SetupCount: 3,
            CreatedAt: DateTime.UtcNow,
            Question: "Given an array of integers, return two indices."
        );

    [Fact]
    public async Task Handle_ProblemExists_ReturnsDetailsWithReactionAndSolveStats()
    {
        Guid problemId = Guid.NewGuid();
        StudioProblemDetailsDto core = CreateCoreDetails(problemId);

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(core);

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ProblemReactionSummaryDto(
                    [
                        new ProblemReactionCountDto("like", "Like", "👍", 7),
                        new ProblemReactionCountDto("love", "Love", "❤️", 3),
                    ],
                    null
                )
            );

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 40, AcceptedSubmissions: 25));

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.ReactionCount);
        Assert.Equal(40, result.Value.AttemptCount);
        Assert.Equal(62.5, result.Value.SolveRatePercent);
        Assert.Equal(DifficultyTier.Intermediate, result.Value.DifficultyTier);
    }

    [Fact]
    public async Task Handle_NoAttempts_SolveRateIsZero()
    {
        Guid problemId = Guid.NewGuid();
        StudioProblemDetailsDto core = CreateCoreDetails(problemId);

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(core);

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemReactionSummaryDto([], null));

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 0, AcceptedSubmissions: 0));

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(0, result.Value.SolveRatePercent);
    }

    [Fact]
    public async Task Handle_SetupMissingReferenceSolution_ReportsUnmetRequirementWithLanguageName()
    {
        Guid problemId = Guid.NewGuid();
        Language language = new(new LanguageName("Go"), new LanguageSlug("go"), Guid.NewGuid());
        LanguageVersionEntry versionEntry = language.AddVersion(new LanguageVersion("1.22"), new Judge0Id(95));
        Guid languageVersionId = versionEntry.Id;

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCoreDetails(problemId) with { Status = ProblemStatus.Draft });

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemReactionSummaryDto([], null));

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 0, AcceptedSubmissions: 0));

        _problemReadRepository
            .Setup(x => x.FindPublishReadinessAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ProblemPublishReadinessProjection(
                    SetupCount: 2,
                    LanguageVersionIdsMissingReferenceSolution: [languageVersionId],
                    SampleTestCaseCount: 6,
                    HasGenerationParameters: true
                )
            );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        StudioProblemPublishReadinessDto readiness = result.Value.PublishReadiness!;

        Assert.False(readiness.IsReady);
        Assert.True(readiness.CanSubmit);
        Assert.Equal(3, readiness.RequirementsMet);
        Assert.Equal(["Go"], readiness.LanguagesMissingReferenceSolution);
        Assert.Equal(0, readiness.AttemptCount);
    }

    [Fact]
    public async Task Handle_ProblemMissing_ReturnsNotFound()
    {
        Guid problemId = Guid.NewGuid();

        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StudioProblemDetailsDto?)null);

        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _problemReactionReadRepository.VerifyNoOtherCalls();
        _submissionReadRepository.VerifyNoOtherCalls();
    }

    private void GivenProblemWithStatus(Guid problemId, ProblemStatus status)
    {
        _problemReadRepository
            .Setup(x => x.FindStudioProblemDetailsAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCoreDetails(problemId) with { Status = status });

        _problemReactionReadRepository
            .Setup(x => x.GetSummaryAsync(problemId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProblemReactionSummaryDto([], null));

        _submissionReadRepository
            .Setup(x => x.GetSubmissionStatsForProblemAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionStatsDto(TotalSubmissions: 0, AcceptedSubmissions: 0));
    }

    private void GivenPublishReadiness(Guid problemId, ProblemPublishReadinessProjection? projection) =>
        _problemReadRepository
            .Setup(x => x.FindPublishReadinessAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

    private async Task<StudioProblemDetailsDto> HandleAsync(Guid problemId)
    {
        Result<StudioProblemDetailsDto> result = await _handler.Handle(
            new GetStudioProblemByIdQuery(problemId, Guid.NewGuid()),
            CancellationToken.None
        );

        return result.Value;
    }

    [Fact]
    public async Task Handle_NoPublishReadinessProjection_LeavesPublishReadinessNull()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblemWithStatus(problemId, ProblemStatus.Draft);
        GivenPublishReadiness(problemId, null);

        StudioProblemDetailsDto details = await HandleAsync(problemId);

        Assert.Null(details.PublishReadiness);
        _problemValidationJobReadRepository.Verify(
            x => x.GetAttemptSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_EveryRequirementMet_ReportsReadyWithoutResolvingLanguageNames()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblemWithStatus(problemId, ProblemStatus.Draft);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 2,
                LanguageVersionIdsMissingReferenceSolution: [],
                SampleTestCaseCount: 4,
                HasGenerationParameters: true
            )
        );

        StudioProblemPublishReadinessDto readiness = (await HandleAsync(problemId)).PublishReadiness!;

        Assert.True(readiness.IsReady);
        Assert.Equal(4, readiness.RequirementsMet);
        Assert.Equal(4, readiness.RequirementCount);
        Assert.Empty(readiness.LanguagesMissingReferenceSolution);
        _languageReadRepository.Verify(
            x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(ProblemStatus.Published)]
    [InlineData(ProblemStatus.Archived)]
    [InlineData(ProblemStatus.Pending)]
    public async Task Handle_StatusNotSubmittable_ReportsCannotSubmit(ProblemStatus status)
    {
        Guid problemId = Guid.NewGuid();
        GivenProblemWithStatus(problemId, status);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 1,
                LanguageVersionIdsMissingReferenceSolution: [],
                SampleTestCaseCount: 1,
                HasGenerationParameters: true
            )
        );

        StudioProblemPublishReadinessDto readiness = (await HandleAsync(problemId)).PublishReadiness!;

        Assert.False(readiness.CanSubmit);
        Assert.True(readiness.IsReady);
    }

    [Theory]
    [InlineData(ProblemStatus.Draft)]
    [InlineData(ProblemStatus.Failed)]
    public async Task Handle_StatusSubmittable_ReportsCanSubmit(ProblemStatus status)
    {
        Guid problemId = Guid.NewGuid();
        GivenProblemWithStatus(problemId, status);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 1,
                LanguageVersionIdsMissingReferenceSolution: [],
                SampleTestCaseCount: 1,
                HasGenerationParameters: true
            )
        );

        Assert.True((await HandleAsync(problemId)).PublishReadiness!.CanSubmit);
    }

    [Fact]
    public async Task Handle_LanguageVersionNotResolvable_ReportsUnknown()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblemWithStatus(problemId, ProblemStatus.Draft);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 1,
                LanguageVersionIdsMissingReferenceSolution: [Guid.NewGuid()],
                SampleTestCaseCount: 1,
                HasGenerationParameters: true
            )
        );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        StudioProblemPublishReadinessDto readiness = (await HandleAsync(problemId)).PublishReadiness!;

        Assert.Equal(["Unknown"], readiness.LanguagesMissingReferenceSolution);
    }

    [Fact]
    public async Task Handle_MultipleSetupsOnSameLanguage_ReportsEachLanguageNameOnce()
    {
        Guid problemId = Guid.NewGuid();
        Language language = new(new LanguageName("Go"), new LanguageSlug("go"), Guid.NewGuid());
        Guid firstVersionId = language.AddVersion(new LanguageVersion("1.21"), new Judge0Id(94)).Id;
        Guid secondVersionId = language.AddVersion(new LanguageVersion("1.22"), new Judge0Id(95)).Id;

        GivenProblemWithStatus(problemId, ProblemStatus.Draft);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 2,
                LanguageVersionIdsMissingReferenceSolution: [firstVersionId, secondVersionId],
                SampleTestCaseCount: 1,
                HasGenerationParameters: true
            )
        );

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([language]);

        StudioProblemPublishReadinessDto readiness = (await HandleAsync(problemId)).PublishReadiness!;

        Assert.Equal(["Go"], readiness.LanguagesMissingReferenceSolution);
    }

    [Fact]
    public async Task Handle_PreviousValidationAttempt_SurfacesAttemptSummary()
    {
        Guid problemId = Guid.NewGuid();
        DateTime submittedAt = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime completedAt = new(2026, 3, 1, 10, 5, 0, DateTimeKind.Utc);

        GivenProblemWithStatus(problemId, ProblemStatus.Failed);
        GivenPublishReadiness(
            problemId,
            new ProblemPublishReadinessProjection(
                SetupCount: 1,
                LanguageVersionIdsMissingReferenceSolution: [],
                SampleTestCaseCount: 1,
                HasGenerationParameters: true
            )
        );

        _problemValidationJobReadRepository
            .Setup(x => x.GetAttemptSummaryAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ProblemValidationAttemptSummaryDto(
                    AttemptCount: 3,
                    LastSubmittedAt: submittedAt,
                    LastCompletedAt: completedAt,
                    LastStatus: ProblemValidationJobStatus.Failed,
                    LastFailureReason: "reference solution timed out"
                )
            );

        StudioProblemPublishReadinessDto readiness = (await HandleAsync(problemId)).PublishReadiness!;

        Assert.Equal(3, readiness.AttemptCount);
        Assert.Equal(submittedAt, readiness.LastSubmittedAt);
        Assert.Equal(completedAt, readiness.LastCompletedAt);
        Assert.Equal(ProblemValidationJobStatus.Failed, readiness.LastAttemptStatus);
        Assert.Equal("reference solution timed out", readiness.LastFailureReason);
    }
}