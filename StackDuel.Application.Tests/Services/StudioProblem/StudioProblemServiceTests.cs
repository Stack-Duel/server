using Ardalis.Result;
using MediatR;
using Moq;
using StackDuel.Application;
using StackDuel.Application.Commands.Problems.CreateDraftProblem;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Queries.StudioProblems.GetInitialStudioProblemSetups;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.StudioProblems.Dtos;
using StackDuel.Application.Tracks.Dtos;
using StackDuel.Domain.Problems.Enums;

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
}
