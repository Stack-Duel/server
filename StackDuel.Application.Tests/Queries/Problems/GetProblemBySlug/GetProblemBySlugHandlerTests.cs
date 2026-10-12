using Ardalis.Result;
using Moq;
using StackDuel.Application.Languages;
using StackDuel.Application.Problems;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using GetProblemBySlugHandler = StackDuel.Application.Queries.Problems.GetProblemBySlug.GetProblemBySlugHandler;
using GetProblemBySlugQuery = StackDuel.Application.Queries.Problems.GetProblemBySlug.GetProblemBySlugQuery;

namespace StackDuel.Application.Tests.Queries.Problems.GetProblemBySlug;

public class GetProblemBySlugHandlerTests
{
    private Mock<IProblemReadRepository> _problemReadRepository = null!;
    private Mock<ILanguageReadRepository> _languageReadRepository = null!;
    private GetProblemBySlugHandler _handler = null!;

    public GetProblemBySlugHandlerTests()
    {
        _problemReadRepository = new Mock<IProblemReadRepository>();
        _languageReadRepository = new Mock<ILanguageReadRepository>();
        _handler = new GetProblemBySlugHandler(_problemReadRepository.Object, _languageReadRepository.Object);

        _languageReadRepository
            .Setup(x => x.FindLanguagesByVersionId(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemReadRepository
            .Setup(x => x.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemBySlugQuery("missing"),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemFound_LooksUpBySlugAndMapsFields()
    {
        var problem = CreateProblem();
        problem.AddTag(new ProblemTag(new Tag("arrays")));
        _problemReadRepository
            .Setup(x => x.FindBySlugAsync("two-sum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result<ProblemWithSetupsDto> result = await _handler.Handle(
            new GetProblemBySlugQuery("two-sum"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        ProblemWithSetupsDto dto = result.Value;
        Assert.Equal(problem.Id, dto.Id);
        Assert.Equal("two-sum", dto.Slug);
        Assert.Equal("Two Sum", dto.Title);
        Assert.Equivalent(new[] { "arrays" }, dto.Tags, strict: true);
        Assert.Null(dto.Author);
        Assert.Empty(dto.PublicTestCases);
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithGivenSlug()
    {
        _problemReadRepository
            .Setup(x => x.FindBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        await _handler.Handle(new GetProblemBySlugQuery("some-slug"), CancellationToken.None);

        _problemReadRepository.Verify(x => x.FindBySlugAsync("some-slug", It.IsAny<CancellationToken>()), Times.Once);
    }
}