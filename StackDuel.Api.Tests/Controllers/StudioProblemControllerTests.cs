using Ardalis.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StackDuel.Api.Controllers;
using StackDuel.Api.Requests.Problem;
using StackDuel.Application;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Problems.Enums;

namespace StackDuel.Api.Tests.Controllers;

public class StudioProblemControllerTests
{
    private readonly Mock<IStudioProblemService> _service = new();

    private StudioProblemController ControllerFor(UserContext userContext) =>
        ControllerAssert.WithHttpContext(new StudioProblemController(_service.Object, userContext));

    private static StudioProblemDetailsDto DetailsFor(Guid problemId) =>
        new(
            problemId,
            "two-sum",
            "Two Sum",
            DifficultyValue: 2,
            ProblemStatus.Published,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            SetupCount: 1,
            CreatedAt: DateTime.UtcNow,
            Question: "Given an array of integers, return two indices."
        );

    private static StudioProblemDetailsSetupDto SetupDto() =>
        new(
            Guid.NewGuid(),
            "3.12",
            Guid.NewGuid(),
            "Python",
            "def solve():",
            "solve",
            "def solve(): return 1",
            "1",
            "3"
        );

    private static CreateDraftProblemRequest DraftRequest(Guid trackId) =>
        new(
            "Two Sum",
            "Given an array of integers, return two indices.",
            ["arrays", "hash-map"],
            Difficulty: 2,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            trackId
        );

    [Fact]
    public async Task GetByIdAsync_ReturnsTheServicesProblem()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        StudioProblemDetailsDto details = DetailsFor(problemId);
        _service
            .Setup(x => x.GetStudioProblemByIdAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StudioProblemDetailsDto>.Success(details));

