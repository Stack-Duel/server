using Ardalis.Result;
using MediatR;
using Moq;
using StackDuel.Application;
using StackDuel.Application.Commands.Problems.AddProblemSetup;
using StackDuel.Application.Commands.Problems.CancelProblemValidation;
using StackDuel.Application.Commands.Problems.CreateDraftProblem;
using StackDuel.Application.Commands.Problems.DeleteProblem;
using StackDuel.Application.Commands.Problems.SetProblemGenerationParameters;
using StackDuel.Application.Commands.Problems.SetProblemSampleTestCases;
using StackDuel.Application.Commands.Problems.SubmitProblemForValidation;
using StackDuel.Application.Commands.Problems.UpdateProblem;
using StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemDeletionImpact;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;

namespace StackDuel.Application.Tests.Services.StudioProblem;

public class StudioProblemServiceTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly StudioProblemService _service;

    public StudioProblemServiceTests()
    {
        _service = new StudioProblemService(_mediator.Object);
    }

    [Fact]
    public async Task GetStudioProblemByIdAsync_SendsQueryWithIdAndUserId()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var expected = Result<StudioProblemDetailsDto>.Success(
            new StudioProblemDetailsDto(
                problemId,
                "two-sum",
                "Two Sum",
                DifficultyValue: 2,
                ProblemStatus.Published,
                TimeLimitMs: 2000,
                MemoryLimitMb: 256,
                SetupCount: 3,
                CreatedAt: DateTime.UtcNow,
                Question: "Given an array of integers, return two indices."
            )
        );

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<GetStudioProblemByIdQuery>(q =>
                        q.ProblemId == problemId && q.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        Result<StudioProblemDetailsDto> result = await _service.GetStudioProblemByIdAsync(
            problemId,
            userId,
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetInitialStudioProblemSetupAsync_SendsQuery()
    {
        var expected = Result<InitialStudioProblemSetupDto>.Success(
            new InitialStudioProblemSetupDto([new TrackDto(Guid.NewGuid(), "sql", "SQL", true, [])])
        );

        _mediator
            .Setup(x => x.Send(It.IsAny<GetInitialStudioProblemSetupsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        Result<InitialStudioProblemSetupDto> result = await _service.GetInitialStudioProblemSetupAsync(
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CreateStudioProblemAsync_SendsCommandWithInput()
    {
        var input = new CreateDraftProblemInput(
            "Two Sum",
            "Given an array of integers, return two indices.",
            ["arrays", "hash-map"],
            Difficulty: 2,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            TrackId: Guid.NewGuid()
        );
        var expected = Result<Guid>.Success(Guid.NewGuid());

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<CreateDraftProblemCommand>(c => c.Input == input),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        Result<Guid> result = await _service.CreateStudioProblemAsync(input, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpdateStudioProblemMetadataAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var input = new UpdateStudioProblemMetadataInput(
            problemId,
            "Two Sum",
            new string('q', 60),
            Difficulty: 800,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            Tags: ["arrays"]
        );

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<UpdateProblemCommand>(c =>
                        c.ProblemId == problemId && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.UpdateStudioProblemMetadataAsync(
            input,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateStudioProblemMetadataAsync_PassesTagsThroughAndNeverTransitionsStatus()
    {
        Guid problemId = Guid.NewGuid();
        var input = new UpdateStudioProblemMetadataInput(
            problemId,
            "Two Sum",
            new string('q', 60),
            Difficulty: 800,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            Tags: ["arrays"]
        );

        _mediator
            .Setup(x => x.Send(It.IsAny<UpdateProblemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        await _service.UpdateStudioProblemMetadataAsync(input, Guid.NewGuid(), CancellationToken.None);

        _mediator.Verify(
            x =>
                x.Send(
                    It.Is<UpdateProblemCommand>(c =>
                        c.Status == null && c.Tags != null && c.Tags.Contains("arrays")
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AddStudioProblemSetupAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid languageVersionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var expected = Result<Guid>.Success(Guid.NewGuid());

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<AddProblemSetupCommand>(c =>
                        c.ProblemId == problemId
                        && c.LanguageVersionId == languageVersionId
                        && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        Result<Guid> result = await _service.AddStudioProblemSetupAsync(
            problemId,
            languageVersionId,
            userId,
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task UpsertStudioProblemSetupSolutionAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var input = new UpsertStudioProblemSetupSolutionInput(
            problemId,
            LanguageVersionId: Guid.NewGuid(),
            StarterCode: "function solve() {}",
            FunctionName: "solve",
            ReferenceSolution: "function solve() { return 1; }"
        );
        var expected = Result<Guid>.Success(Guid.NewGuid());

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<UpsertProblemSetupReferenceSolutionCommand>(c =>
                        c.ProblemId == problemId
                        && c.InitialCode == input.StarterCode
                        && c.ReferenceSolutionCode == input.ReferenceSolution
                        && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(expected);

        Result<Guid> result = await _service.UpsertStudioProblemSetupSolutionAsync(
            input,
            userId,
            CancellationToken.None
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SetStudioProblemGenerationParametersAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var input = new SetStudioProblemGenerationParametersInput(
            problemId,
            [new GenerationParameterSpec("nums", "integer_array", 0, 100, 1, 50, null)],
            OutputValueType: "integer",
            TargetCaseCount: 20,
            Seed: 12345
        );

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<SetProblemGenerationParametersCommand>(c =>
                        c.ProblemId == problemId && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.SetStudioProblemGenerationParametersAsync(
            input,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetStudioProblemSampleTestCasesAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var input = new SetStudioProblemSampleTestCasesInput(
            problemId,
            [
                new SampleTestCaseDto(
                    "two positives",
                    [new SampleTestCaseInputDto("[1,2]", "integer_array")],
                    ExpectedOutputValue: "3",
                    ExpectedOutputValueType: "integer"
                ),
            ]
        );

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<SetProblemSampleTestCasesCommand>(c =>
                        c.ProblemId == problemId
                        && c.RequestedByUserId == userId
                        && c.TestCases.Count == 1
                        && c.TestCases[0].Name == "two positives"
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.SetStudioProblemSampleTestCasesAsync(input, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SetStudioProblemSampleTestCasesAsync_SurfacesAFailureUnchanged()
    {
        var input = new SetStudioProblemSampleTestCasesInput(Guid.NewGuid(), []);
        _mediator
            .Setup(x => x.Send(It.IsAny<SetProblemSampleTestCasesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.NotFound());

        Result result = await _service.SetStudioProblemSampleTestCasesAsync(
            input,
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task SubmitStudioProblemForValidationAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<SubmitProblemForValidationCommand>(c =>
                        c.ProblemId == problemId && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.SubmitStudioProblemForValidationAsync(
            problemId,
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitStudioProblemForValidationAsync_SurfacesARejectionUnchanged()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<SubmitProblemForValidationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new ValidationError("Status", "Already published.")));

        Result result = await _service.SubmitStudioProblemForValidationAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task CancelStudioProblemValidationAsync_ScopesTheCommandToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<CancelProblemValidationCommand>(c =>
                        c.ProblemId == problemId && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.CancelStudioProblemValidationAsync(problemId, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetStudioProblemDeletionImpactAsync_ScopesTheQueryToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var impact = new ProblemDeletionImpactDto(
            SubmissionCount: 4,
            GameRoundCount: 2,
            UnfinishedGameCount: 1,
            DailyChallengeCount: 0,
            CampaignUnitCount: 0,
            PoolCount: 1
        );

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<GetStudioProblemDeletionImpactQuery>(q =>
                        q.ProblemId == problemId && q.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<ProblemDeletionImpactDto>.Success(impact));

        Result<ProblemDeletionImpactDto> result = await _service.GetStudioProblemDeletionImpactAsync(
            problemId,
            userId,
            CancellationToken.None
        );

        Assert.Equal(impact, result.Value);
    }

    [Fact]
    public async Task DeleteStudioProblemAsync_PassesTheTypedTitleThroughAndScopesToTheRequestingUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<DeleteProblemCommand>(c =>
                        c.ProblemId == problemId
                        && c.ConfirmationTitle == "Count Vowels"
                        && c.RequestedByUserId == userId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        Result result = await _service.DeleteStudioProblemAsync(
            problemId,
            "Count Vowels",
            userId,
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteStudioProblemAsync_SurfacesAMismatchedTitleUnchanged()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<DeleteProblemCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new ValidationError("ConfirmationTitle", "Titles do not match.")));

        Result result = await _service.DeleteStudioProblemAsync(
            Guid.NewGuid(),
            "wrong",
            Guid.NewGuid(),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}