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
}