        ActionResult<StudioProblemDetailsDto> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext(userId)
        ).GetByIdAsync(problemId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(details, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetByIdAsync_PassesTheAuthenticatedUserIdAndToken()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _service
            .Setup(x => x.GetStudioProblemByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StudioProblemDetailsDto>.Success(DetailsFor(problemId)));

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).GetByIdAsync(problemId, cts.Token);

        _service.Verify(x => x.GetStudioProblemByIdAsync(problemId, userId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ServiceNotFound_IsSurfacedAsAnErrorStatus()
    {
        _service
            .Setup(x => x.GetStudioProblemByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<StudioProblemDetailsDto>.NotFound());

        ActionResult<StudioProblemDetailsDto> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext()
        ).GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task GetInitialStudioProblemAsync_ReturnsTheServicesSetup()
    {
        var setup = new InitialStudioProblemSetupDto([new TrackDto(Guid.NewGuid(), "sql", "SQL", true, [])]);
        _service
            .Setup(x => x.GetInitialStudioProblemSetupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InitialStudioProblemSetupDto>.Success(setup));

        ActionResult<InitialStudioProblemSetupDto> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext()
        ).GetInitialStudioProblemAsync(CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(setup, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetInitialStudioProblemAsync_PassesTheCancellationTokenThrough()
    {
        using CancellationTokenSource cts = new();
        _service
            .Setup(x => x.GetInitialStudioProblemSetupAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InitialStudioProblemSetupDto>.Success(new InitialStudioProblemSetupDto([])));

        await ControllerFor(ControllerAssert.AuthenticatedContext()).GetInitialStudioProblemAsync(cts.Token);

        _service.Verify(x => x.GetInitialStudioProblemSetupAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSetupsAsync_ReturnsTheServicesSetups()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        IEnumerable<StudioProblemDetailsSetupDto> setups = [SetupDto()];
        _service
            .Setup(x => x.GetStudioProblemDetailSetupsAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IEnumerable<StudioProblemDetailsSetupDto>>.Success(setups));

        ActionResult<IEnumerable<StudioProblemDetailsSetupDto>> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext(userId)
        ).GetSetupsAsync(problemId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(setups, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetSetupsAsync_PassesTheAuthenticatedUserIdAndToken()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _service
            .Setup(x =>
                x.GetStudioProblemDetailSetupsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<IEnumerable<StudioProblemDetailsSetupDto>>.Success([]));

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).GetSetupsAsync(problemId, cts.Token);

        _service.Verify(x => x.GetStudioProblemDetailSetupsAsync(problemId, userId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetSetupsAsync_ServiceNotFound_IsSurfacedAsAnErrorStatus()
    {
        _service
            .Setup(x =>
                x.GetStudioProblemDetailSetupsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<IEnumerable<StudioProblemDetailsSetupDto>>.NotFound());

        ActionResult<IEnumerable<StudioProblemDetailsSetupDto>> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext()
        ).GetSetupsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task CreateAsync_ReturnsTheCreatedProblemId()
    {
        Guid createdId = Guid.NewGuid();
        _service
            .Setup(x => x.CreateStudioProblemAsync(It.IsAny<CreateDraftProblemInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(createdId));

        ActionResult<Guid> result = await ControllerFor(ControllerAssert.AuthenticatedContext()).CreateAsync(
            DraftRequest(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(createdId, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task CreateAsync_MapsEveryRequestFieldOntoTheInput()
    {
        using CancellationTokenSource cts = new();
        Guid trackId = Guid.NewGuid();
        CreateDraftProblemRequest request = DraftRequest(trackId);
        _service
            .Setup(x => x.CreateStudioProblemAsync(It.IsAny<CreateDraftProblemInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        await ControllerFor(ControllerAssert.AuthenticatedContext()).CreateAsync(request, cts.Token);

        _service.Verify(
            x =>
                x.CreateStudioProblemAsync(
                    It.Is<CreateDraftProblemInput>(input =>
                        input.Title == request.Title
                        && input.Question == request.Question
                        && input.Tags.SequenceEqual(request.Tags)
                        && input.Difficulty == request.Difficulty
                        && input.TimeLimitMs == request.TimeLimitMs
                        && input.MemoryLimitMb == request.MemoryLimitMb
                        && input.TrackId == trackId
                    ),
                    cts.Token
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_ServiceInvalid_IsSurfacedAsAnErrorStatus()
    {
        _service
            .Setup(x => x.CreateStudioProblemAsync(It.IsAny<CreateDraftProblemInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Invalid(new ValidationError { Identifier = "Title", ErrorMessage = "Required" }));

        ActionResult<Guid> result = await ControllerFor(ControllerAssert.AuthenticatedContext()).CreateAsync(
            DraftRequest(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status400BadRequest, ControllerAssert.StatusCodeOf(result));
    }

    private static UpdateStudioProblemMetadataRequest MetadataRequest() =>
        new(
            "Two Sum",
            new string('q', 60),
            Difficulty: 800,
            TimeLimitMs: 2000,
            MemoryLimitMb: 256,
            Tags: ["arrays"]
        );

    [Fact]
    public async Task UpdateMetadataAsync_PassesTheRouteIdAuthenticatedUserIdAndToken()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        UpdateStudioProblemMetadataRequest request = MetadataRequest();
        _service
            .Setup(x =>
                x.UpdateStudioProblemMetadataAsync(
                    It.IsAny<UpdateStudioProblemMetadataInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).UpdateMetadataAsync(
            problemId,
            request,
            cts.Token
        );

        _service.Verify(
            x =>
                x.UpdateStudioProblemMetadataAsync(
                    It.Is<UpdateStudioProblemMetadataInput>(input =>
                        input.ProblemId == problemId
                        && input.Title == request.Title
                        && input.Question == request.Question
                        && input.Difficulty == request.Difficulty
                        && input.TimeLimitMs == request.TimeLimitMs
                        && input.MemoryLimitMb == request.MemoryLimitMb
                    ),
                    userId,
                    cts.Token
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateMetadataAsync_ServiceNotFound_IsSurfacedAsAnErrorStatus()
    {
        _service
            .Setup(x =>
                x.UpdateStudioProblemMetadataAsync(
                    It.IsAny<UpdateStudioProblemMetadataInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.NotFound());

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext()).UpdateMetadataAsync(
            Guid.NewGuid(),
            MetadataRequest(),
            CancellationToken.None
        );

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task AddSetupAsync_PassesTheRouteIdAuthenticatedUserIdAndToken()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid languageVersionId = Guid.NewGuid();
        _service
            .Setup(x =>
                x.AddStudioProblemSetupAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).AddSetupAsync(
            problemId,
            new AddProblemSetupRequest(languageVersionId),
            cts.Token
        );

        _service.Verify(
            x => x.AddStudioProblemSetupAsync(problemId, languageVersionId, userId, cts.Token),
            Times.Once
        );
    }

    [Fact]
    public async Task UpsertSetupSolutionAsync_PassesTheRouteIdAuthenticatedUserIdAndToken()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var request = new UpsertStudioProblemSetupSolutionRequest(
            Guid.NewGuid(),
            StarterCode: "def solve():",
            FunctionName: "solve",
            ReferenceSolution: "def solve(): return 1"
        );
        _service
            .Setup(x =>
                x.UpsertStudioProblemSetupSolutionAsync(
                    It.IsAny<UpsertStudioProblemSetupSolutionInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).UpsertSetupSolutionAsync(
            problemId,
            request,
            cts.Token
        );

        _service.Verify(
            x =>
                x.UpsertStudioProblemSetupSolutionAsync(
                    It.Is<UpsertStudioProblemSetupSolutionInput>(input =>
                        input.ProblemId == problemId
                        && input.LanguageVersionId == request.LanguageVersionId
                        && input.StarterCode == request.StarterCode
                        && input.FunctionName == request.FunctionName
                        && input.ReferenceSolution == request.ReferenceSolution
                    ),
                    userId,
                    cts.Token
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SetGenerationParametersAsync_MapsRequestItemsAndPassesTheAuthenticatedUserId()
    {
        using CancellationTokenSource cts = new();
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var request = new SetProblemGenerationParametersRequest(
            [new GenerationParameterRequestItem("nums", "integer_array", 0, 100, 1, 50, null)],
            OutputValueType: "integer",
            TargetCaseCount: 20,
            Seed: 12345
        );
        _service
            .Setup(x =>
                x.SetStudioProblemGenerationParametersAsync(
                    It.IsAny<SetStudioProblemGenerationParametersInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        await ControllerFor(ControllerAssert.AuthenticatedContext(userId)).SetGenerationParametersAsync(
            problemId,
            request,
            cts.Token
        );

        _service.Verify(
            x =>
                x.SetStudioProblemGenerationParametersAsync(
                    It.Is<SetStudioProblemGenerationParametersInput>(input =>
                        input.ProblemId == problemId
                        && input.OutputValueType == "integer"
                        && input.TargetCaseCount == 20
                        && input.Seed == 12345
                        && input.Parameters.Count == 1
                        && input.Parameters[0].Name == "nums"
                    ),
                    userId,
                    cts.Token
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SetSampleTestCasesAsync_MapsEveryRequestItemIntoTheServiceInput()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var request = new SetProblemSampleTestCasesRequest(
            [
                new SampleTestCaseRequestItem(
                    "two positives",
                    [
                        new SampleTestCaseInputRequestItem("[1,2]", "integer_array"),
                        new SampleTestCaseInputRequestItem("3", "integer"),
                    ],
                    ExpectedOutputValue: "3",
                    ExpectedOutputValueType: "integer"
                ),
                new SampleTestCaseRequestItem(
                    null,
                    [new SampleTestCaseInputRequestItem("[]", "integer_array")],
                    ExpectedOutputValue: "0",
                    ExpectedOutputValueType: "integer"
                ),
            ]
        );

        SetStudioProblemSampleTestCasesInput? captured = null;
        _service
            .Setup(x =>
                x.SetStudioProblemSampleTestCasesAsync(
                    It.IsAny<SetStudioProblemSampleTestCasesInput>(),
                    userId,
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback(
                (SetStudioProblemSampleTestCasesInput input, Guid _, CancellationToken _) => captured = input
            )
            .ReturnsAsync(Result.Success());

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext(userId))
            .SetSampleTestCasesAsync(problemId, request, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.NotNull(captured);
        Assert.Equal(problemId, captured.ProblemId);
        Assert.Equal(2, captured.TestCases.Count);
        Assert.Equal("two positives", captured.TestCases[0].Name);
        Assert.Equal(["[1,2]", "3"], captured.TestCases[0].Inputs.Select(input => input.Value));
        Assert.Equal(["integer_array", "integer"], captured.TestCases[0].Inputs.Select(input => input.ValueType));
        Assert.Equal("3", captured.TestCases[0].ExpectedOutputValue);
        Assert.Equal("integer", captured.TestCases[0].ExpectedOutputValueType);
        Assert.Null(captured.TestCases[1].Name);
    }

    [Fact]
    public async Task SetSampleTestCasesAsync_SendsAnEmptyListWhenTheRequestCarriesNoCases()
    {
        SetStudioProblemSampleTestCasesInput? captured = null;
        _service
            .Setup(x =>
                x.SetStudioProblemSampleTestCasesAsync(
                    It.IsAny<SetStudioProblemSampleTestCasesInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback(
                (SetStudioProblemSampleTestCasesInput input, Guid _, CancellationToken _) => captured = input
            )
            .ReturnsAsync(Result.Success());

        await ControllerFor(ControllerAssert.AuthenticatedContext())
            .SetSampleTestCasesAsync(
                Guid.NewGuid(),
                new SetProblemSampleTestCasesRequest([]),
                CancellationToken.None
            );

        Assert.NotNull(captured);
        Assert.Empty(captured.TestCases);
    }

    [Fact]
    public async Task SetSampleTestCasesAsync_SurfacesAValidationFailure()
    {
        _service
            .Setup(x =>
                x.SetStudioProblemSampleTestCasesAsync(
                    It.IsAny<SetStudioProblemSampleTestCasesInput>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Invalid(new ValidationError("TestCases", "Add at least one language setup.")));

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext())
            .SetSampleTestCasesAsync(
                Guid.NewGuid(),
                new SetProblemSampleTestCasesRequest([]),
                CancellationToken.None
            );

        Assert.Equal(StatusCodes.Status400BadRequest, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task SubmitForValidationAsync_PassesTheProblemIdAndAuthenticatedUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _service
            .Setup(x => x.SubmitStudioProblemForValidationAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext(userId))
            .SubmitForValidationAsync(problemId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        _service.Verify(
            x => x.SubmitStudioProblemForValidationAsync(problemId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SubmitForValidationAsync_SurfacesARejectedSubmission()
    {
        _service
            .Setup(x =>
                x.SubmitStudioProblemForValidationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Invalid(new ValidationError("Status", "Already published.")));

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext())
            .SubmitForValidationAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task CancelValidationAsync_PassesTheProblemIdAndAuthenticatedUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _service
            .Setup(x => x.CancelStudioProblemValidationAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext(userId))
            .CancelValidationAsync(problemId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        _service.Verify(
            x => x.CancelStudioProblemValidationAsync(problemId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task CancelValidationAsync_SurfacesAProblemThatIsNotValidating()
    {
        _service
            .Setup(x =>
                x.CancelStudioProblemValidationAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Invalid(new ValidationError("Status", "Nothing to cancel.")));

        ActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext())
            .CancelValidationAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task GetStudioProblemDeletionImpactAsync_ReturnsTheServicesImpact()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        var impact = new ProblemDeletionImpactDto(
            SubmissionCount: 7,
            GameRoundCount: 3,
            UnfinishedGameCount: 2,
            DailyChallengeCount: 0,
            CampaignUnitCount: 0,
            PoolCount: 1
        );
        _service
            .Setup(x => x.GetStudioProblemDeletionImpactAsync(problemId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProblemDeletionImpactDto>.Success(impact));

        ActionResult<ProblemDeletionImpactDto> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext(userId)
        ).GetStudioProblemDeletionImpactAsync(problemId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf(result));
        Assert.Equal(impact, ControllerAssert.ValueOf(result));
    }

    [Fact]
    public async Task GetStudioProblemDeletionImpactAsync_SurfacesAMissingProblem()
    {
        _service
            .Setup(x =>
                x.GetStudioProblemDeletionImpactAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result<ProblemDeletionImpactDto>.NotFound());

        ActionResult<ProblemDeletionImpactDto> result = await ControllerFor(
            ControllerAssert.AuthenticatedContext()
        ).GetStudioProblemDeletionImpactAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, ControllerAssert.StatusCodeOf(result));
    }

    [Fact]
    public async Task DeleteStudioProblemAsync_PassesTheTypedTitleAndAuthenticatedUser()
    {
        Guid problemId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _service
            .Setup(x => x.DeleteStudioProblemAsync(problemId, "Count Vowels", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        IActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext(userId))
            .DeleteStudioProblemAsync(
                problemId,
                new DeleteStudioProblemRequest("Count Vowels"),
                CancellationToken.None
            );

        Assert.Equal(StatusCodes.Status200OK, ControllerAssert.StatusCodeOf((ActionResult)result));
        _service.Verify(
            x => x.DeleteStudioProblemAsync(problemId, "Count Vowels", userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteStudioProblemAsync_SurfacesAMismatchedTitle()
    {
        _service
            .Setup(x =>
                x.DeleteStudioProblemAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Invalid(new ValidationError("ConfirmationTitle", "Titles do not match.")));

        IActionResult result = await ControllerFor(ControllerAssert.AuthenticatedContext())
            .DeleteStudioProblemAsync(
                Guid.NewGuid(),
                new DeleteStudioProblemRequest("wrong"),
                CancellationToken.None
            );

        Assert.Equal(StatusCodes.Status400BadRequest, ControllerAssert.StatusCodeOf((ActionResult)result));
    }
}