using Ardalis.Result;
using MediatR;
using Moq;
using StackDuel.Application;
using StackDuel.Application.Queries.StudioProblems.GetStudioProblemById;
using StackDuel.Application.StudioProblems.Dtos;
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
                CreatedAt: DateTime.UtcNow
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
}
